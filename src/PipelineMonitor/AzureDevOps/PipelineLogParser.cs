// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;

namespace PipelineMonitor.AzureDevOps;

/// <summary>
/// Parses Azure DevOps pipeline log entries using source-generated regex.
/// </summary>
public partial class PipelineLogParser
{
    /// <summary>
    /// Regex pattern for timestamped log entries.
    /// Example: "2024-01-15T10:30:45.1234567Z [INFO] Pipeline started"
    /// </summary>
    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z)\s+\[(?<level>\w+)\]\s+(?<message>.+)$")]
    private static partial Regex LogEntryRegex();

    /// <summary>
    /// Parses a log entry and extracts timestamp, log level, and message.
    /// </summary>
    public static LogEntry? Parse(string logLine)
    {
        var match = LogEntryRegex().Match(logLine);
        if (!match.Success)
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(match.Groups["timestamp"].Value, out var timestamp))
        {
            return null;
        }

        return new LogEntry(
            timestamp,
            match.Groups["level"].Value,
            match.Groups["message"].Value
        );
    }
}

/// <summary>
/// Information extracted from a pipeline log entry.
/// </summary>
public record LogEntry(DateTimeOffset Timestamp, string Level, string Message);
