// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using AzurePipelinesTool.AzureDevOps;
using Markout;

namespace AzurePipelinesTool.Display;

/// <summary>
/// Shared formatting for timeline tree output used by both the status and wait commands.
/// </summary>
internal static class TimelineFormatter
{
    internal static string GetStatusLabel(TimelineRecordStatus state, PipelineRunResult result) =>
        state switch
        {
            TimelineRecordStatus.Completed => result switch
            {
                PipelineRunResult.Succeeded => "[OK]",
                PipelineRunResult.PartiallySucceeded => "[WARN]",
                PipelineRunResult.Failed => "[FAIL]",
                PipelineRunResult.Canceled => "[CANCEL]",
                PipelineRunResult.Skipped => "[SKIP]",
                _ => "[OK]",
            },
            TimelineRecordStatus.InProgress => "[RUNNING]",
            TimelineRecordStatus.Pending => "[PENDING]",
            _ => "[?]",
        };

    internal static string FormatSummaryLine(string pipelineName, int buildId, BuildTimelineInfo timeline)
    {
        var completedStages = timeline.Stages.Count(s => s.State == TimelineRecordStatus.Completed);
        var totalStages = timeline.Stages.Count;
        var status = GetOverallStatusLabel(timeline);

        return $"{status} Build #{buildId} | {pipelineName} | {completedStages}/{totalStages} Stages";
    }

    internal static string GetOverallStatusLabel(BuildTimelineInfo timeline)
    {
        if (timeline.Stages.Any(s => s.State == TimelineRecordStatus.InProgress))
            return "[RUNNING]";
        if (!timeline.Stages.All(s => s.State == TimelineRecordStatus.Completed))
            return "[PENDING]";

        var worstResult = timeline.Stages.Select(s => s.Result).Aggregate(PipelineRunResult.None, WorstOf);

        return worstResult switch
        {
            PipelineRunResult.Succeeded => "[OK]",
            PipelineRunResult.PartiallySucceeded => "[WARN]",
            PipelineRunResult.Failed => "[FAIL]",
            PipelineRunResult.Canceled => "[CANCEL]",
            PipelineRunResult.Skipped => "[SKIP]",
            _ => "[OK]",
        };
    }

    internal static List<TreeNode> BuildStageNodes(BuildTimelineInfo timeline, int depth) =>
        timeline.Stages.Select(stage => BuildStageNode(stage, depth)).ToList();

    internal static TreeNode BuildStageNode(TimelineStageInfo stage, int depth)
    {
        var status = GetStatusLabel(stage.State, stage.Result);
        var completedJobs = stage.Jobs.Count(j => j.State == TimelineRecordStatus.Completed);
        var totalJobs = stage.Jobs.Count;
        var logPrefix = stage.LogId is not null ? $" #{stage.LogId}" : "";
        var label = $"{status} Stage{logPrefix} | {stage.Name} | {completedJobs}/{totalJobs} Jobs";

        List<TreeNode>? children = depth >= 2 ? stage.Jobs.Select(job => BuildJobNode(job, depth)).ToList() : null;

        return new TreeNode(label, children: children);
    }

    internal static TreeNode BuildJobNode(TimelineJobInfo job, int depth)
    {
        var status = GetStatusLabel(job.State, job.Result);
        var completedTasks = job.Tasks.Count(t => t.State == TimelineRecordStatus.Completed);
        var totalTasks = job.Tasks.Count;
        var logPrefix = job.LogId is not null ? $" #{job.LogId}" : "";
        var label = $"{status} Job{logPrefix} | {job.Name} | {completedTasks}/{totalTasks} Tasks";

        List<TreeNode>? children = depth >= 3 ? job.Tasks.Select(BuildTaskNode).ToList() : null;

        return new TreeNode(label, children: children);
    }

    internal static TreeNode BuildTaskNode(TimelineTaskInfo task)
    {
        var status = GetStatusLabel(task.State, task.Result);
        var logPrefix = task.LogId is not null ? $" #{task.LogId}" : "";
        return new TreeNode($"{status} Task{logPrefix} | {task.Name}");
    }

    private static PipelineRunResult WorstOf(PipelineRunResult a, PipelineRunResult b) =>
        Severity(a) > Severity(b) ? a : b;

    private static int Severity(PipelineRunResult result) =>
        result switch
        {
            PipelineRunResult.Skipped => 0,
            PipelineRunResult.Succeeded => 1,
            PipelineRunResult.PartiallySucceeded => 2,
            PipelineRunResult.Canceled => 3,
            PipelineRunResult.Failed => 4,
            _ => -1,
        };
}
