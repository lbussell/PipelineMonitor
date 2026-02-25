// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

// Creates a release by tagging the current commit and pushing the tag.
// The publish workflow triggers automatically when a version tag is pushed.
//
// Usage: dotnet run scripts/Release.cs <version>
// Example: dotnet run scripts/Release.cs 0.6.0

#:package CliWrap@3.10.0
#:package Spectre.Console@0.54.1-alpha.0.31

using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

if (args.Length != 1)
{
    Prompt.Error("Expected a single version argument.");
    AnsiConsole.MarkupLine("Usage: [blue]dotnet run scripts/Release.cs <version>[/]");
    AnsiConsole.MarkupLine("Example: [blue]dotnet run scripts/Release.cs 0.6.0[/]");
    return 1;
}

var version = args[0];
if (!IsValidSemVer(version))
{
    Prompt.Error($"'{Markup.Escape(version)}' is not a valid SemVer version.");
    return 1;
}

var tag = $"v{version}";
var git = new CliWrapper("git");

Prompt.Info($"Preparing release [green]{Markup.Escape(tag)}[/]");

var statusResult = await git.RunAsync("status --porcelain");
if (!string.IsNullOrWhiteSpace(statusResult.StandardOutput))
{
    Prompt.Error("Working tree is not clean. Commit or stash your changes first.");
    return 1;
}

var tagResult = await git.RunAsync($"tag --list {tag}");
if (!string.IsNullOrWhiteSpace(tagResult.StandardOutput))
{
    Prompt.Error($"Tag [green]{Markup.Escape(tag)}[/] already exists.");
    return 1;
}

await git.RunWithConfirmationAsync($"tag {tag}");
Prompt.Success($"Created tag [green]{Markup.Escape(tag)}[/]");

await git.RunWithConfirmationAsync($"push origin {tag}");
Prompt.Success($"Pushed tag [green]{Markup.Escape(tag)}[/] — publish workflow will trigger automatically.");

AnsiConsole.WriteLine();
Prompt.Warning($"Remember to update the version in [blue].claude-plugin/plugin.json[/] and [blue].claude-plugin/marketplace.json[/] to [green]{Markup.Escape(version)}[/].");

return 0;

static bool IsValidSemVer(string version) =>
    Regex.IsMatch(version, @"^\d+\.\d+\.\d+(-[0-9A-Za-z\-]+(\.[0-9A-Za-z\-]+)*)?(\+[0-9A-Za-z\-]+(\.[0-9A-Za-z\-]+)*)?$");

#region Helpers
internal class CliWrapper
{
    private readonly string _commandName;
    private readonly Command _command;

    public CliWrapper(string command)
    {
        _commandName = command;
        var stdOutPipe = PipeTarget.ToDelegate(line => AnsiConsole.MarkupLineInterpolated($"[dim][[stdout]] {line}[/]"));
        var stdErrPipe = PipeTarget.ToDelegate(line => AnsiConsole.MarkupLineInterpolated($"[yellow][[stderr]] {line}[/]"));
        _command = Cli.Wrap(command)
            .WithStandardOutputPipe(stdOutPipe)
            .WithStandardErrorPipe(stdErrPipe)
            .WithValidation(CommandResultValidation.ZeroExitCode);
    }

    public async Task<BufferedCommandResult> RunWithConfirmationAsync(
        string arguments,
        string? standardInput = null,
        CancellationToken cancellationToken = default
    )
    {
        var commandString = Markup.Escape($"{_commandName} {arguments}");
        return !Prompt.Confirm($"Run `[blue]{commandString}[/]`?")
            ? throw new OperationCanceledException("User aborted the operation.")
            : await RunAsync(arguments, standardInput, cancellationToken);
    }

    public async Task<BufferedCommandResult> RunAsync(
        string arguments,
        string? standardInput = null,
        CancellationToken cancellationToken = default
    )
    {
        var commandString = Markup.Escape($"{_commandName} {arguments}");
        var cmd = _command.WithArguments(arguments);
        AnsiConsole.MarkupLineInterpolated($"[blue][[exec]] {commandString}[/]");
        if (standardInput is not null) cmd = cmd.WithStandardInputPipe(PipeSource.FromString(standardInput));
        return await cmd.ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cancellationToken);
    }
}

internal static class Prompt
{
    public static void Info(string message) => AnsiConsole.MarkupLine($"[green][[info]][/] {message}");
    public static void Success(string message) => AnsiConsole.MarkupLine($"[green][[success]][/] {message}");
    public static void Error(string message) => AnsiConsole.MarkupLine($"[red][[error]][/] {message}");
    public static void Warning(string message) => AnsiConsole.MarkupLine($"[yellow][[warning]][/] {message}");
    public static bool Confirm(string message) => AnsiConsole.Confirm($"[purple][[confirm]][/] {message}");
    public static string Ask(string message) => AnsiConsole.Prompt(new TextPrompt<string>(message).PromptStyle("blue"));
}
#endregion
