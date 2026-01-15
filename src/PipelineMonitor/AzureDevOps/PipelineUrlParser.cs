// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;

namespace PipelineMonitor.AzureDevOps;

/// <summary>
/// Parses Azure DevOps pipeline URLs using source-generated regex.
/// </summary>
public partial class PipelineUrlParser
{
    /// <summary>
    /// Regex pattern for Azure DevOps pipeline URLs.
    /// Example: https://dev.azure.com/org/project/_build/results?buildId=12345
    /// </summary>
    [GeneratedRegex(@"^https://dev\.azure\.com/(?<organization>[^/]+)/(?<project>[^/]+)/_build/results\?buildId=(?<buildId>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex PipelineUrlRegex();

    /// <summary>
    /// Parses a pipeline URL and extracts organization, project, and build ID.
    /// </summary>
    public static PipelineUrlInfo? Parse(string url)
    {
        var match = PipelineUrlRegex().Match(url);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["buildId"].Value, out var buildId))
        {
            return null;
        }

        return new PipelineUrlInfo(
            match.Groups["organization"].Value,
            match.Groups["project"].Value,
            buildId
        );
    }
}

/// <summary>
/// Information extracted from an Azure DevOps pipeline URL.
/// </summary>
public record PipelineUrlInfo(string Organization, string Project, int BuildId);
