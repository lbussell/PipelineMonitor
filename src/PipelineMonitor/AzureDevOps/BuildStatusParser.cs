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

        return new BuildStatusInfo(
            int.Parse(match.Groups["buildId"].Value),
            match.Groups["status"].Value,
            TimeSpan.Parse(match.Groups["duration"].Value)
        );
    }
}

/// <summary>
/// Information extracted from a build status message.
/// </summary>
public record BuildStatusInfo(int BuildId, string Status, TimeSpan Duration);
