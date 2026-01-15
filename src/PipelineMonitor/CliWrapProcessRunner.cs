// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using CliWrap;
using CliWrap.Buffered;

namespace PipelineMonitor;

/// <summary>
/// Implementation of <see cref="IProcessRunner"/> using CliWrap.
/// </summary>
public sealed class CliWrapProcessRunner : IProcessRunner
{
    /// <inheritdoc/>
    public async Task<ProcessResult> RunAsync(
        string executable,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap(executable)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None); // Don't throw on non-zero exit codes

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            command = command.WithWorkingDirectory(workingDirectory);
        }

        var result = await command.ExecuteBufferedAsync(cancellationToken);

        return new ProcessResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError
        };
    }
}
