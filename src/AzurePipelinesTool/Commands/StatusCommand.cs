// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using AzurePipelinesTool.AzureDevOps;
using AzurePipelinesTool.Display;
using ConsoleAppFramework;
using Markout;
using Spectre.Console;

namespace AzurePipelinesTool.Commands;

internal sealed class StatusCommand(
    IAnsiConsole ansiConsole,
    InteractionService interactionService,
    PipelinesService pipelinesService,
    BuildIdResolver buildIdResolver
)
{
    private readonly IAnsiConsole _ansiConsole = ansiConsole;
    private readonly InteractionService _interactionService = interactionService;
    private readonly PipelinesService _pipelinesService = pipelinesService;
    private readonly BuildIdResolver _buildIdResolver = buildIdResolver;

    /// <summary>
    /// Show the status of a pipeline run.
    /// </summary>
    /// <param name="buildIdOrUrl">Build ID or Azure DevOps build results URL.</param>
    /// <param name="depth">-d, Tree nesting depth: 1=stages, 2=stages+jobs, 3=stages+jobs+tasks.</param>
    [Command("status")]
    public async Task ExecuteAsync([Argument] string buildIdOrUrl, int depth = 2)
    {
        if (depth is < 1 or > 3)
            throw new UserFacingException("--depth must be between 1 and 3.");

        var (org, project, buildId) = await _buildIdResolver.ResolveAsync(buildIdOrUrl);

        var summaryTask = _pipelinesService.GetBuildSummaryAsync(org, project, buildId);
        var timelineTask = _pipelinesService.GetBuildTimelineAsync(org, project, buildId);
        var summary = await summaryTask;
        var timeline = await timelineTask;

        var textWriter = _ansiConsole.Profile.Out.Writer;
        var writer = new MarkoutWriter(textWriter);
        WriteSummary(textWriter, summary.PipelineName, timeline, buildId);
        WriteTimelineTree(writer, timeline, depth);
        writer.Flush();
    }

    private void WriteSummary(TextWriter writer, string pipelineName, BuildTimelineInfo timeline, int buildId)
    {
        writer.WriteLine(TimelineFormatter.FormatSummaryLine(pipelineName, buildId, timeline));

        var isRunning = timeline.Stages.Any(s =>
            s.State is TimelineRecordStatus.InProgress or TimelineRecordStatus.Pending
        );
        if (isRunning)
            _interactionService.DisplaySubtleMessage($"To cancel, run: `cancel {buildId}`");
    }

    private static void WriteTimelineTree(MarkoutWriter writer, BuildTimelineInfo timeline, int depth)
    {
        writer.WriteTree(TimelineFormatter.BuildStageNodes(timeline, depth));
    }
}
