// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using AzurePipelinesTool.AzureDevOps;
using Spectre.Console;

namespace AzurePipelinesTool.Display;

/// <summary>
/// Displays pipeline wait progress using Spectre.Console live progress bars — one per stage,
/// with job completion as the progress value.
/// </summary>
internal sealed class InteractiveWaitDisplay(IAnsiConsole ansiConsole)
{
    private const int PollIntervalSeconds = 10;

    private readonly IAnsiConsole _ansiConsole = ansiConsole;

    public async Task RunAsync(
        PipelinesService pipelinesService,
        OrganizationInfo org,
        ProjectInfo project,
        int buildId,
        string pipelineName,
        bool quiet,
        bool failOnError,
        CancellationToken cancellationToken)
    {
        var url = BuildWebUrl(org.Name, project.Name, buildId);
        _ansiConsole.MarkupLine($"{pipelineName.EscapeMarkup()} [blue][link={url}]View in browser[/][/]");
        _ansiConsole.WriteLine();

        BuildTimelineInfo? finalTimeline = null;

        await _ansiConsole.Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn())
            .StartAsync(async ctx =>
            {
                Dictionary<string, ProgressTask> tasksByStage = [];

                while (true)
                {
                    var timeline = await pipelinesService.GetBuildTimelineAsync(org, project, buildId, cancellationToken);
                    UpdateProgressTasks(ctx, tasksByStage, timeline);

                    if (timeline.Stages.All(s => s.State == TimelineRecordStatus.Completed))
                    {
                        finalTimeline = timeline;
                        return;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), cancellationToken);
                }
            });

        if (!quiet)
            _ansiConsole.Write("\a");

        if (failOnError && finalTimeline is not null && HasFailure(finalTimeline))
            Environment.ExitCode = 1;
    }

    private static void UpdateProgressTasks(
        ProgressContext ctx,
        Dictionary<string, ProgressTask> tasksByStage,
        BuildTimelineInfo timeline)
    {
        foreach (var stage in timeline.Stages)
        {
            if (!tasksByStage.TryGetValue(stage.Name, out var progressTask))
            {
                var totalJobs = Math.Max(stage.Jobs.Count, 1);
                progressTask = ctx.AddTask(stage.Name.EscapeMarkup(), maxValue: totalJobs);
                progressTask.IsIndeterminate = stage.State == TimelineRecordStatus.Pending;
                tasksByStage[stage.Name] = progressTask;
            }

            var maxValue = Math.Max(stage.Jobs.Count, 1);
            progressTask.MaxValue = maxValue;

            switch (stage.State)
            {
                case TimelineRecordStatus.Pending:
                    progressTask.IsIndeterminate = true;
                    break;

                case TimelineRecordStatus.InProgress:
                    progressTask.IsIndeterminate = false;
                    var completedJobs = stage.Jobs.Count(j => j.State == TimelineRecordStatus.Completed);
                    progressTask.Value = completedJobs;
                    break;

                case TimelineRecordStatus.Completed:
                    progressTask.IsIndeterminate = false;
                    progressTask.Value = maxValue;
                    var statusLabel = TimelineFormatter.GetStatusLabel(stage.State, stage.Result).EscapeMarkup();
                    progressTask.Description = $"{statusLabel} {stage.Name.EscapeMarkup()}";
                    break;
            }
        }
    }

    private static bool HasFailure(BuildTimelineInfo timeline) =>
        timeline.Stages.Any(s => s.Result is PipelineRunResult.Failed or PipelineRunResult.Canceled);

    internal static string BuildWebUrl(string orgName, string projectName, int buildId) =>
        $"https://dev.azure.com/{Uri.EscapeDataString(orgName)}/{Uri.EscapeDataString(projectName)}/_build/results?buildId={buildId}";
}
