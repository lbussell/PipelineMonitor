// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace PipelineMonitor;

/// <summary>
/// Abstraction for running processes and capturing their output.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process with the specified executable and arguments.
    /// </summary>
    /// <param name="executable">The path to the executable.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="workingDirectory">Optional working directory for the process.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the process output.</returns>
    Task<ProcessResult> RunAsync(
        string executable,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of running a process.
/// </summary>
public sealed class ProcessResult
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output of the process.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// Gets the standard error of the process.
    /// </summary>
    public required string StandardError { get; init; }
}
