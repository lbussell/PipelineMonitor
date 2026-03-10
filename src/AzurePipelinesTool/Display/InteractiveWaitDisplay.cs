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
        SetConsoleTitle($"⏳ {pipelineName}");
        _ansiConsole.WriteLine();
        _ansiConsole.MarkupLine($"{pipelineName.EscapeMarkup()} ([blue][link={url}]View in browser[/][/])");

        BuildTimelineInfo? finalTimeline = null;
        var elapsedColumn = new OffsetElapsedTimeColumn();

        await _ansiConsole.Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                elapsedColumn)
            .StartAsync(async ctx =>
            {
                Dictionary<string, ProgressTask> tasksByStage = [];

                while (true)
                {
                    var timeline = await pipelinesService.GetBuildTimelineAsync(org, project, buildId, cancellationToken);
                    UpdateProgressTasks(ctx, elapsedColumn, tasksByStage, timeline);

                    if (timeline.Stages.All(s => s.State == TimelineRecordStatus.Completed))
                    {
                        finalTimeline = timeline;
                        return;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), cancellationToken);
                }
            });

        var hasFailure = finalTimeline is not null && HasFailure(finalTimeline);
        SetConsoleTitle($"{(hasFailure ? "❌" : "✅")} {pipelineName}");

        if (!quiet)
            _ansiConsole.Write("\a");

        if (failOnError && hasFailure)
            Environment.ExitCode = 1;
    }

    private static void UpdateProgressTasks(
        ProgressContext ctx,
        OffsetElapsedTimeColumn elapsedColumn,
        Dictionary<string, ProgressTask> tasksByStage,
        BuildTimelineInfo timeline)
    {
        foreach (var stage in timeline.Stages)
        {
            var completedJobs = stage.Jobs.Count(j => j.State == TimelineRecordStatus.Completed);
            var totalJobCount = stage.Jobs.Count;
            var escapedName = stage.Name.EscapeMarkup();

            if (!tasksByStage.TryGetValue(stage.Name, out var progressTask))
            {
                var maxValue = Math.Max(totalJobCount, 1);
                progressTask = ctx.AddTask(FormatDescription(escapedName, completedJobs, totalJobCount), autoStart: false, maxValue: maxValue);

                if (stage.StartTime.HasValue)
                {
                    // Offset the elapsed timer so it reflects actual pipeline duration
                    elapsedColumn.SetOffset(progressTask, DateTime.UtcNow - stage.StartTime.Value.ToUniversalTime());
                    progressTask.StartTask();
                }

                tasksByStage[stage.Name] = progressTask;
            }

            progressTask.MaxValue = Math.Max(totalJobCount, 1);

            switch (stage.State)
            {
                case TimelineRecordStatus.Pending:
                    progressTask.Description = FormatDescription(escapedName, completedJobs, totalJobCount);
                    break;

                case TimelineRecordStatus.InProgress:
                    progressTask.IsIndeterminate = false;
                    if (!progressTask.IsStarted)
                    {
                        if (stage.StartTime.HasValue)
                            elapsedColumn.SetOffset(progressTask, DateTime.UtcNow - stage.StartTime.Value.ToUniversalTime());
                        progressTask.StartTask();
                    }
                    progressTask.Value = completedJobs;
                    progressTask.Description = FormatDescription(escapedName, completedJobs, totalJobCount);
                    break;

                case TimelineRecordStatus.Completed:
                    progressTask.IsIndeterminate = false;
                    if (!progressTask.IsStarted)
                    {
                        if (stage.StartTime.HasValue)
                            elapsedColumn.SetOffset(progressTask, DateTime.UtcNow - stage.StartTime.Value.ToUniversalTime());
                        progressTask.StartTask();
                    }
                    progressTask.Value = progressTask.MaxValue;
                    var isFailure = stage.Result is PipelineRunResult.Failed or PipelineRunResult.Canceled;
                    var stageLabel = isFailure ? $"[red]{escapedName}[/]" : escapedName;
                    progressTask.Description = FormatDescription(stageLabel, completedJobs, totalJobCount);
                    // For completed stages, override the offset to show exact duration
                    if (stage.StartTime.HasValue && stage.FinishTime.HasValue)
                        elapsedColumn.SetOffset(progressTask, stage.FinishTime.Value - stage.StartTime.Value);
                    progressTask.StopTask();
                    break;
            }
        }
    }

    private static string FormatDescription(string stageLabel, int completedJobs, int totalJobs) =>
        $"{stageLabel} [dim]{completedJobs}/{totalJobs}[/]";

    private static bool HasFailure(BuildTimelineInfo timeline) =>
        timeline.Stages.Any(s => s.Result is PipelineRunResult.Failed or PipelineRunResult.Canceled);

    internal static string BuildWebUrl(string orgName, string projectName, int buildId) =>
        $"https://dev.azure.com/{Uri.EscapeDataString(orgName)}/{Uri.EscapeDataString(projectName)}/_build/results?buildId={buildId}";

    /// <summary>
    /// Sets the terminal window title using the OSC escape sequence.
    /// </summary>
    private static void SetConsoleTitle(string title) =>
        Console.Write($"\x1b]0;{title}\x07");
}
