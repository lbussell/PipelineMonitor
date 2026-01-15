// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;

namespace PipelineMonitor.AzureDevOps;

/// <summary>
/// Parses Azure DevOps build status messages using source-generated regex.
/// </summary>
public partial class BuildStatusParser
{
    /// <summary>
    /// Regex pattern for build status lines.
    /// Example: "Build 12345 completed: Succeeded (Duration: 00:15:32)"
    /// </summary>
    [GeneratedRegex(@"^Build\s+(?<buildId>\d+)\s+completed:\s+(?<status>\w+)\s+\(Duration:\s+(?<duration>[\d:]+)\)$", RegexOptions.IgnoreCase)]
    private static partial Regex BuildStatusRegex();

    /// <summary>
    /// Parses a build status message and extracts build ID, status, and duration.
    /// </summary>
    public static BuildStatusInfo? Parse(string statusMessage)
    {
        var match = BuildStatusRegex().Match(statusMessage);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["buildId"].Value, out var buildId))
        {
            return null;
        }

        if (!TimeSpan.TryParse(match.Groups["duration"].Value, out var duration))
        {
            return null;
        }

        return new BuildStatusInfo(
            buildId,
            match.Groups["status"].Value,
            duration
        );
    }
}

/// <summary>
/// Information extracted from a build status message.
/// </summary>
public record BuildStatusInfo(int BuildId, string Status, TimeSpan Duration);
