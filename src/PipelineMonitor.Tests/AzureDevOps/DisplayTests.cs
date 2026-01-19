// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using PipelineMonitor.AzureDevOps;
using Spectre.Console.Rendering;

namespace PipelineMonitor.Tests.AzureDevOps;

[TestClass]
public class DisplayTests
{
    [TestMethod]
    public void RunDetails_WithBracketsInCommitMessage_DoesNotThrow()
    {
        var run = new PipelineRunInfo(
            Name: "Build Pipeline",
            Id: new RunId(123),
            State: "completed",
            Result: PipelineRunResult.Succeeded,
            Started: DateTimeOffset.UtcNow,
            Finished: DateTimeOffset.UtcNow.AddMinutes(5),
            Commit: new CommitInfo(
                Sha: "abc123def456",
                Message: "Merge branch 'main' into feature-branch",
                Author: "Test User",
                Date: DateTime.UtcNow
            ),
            Url: "https://dev.azure.com/org/project/_build/results?buildId=123",
            Stages: []
        );

        var result = run.RunDetails;
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void RunDetails_WithSquareBracketsInCommitMessage_DoesNotThrow()
    {
        var run = new PipelineRunInfo(
            Name: "Build Pipeline [CI]",
            Id: new RunId(456),
            State: "completed",
            Result: PipelineRunResult.Succeeded,
            Started: DateTimeOffset.UtcNow,
            Finished: DateTimeOffset.UtcNow.AddMinutes(5),
            Commit: new CommitInfo(
                Sha: "xyz789abc012",
                Message: "Fix issue [#123] by updating [main] branch",
                Author: "Test User",
                Date: DateTime.UtcNow
            ),
            Url: "https://dev.azure.com/org/project/_build/results?buildId=456",
            Stages: []
        );

        var result = run.RunDetails;
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SingleLineDisplay_WithBracketsInPipelineName_DoesNotThrow()
    {
        var pipeline = new LocalPipelineInfo(
            Name: "Build Pipeline [Production]",
            DefinitionFile: new FileInfo("/path/to/azure-pipelines.yml"),
            Id: new PipelineId(789),
            RelativePath: "path/to/[bracketed]/azure-pipelines.yml",
            Organization: new OrganizationInfo("TestOrg", new Uri("https://dev.azure.com/TestOrg")),
            Project: new ProjectInfo("TestProject"),
            Repository: new RepositoryInfo("TestRepo", Guid.NewGuid())
        );

        var result = pipeline.SingleLineDisplay;
        Assert.IsNotNull(result);
    }
}
