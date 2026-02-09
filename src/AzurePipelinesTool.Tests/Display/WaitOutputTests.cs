// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using AzurePipelinesTool.AzureDevOps;
using AzurePipelinesTool.Commands;
using AzurePipelinesTool.Display;

namespace AzurePipelinesTool.Tests.Display;

[TestClass]
public class WaitOutputTests : VerifyBase
{
    private const string TestPipelineName = "docker-tools-imagebuilder-unofficial";

    [TestMethod]
    public void FormatElapsed_Seconds() => Assert.AreEqual("42s", WaitCommand.FormatElapsed(TimeSpan.FromSeconds(42)));

    [TestMethod]
    public void FormatElapsed_Minutes() =>
        Assert.AreEqual("5m 30s", WaitCommand.FormatElapsed(TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(30))));

    [TestMethod]
    public void FormatElapsed_Hours() =>
        Assert.AreEqual(
            "2h 15m 10s",
            WaitCommand.FormatElapsed(TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(10))))
        );

    [TestMethod]
    public void FormatElapsed_Zero() => Assert.AreEqual("0s", WaitCommand.FormatElapsed(TimeSpan.Zero));

    [TestMethod]
    public void FormatElapsed_ExactlyOneMinute() =>
        Assert.AreEqual("1m 0s", WaitCommand.FormatElapsed(TimeSpan.FromMinutes(1)));

    [TestMethod]
    public Task WaitProgress_SucceededTimeline() =>
        Verify(
            RenderWaitProgress(
                TestData.SucceededTimeline,
                buildId: 12345,
                TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(42))
            )
        );

    [TestMethod]
    public Task WaitProgress_InProgressTimeline() =>
        Verify(RenderWaitInProgress(TestData.InProgressTimeline, TimeSpan.FromSeconds(45), nextCheckSeconds: 10));

    // ── helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the final wait summary for a completed timeline.
    /// Mirrors <see cref="WaitCommand.DisplayFinalSummary"/>.
    /// </summary>
    private static string RenderWaitProgress(BuildTimelineInfo timeline, int buildId, TimeSpan elapsed)
    {
        using var writer = new StringWriter();

        var summaryLine = TimelineFormatter.FormatSummaryLine(TestPipelineName, buildId, timeline);
        writer.WriteLine($"{summaryLine} - {WaitCommand.FormatElapsed(elapsed)} elapsed");
        writer.WriteLine();

        foreach (var stage in timeline.Stages)
        {
            var status = TimelineFormatter.GetStatusLabel(stage.State, stage.Result);
            var completedJobs = stage.Jobs.Count(j => j.State == TimelineRecordStatus.Completed);
            var totalJobs = stage.Jobs.Count;
            var logPrefix = stage.LogId is not null ? $" #{stage.LogId}" : "";
            writer.WriteLine($"Stage{logPrefix}: {stage.Name} {status} (Jobs: {completedJobs}/{totalJobs})");
        }

        return writer.ToString();
    }

    /// <summary>
    /// Renders the in-progress wait status display.
    /// Mirrors <see cref="WaitCommand.DisplayTimelineProgress"/>.
    /// </summary>
    private static string RenderWaitInProgress(BuildTimelineInfo timeline, TimeSpan elapsed, int nextCheckSeconds)
    {
        using var writer = new StringWriter();

        writer.WriteLine(FormatStatusCounts("Stages", timeline.Stages.Select(s => s.State)));
        writer.WriteLine(FormatStatusCounts("Jobs", timeline.Stages.SelectMany(s => s.Jobs).Select(j => j.State)));
        writer.WriteLine($"Elapsed: {WaitCommand.FormatElapsed(elapsed)}. Next check in {nextCheckSeconds}s...");

        return writer.ToString();
    }

    private static string FormatStatusCounts(string label, IEnumerable<TimelineRecordStatus> states)
    {
        var counts = states.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        List<string> parts = [];
        if (counts.TryGetValue(TimelineRecordStatus.Completed, out var completed))
            parts.Add($"{completed} completed");
        if (counts.TryGetValue(TimelineRecordStatus.InProgress, out var inProgress))
            parts.Add($"{inProgress} in progress");
        if (counts.TryGetValue(TimelineRecordStatus.Pending, out var pending))
            parts.Add($"{pending} pending");

        return $"{label}: {string.Join(", ", parts)}";
    }
}
