// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace PipelineMonitor.Tests.AzureDevOps;

[TestClass]
public class TaskResultParserTests
{
    [TestMethod]
    public void Parse_ValidTaskResult_ReturnsCorrectInfo()
    {
        // Arrange
        var message = "Task 'RunTests' completed with result: Succeeded (Exit code: 0)";

        // Act
        var result = PipelineMonitor.AzureDevOps.TaskResultParser.Parse(message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("RunTests", result.TaskName);
        Assert.AreEqual("Succeeded", result.Result);
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public void Parse_FailedTask_ReturnsCorrectInfo()
    {
        // Arrange
        var message = "Task 'BuildProject' completed with result: Failed (Exit code: 1)";

        // Act
        var result = PipelineMonitor.AzureDevOps.TaskResultParser.Parse(message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("BuildProject", result.TaskName);
        Assert.AreEqual("Failed", result.Result);
        Assert.AreEqual(1, result.ExitCode);
    }

    [TestMethod]
    public void Parse_NegativeExitCode_ReturnsCorrectInfo()
    {
        // Arrange
        var message = "Task 'Deploy' completed with result: Cancelled (Exit code: -1)";

        // Act
        var result = PipelineMonitor.AzureDevOps.TaskResultParser.Parse(message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Deploy", result.TaskName);
        Assert.AreEqual("Cancelled", result.Result);
        Assert.AreEqual(-1, result.ExitCode);
    }

    [TestMethod]
    public void Parse_TaskNameWithSpaces_ReturnsCorrectInfo()
    {
        // Arrange
        var message = "Task 'Run Integration Tests' completed with result: PartiallySucceeded (Exit code: 0)";

        // Act
        var result = PipelineMonitor.AzureDevOps.TaskResultParser.Parse(message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Run Integration Tests", result.TaskName);
        Assert.AreEqual("PartiallySucceeded", result.Result);
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public void Parse_InvalidMessage_ReturnsNull()
    {
        // Arrange
        var message = "Invalid task result message";

        // Act
        var result = PipelineMonitor.AzureDevOps.TaskResultParser.Parse(message);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNull()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var result = PipelineMonitor.AzureDevOps.TaskResultParser.Parse(message);

        // Assert
        Assert.IsNull(result);
    }
}
