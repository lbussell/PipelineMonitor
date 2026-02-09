// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using AzurePipelinesTool.AzureDevOps;
using AzurePipelinesTool.Display;
using Markout;

namespace AzurePipelinesTool.Tests.Display;

[TestClass]
public class StatusOutputTests : VerifyBase
{
    private const string TestPipelineName = "docker-tools-imagebuilder-unofficial";
    private const int TestBuildId = 12345;

    [TestMethod]
    public Task StatusTree_Succeeded_Depth2() => Verify(RenderStatusOutput(TestData.SucceededTimeline, depth: 2));

    [TestMethod]
    public Task StatusTree_Failed_Depth2() => Verify(RenderStatusOutput(TestData.FailedTimeline, depth: 2));

    [TestMethod]
    public Task StatusTree_InProgress_Depth2() => Verify(RenderStatusOutput(TestData.InProgressTimeline, depth: 2));

    [TestMethod]
    public Task StatusTree_Succeeded_Depth1() => Verify(RenderStatusOutput(TestData.SucceededTimeline, depth: 1));

    [TestMethod]
    public Task StatusTree_Succeeded_Depth3() => Verify(RenderStatusOutput(TestData.SucceededTimeline, depth: 3));

    // ── helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the full status command output: summary paragraph + timeline tree.
    /// Mirrors the rendering logic in <see cref="AzurePipelinesTool.Commands.StatusCommand"/>.
    /// </summary>
    private static string RenderStatusOutput(BuildTimelineInfo timeline, int depth)
    {
        using var writer = new StringWriter();
        var markout = new MarkoutWriter(writer);

        writer.WriteLine(TimelineFormatter.FormatSummaryLine(TestPipelineName, TestBuildId, timeline));
        markout.WriteTree(TimelineFormatter.BuildStageNodes(timeline, depth));
        markout.Flush();

        return writer.ToString();
    }
}
