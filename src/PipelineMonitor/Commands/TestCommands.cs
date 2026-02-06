// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text;
using ConsoleAppFramework;
using PipelineMonitor.AzureDevOps;
using PipelineMonitor.Git;
using Spectre.Console;

namespace PipelineMonitor.Commands;

internal sealed class TestCommands(
    IAnsiConsole ansiConsole,
    InteractionService interactionService,
    GitService gitService,
    VstsGitUrlParser vstsGitUrlParser)
{
    private readonly IAnsiConsole _ansiConsole = ansiConsole;
    private readonly InteractionService _interactionService = interactionService;
    private readonly GitService _gitService = gitService;
    private readonly VstsGitUrlParser _vstsGitUrlParser = vstsGitUrlParser;

    [Command("hello")]
    public async Task GreetUserAsync(string? name = null)
    {
        name ??= await _interactionService.PromptAsync<string>("What is your name?");
        _ansiConsole.WriteLine($"Hello, {name}!");
    }

    [Command("confirm")]
    public async Task ConfirmActionAsync()
    {
        var ok = await _interactionService.ConfirmAsync("All good?");
        _ansiConsole.WriteLine(ok ? "OK!" : "Not OK!");
    }

    [Command("tag")]
    public async Task SelectTagAsync()
    {
        var tag = await _interactionService.SelectAsync(
            prompt: "Enter a tag for the pipeline:",
            suggestions: ["foo", "bar", "baz"]);
        _ansiConsole.WriteLine($"Selected tag: {tag}");
    }

    [Command("gitstatus")]
    public async Task ShowGitStatusAsync()
    {
        var branch = await _gitService.GetCurrentBranchAsync();
        if (branch is null)
        {
            _ansiConsole.WriteLine("Not in a git repository");
            return;
        }

        _ansiConsole.WriteLine($"Branch: {branch}");

        var upstream = await _gitService.GetUpstreamBranchAsync();
        if (upstream is null)
        {
            _ansiConsole.WriteLine("Upstream: (not set)");
        }
        else
        {
            _ansiConsole.WriteLine($"Upstream: {upstream}");

            // Get the remote name from upstream (e.g., "origin/main" -> "origin")
            var remoteName = upstream.Split('/')[0];
            var remoteUrl = await _gitService.GetRemoteUrlByNameAsync(remoteName);

            if (remoteUrl is not null)
            {
                _ansiConsole.WriteLine($"Remote: {remoteUrl}");
                var isAzureDevOps = _vstsGitUrlParser.IsAzureDevOpsUrl(remoteUrl);
                _ansiConsole.WriteLine($"Azure DevOps: {(isAzureDevOps ? "Yes" : "No")}");
            }

            var aheadBehind = await _gitService.GetAheadBehindAsync();
            if (aheadBehind is not null)
            {
                var (ahead, behind) = aheadBehind.Value;
                if (ahead == 0 && behind == 0)
                    _ansiConsole.WriteLine("Status: Up to date");
                else
                    _ansiConsole.WriteLine($"Status: {ahead} ahead, {behind} behind");
            }
        }

        var workingTree = await _gitService.GetWorkingTreeStatusAsync();
        if (workingTree.IsClean)
        {
            _ansiConsole.WriteLine("Working tree: Clean");
        }
        else
        {
            var parts = new List<string>();
            if (workingTree.Staged > 0) parts.Add($"{workingTree.Staged} staged");
            if (workingTree.Modified > 0) parts.Add($"{workingTree.Modified} modified");
            if (workingTree.Untracked > 0) parts.Add($"{workingTree.Untracked} untracked");
            _ansiConsole.WriteLine($"Working tree: {string.Join(", ", parts)}");
        }
    }

    [Command("gitpush")]
    public async Task CommitAndPushAsync()
    {
        var branch = await _gitService.GetCurrentBranchAsync();
        if (branch is null)
        {
            _ansiConsole.WriteLine("Not in a git repository");
            return;
        }

        _ansiConsole.WriteLine($"Branch: {branch}");

        // Check for uncommitted changes
        var workingTree = await _gitService.GetWorkingTreeStatusAsync();
        string? commitMessage = null;

        if (!workingTree.IsClean)
        {
            var statusParts = new List<string>();
            if (workingTree.Staged > 0) statusParts.Add($"{workingTree.Staged} staged");
            if (workingTree.Modified > 0) statusParts.Add($"{workingTree.Modified} modified");
            if (workingTree.Untracked > 0) statusParts.Add($"{workingTree.Untracked} untracked");
            _ansiConsole.WriteLine($"Working tree: {string.Join(", ", statusParts)}");

            var shouldCommit = await _interactionService.ConfirmAsync("Commit changes?");
            if (!shouldCommit)
            {
                _ansiConsole.WriteLine("Operation cancelled.");
                return;
            }

            commitMessage = await _interactionService.PromptAsync<string>("Commit message");
            if (string.IsNullOrWhiteSpace(commitMessage))
            {
                _ansiConsole.WriteLine("Commit message cannot be empty. Operation cancelled.");
                return;
            }
        }
        else
        {
            _ansiConsole.WriteLine("Working tree: Clean");
        }

        // Determine push destination - goal is to sync with Azure DevOps
        string? pushDestination = null;
        string? pushRemote = null;
        string? pushBranch = null;
        bool hasAzureDevOpsUpstream = false;
        int commitsAhead = 0;

        var upstream = await _gitService.GetUpstreamBranchAsync();
        if (upstream is not null)
        {
            var remoteName = upstream.Split('/')[0];
            var remoteUrl = await _gitService.GetRemoteUrlByNameAsync(remoteName);
            hasAzureDevOpsUpstream = remoteUrl is not null && _vstsGitUrlParser.IsAzureDevOpsUrl(remoteUrl);

            var aheadBehind = await _gitService.GetAheadBehindAsync();
            if (aheadBehind is not null)
            {
                commitsAhead = aheadBehind.Value.Ahead;
                var behind = aheadBehind.Value.Behind;

                if (hasAzureDevOpsUpstream)
                {
                    if (commitsAhead == 0 && behind == 0 && commitMessage is null)
                    {
                        _ansiConsole.WriteLine($"Up to date with {upstream}");
                        _ansiConsole.WriteLine("Nothing to do.");
                        return;
                    }

                    if (behind > 0)
                    {
                        _ansiConsole.WriteLine($"Warning: {behind} commit(s) behind {upstream}. Consider pulling first.");
                    }
                }
            }

            if (hasAzureDevOpsUpstream)
            {
                // We have commits to push (either from new commit or existing ahead)
                if (commitMessage is not null || commitsAhead > 0)
                {
                    var shouldPush = await _interactionService.ConfirmAsync($"Push to {upstream}?");
                    if (shouldPush)
                    {
                        pushDestination = upstream;
                    }
                }
            }
            else
            {
                // Upstream exists but is not Azure DevOps
                var adoRemote = await _gitService.GetAzureDevOpsRemoteNameAsync(_vstsGitUrlParser.IsAzureDevOpsUrl);
                if (adoRemote is not null)
                {
                    _ansiConsole.WriteLine($"Upstream ({upstream}) is not Azure DevOps.");
                    _ansiConsole.WriteLine($"Found Azure DevOps remote: {adoRemote}");
                    _ansiConsole.WriteLine("You'll be prompted for a branch name, then shown a final confirmation before any push.");
                    var shouldPush = await _interactionService.ConfirmAsync("Set up push to Azure DevOps?");
                    if (shouldPush)
                    {
                        pushBranch = await _interactionService.PromptAsync("Remote branch name", branch);
                        pushRemote = adoRemote;
                        pushDestination = $"{adoRemote}/{pushBranch}";
                    }
                }
            }
        }
        else
        {
            // No upstream set - must push to Azure DevOps
            _ansiConsole.WriteLine("No upstream branch set.");
            var adoRemote = await _gitService.GetAzureDevOpsRemoteNameAsync(_vstsGitUrlParser.IsAzureDevOpsUrl);
            if (adoRemote is not null)
            {
                _ansiConsole.WriteLine($"Found Azure DevOps remote: {adoRemote}");
                _ansiConsole.WriteLine("You'll be prompted for a branch name, then shown a final confirmation before any push.");
                var shouldPush = await _interactionService.ConfirmAsync("Set up push to Azure DevOps?");
                if (shouldPush)
                {
                    pushBranch = await _interactionService.PromptAsync("Remote branch name", branch);
                    pushRemote = adoRemote;
                    pushDestination = $"{adoRemote}/{pushBranch}";
                }
            }
            else
            {
                _ansiConsole.WriteLine("No Azure DevOps remote found.");
                return;
            }
        }

        // If nothing to do
        if (commitMessage is null && pushDestination is null)
        {
            _ansiConsole.WriteLine("Nothing to do.");
            return;
        }

        // Build and show final confirmation
        var operations = new StringBuilder();
        operations.AppendLine();
        operations.AppendLine("The following operations will be performed:");
        if (commitMessage is not null)
        {
            operations.AppendLine("  • Stage all changes (git add -A)");
            operations.AppendLine($"  • Commit: \"{commitMessage}\"");
        }
        if (pushDestination is not null)
        {
            var pushCount = commitMessage is not null ? commitsAhead + 1 : commitsAhead;
            if (pushRemote is not null && pushBranch is not null)
            {
                // Pushing to a new/different branch
                operations.AppendLine($"  • Push to: {pushDestination}");
            }
            else if (pushCount == 1)
            {
                operations.AppendLine($"  • Push 1 commit to: {pushDestination}");
            }
            else
            {
                operations.AppendLine($"  • Push {pushCount} commits to: {pushDestination}");
            }
        }
        operations.AppendLine();

        _ansiConsole.Write(operations.ToString());

        var proceed = await _interactionService.ConfirmAsync("Proceed?");
        if (!proceed)
        {
            _ansiConsole.WriteLine("Operation cancelled. No changes were made.");
            return;
        }

        // Execute operations
        _ansiConsole.WriteLine();

        if (commitMessage is not null)
        {
            await _gitService.StageAllAsync();
            _ansiConsole.WriteLine("Staged all changes.");

            var commitOutput = await _gitService.CommitAsync(commitMessage);
            _ansiConsole.WriteLine(commitOutput);
        }

        if (pushDestination is not null)
        {
            string pushOutput;
            if (pushRemote is not null && pushBranch is not null)
            {
                pushOutput = await _gitService.PushToRemoteBranchAsync(pushRemote, pushBranch);
            }
            else
            {
                pushOutput = await _gitService.PushAsync();
            }

            if (!string.IsNullOrWhiteSpace(pushOutput))
            {
                _ansiConsole.WriteLine(pushOutput);
            }
            _ansiConsole.WriteLine($"Pushed to {pushDestination}.");
        }
    }
}
