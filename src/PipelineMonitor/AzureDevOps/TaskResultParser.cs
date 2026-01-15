// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;

namespace PipelineMonitor.AzureDevOps;

/// <summary>
/// Parses Azure DevOps task result messages using source-generated regex.
/// </summary>
public partial class TaskResultParser
{
    /// <summary>
    /// Regex pattern for task result messages.
    /// Example: "Task 'RunTests' completed with result: Succeeded (Exit code: 0)"
    /// </summary>
    [GeneratedRegex(@"^Task\s+'(?<taskName>[^']+)'\s+completed\s+with\s+result:\s+(?<result>\w+)\s+\(Exit\s+code:\s+(?<exitCode>-?\d+)\)$")]
    private static partial Regex TaskResultRegex();

    /// <summary>
    /// Parses a task result message and extracts task name, result, and exit code.
    /// </summary>
    public static TaskResult? Parse(string resultMessage)
    {
        var match = TaskResultRegex().Match(resultMessage);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["exitCode"].Value, out var exitCode))
        {
            return null;
        }

        return new TaskResult(
            match.Groups["taskName"].Value,
            match.Groups["result"].Value,
            exitCode
        );
    }
}

/// <summary>
/// Information extracted from a task result message.
/// </summary>
public record TaskResult(string TaskName, string Result, int ExitCode);
