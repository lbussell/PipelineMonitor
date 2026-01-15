// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace PipelineMonitor;

/// <summary>
/// Provides access to Git remote URL information.
/// </summary>
public sealed class GitRemoteUrlProvider
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitRemoteUrlProvider"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner to use for executing git commands.</param>
    public GitRemoteUrlProvider(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <summary>
    /// Gets the remote URL for the specified remote name.
    /// </summary>
    /// <param name="remoteName">The name of the remote (e.g., "origin").</param>
    /// <param name="workingDirectory">The working directory of the git repository.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the remote URL or null if not found.</returns>
    public async Task<string?> GetRemoteUrlAsync(
        string remoteName = "origin",
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync(
            "git",
            $"remote get-url {remoteName}",
            workingDirectory,
            cancellationToken);

        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return result.StandardOutput.Trim();
        }

        return null;
    }
}
