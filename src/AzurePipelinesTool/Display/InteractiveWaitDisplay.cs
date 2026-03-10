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
        var statusColumn = new StatusTextColumn();

        await _ansiConsole.Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                elapsedColumn,
                statusColumn)
            .StartAsync(async ctx =>
            {
                Dictionary<string, ProgressTask> tasksByStage = [];

                while (true)
                {
                    var timeline = await pipelinesService.GetBuildTimelineAsync(org, project, buildId, cancellationToken);
                    UpdateProgressTasks(ctx, elapsedColumn, statusColumn, tasksByStage, timeline);

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
        StatusTextColumn statusColumn,
        Dictionary<string, ProgressTask> tasksByStage,
        BuildTimelineInfo timeline)
    {
        foreach (var stage in timeline.Stages)
        {
            var completedJobs = stage.Jobs.Count(j => j.State == TimelineRecordStatus.Completed);
            var totalJobCount = stage.Jobs.Count;

            // For single-job stages with tasks available, track individual task progress
            // instead of the binary 0/1 job-level progress.
            var singleJob = totalJobCount == 1 && stage.Jobs[0].Tasks.Count > 0 ? stage.Jobs[0] : null;
            var completed = singleJob is not null
                ? singleJob.Tasks.Count(t => t.State == TimelineRecordStatus.Completed)
                : completedJobs;
            var total = singleJob is not null
                ? singleJob.Tasks.Count
                : totalJobCount;
            var escapedName = stage.Name.EscapeMarkup();

            if (!tasksByStage.TryGetValue(stage.Name, out var progressTask))
            {
                var maxValue = Math.Max(total, 1);
                progressTask = ctx.AddTask(FormatDescription(escapedName, completed, total), autoStart: false, maxValue: maxValue);

                var initialJobStart = GetEarliestJobStartTime(stage);
                if (initialJobStart.HasValue)
                {
                    elapsedColumn.SetOffset(progressTask, DateTime.UtcNow - initialJobStart.Value.ToUniversalTime());
                    progressTask.StartTask();
                }

                tasksByStage[stage.Name] = progressTask;
            }

            progressTask.MaxValue = Math.Max(total, 1);

            switch (stage.State)
            {
                case TimelineRecordStatus.Pending:
                    progressTask.Description = FormatDescription(escapedName, completed, total);
                    statusColumn.SetStatus(progressTask, "[dim]Not started[/]");
                    break;

                case TimelineRecordStatus.InProgress:
                    progressTask.IsIndeterminate = false;
                    if (!progressTask.IsStarted)
                    {
                        var earliestJobStart = GetEarliestJobStartTime(stage);
                        if (earliestJobStart.HasValue)
                        {
                            elapsedColumn.SetOffset(progressTask, DateTime.UtcNow - earliestJobStart.Value.ToUniversalTime());
                            progressTask.StartTask();
                        }
                    }
                    progressTask.Value = completed;
                    progressTask.Description = FormatDescription(escapedName, completed, total);
                    var isWaitingForAgent = stage.Jobs.All(j => j.State == TimelineRecordStatus.Pending);
                    statusColumn.SetStatus(progressTask, isWaitingForAgent
                        ? "[dim]Waiting for build agent[/]"
                        : "[dim]Running...[/]");
                    break;

                case TimelineRecordStatus.Completed:
                    progressTask.IsIndeterminate = false;
                    var completedJobStart = GetEarliestJobStartTime(stage);
                    if (!progressTask.IsStarted)
                    {
                        if (completedJobStart.HasValue)
                            elapsedColumn.SetOffset(progressTask, DateTime.UtcNow - completedJobStart.Value.ToUniversalTime());
                        progressTask.StartTask();
                    }
                    progressTask.Value = progressTask.MaxValue;
                    var isFailure = stage.Result is PipelineRunResult.Failed or PipelineRunResult.Canceled;
                    var stageLabel = isFailure ? $"[red]{escapedName}[/]" : escapedName;
                    progressTask.Description = FormatDescription(stageLabel, completed, total);
                    // For completed stages, override the offset to show exact execution duration
                    var effectiveStart = completedJobStart ?? stage.StartTime;
                    if (effectiveStart.HasValue && stage.FinishTime.HasValue)
                        elapsedColumn.SetOffset(progressTask, stage.FinishTime.Value - effectiveStart.Value);
                    statusColumn.SetStatus(progressTask, GetCompletedStatusMarkup(stage.Result));
                    progressTask.StopTask();
                    break;
            }
        }
    }

    private static string FormatDescription(string stageLabel, int completed, int total) =>
        $"{stageLabel} [dim]{completed}/{total}[/]";

    /// <summary>
    /// Returns the earliest <see cref="TimelineJobInfo.StartTime"/> among a stage's jobs,
    /// or <c>null</c> if no job has started yet (e.g. still waiting for an agent).
    /// </summary>
    private static DateTime? GetEarliestJobStartTime(TimelineStageInfo stage) =>
        stage.Jobs.Min(j => j.StartTime);

    private static string GetCompletedStatusMarkup(PipelineRunResult result) => result switch
    {
        PipelineRunResult.Succeeded => "[dim]Succeeded[/]",
        PipelineRunResult.PartiallySucceeded => "[dim]Succeeded with issues[/]",
        PipelineRunResult.Failed => "[red]Failed[/]",
        PipelineRunResult.Canceled => "[dim]Canceled[/]",
        _ => "[dim]Completed[/]",
    };

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
