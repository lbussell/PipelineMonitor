// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace PipelineMonitor.Tests.AzureDevOps;

[TestClass]
public class BuildStatusParserTests
{
    [TestMethod]
    public void Parse_ValidStatusMessage_ReturnsCorrectInfo()
    {
        // Arrange
        var message = "Build 12345 completed: Succeeded (Duration: 00:15:32)";

        // Act
        var result = PipelineMonitor.AzureDevOps.BuildStatusParser.Parse(message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.BuildId);
        Assert.AreEqual("Succeeded", result.Status);
        Assert.AreEqual(TimeSpan.FromSeconds(932), result.Duration);
    }

    [TestMethod]
    public void Parse_FailedStatus_ReturnsCorrectInfo()
    {
        // Arrange
        var message = "Build 67890 completed: Failed (Duration: 01:23:45)";

        // Act
        var result = PipelineMonitor.AzureDevOps.BuildStatusParser.Parse(message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(67890, result.BuildId);
        Assert.AreEqual("Failed", result.Status);
        Assert.AreEqual(TimeSpan.FromSeconds(5025), result.Duration);
    }

    [TestMethod]
    public void Parse_CaseInsensitive_ReturnsCorrectInfo()
    {
        // Arrange
        var message = "build 99999 COMPLETED: PartiallySucceeded (duration: 00:00:30)";

        // Act
        var result = PipelineMonitor.AzureDevOps.BuildStatusParser.Parse(message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(99999, result.BuildId);
        Assert.AreEqual("PartiallySucceeded", result.Status);
        Assert.AreEqual(TimeSpan.FromSeconds(30), result.Duration);
    }

    [TestMethod]
    public void Parse_InvalidMessage_ReturnsNull()
    {
        // Arrange
        var message = "Invalid status message";

        // Act
        var result = PipelineMonitor.AzureDevOps.BuildStatusParser.Parse(message);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNull()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var result = PipelineMonitor.AzureDevOps.BuildStatusParser.Parse(message);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_InvalidDuration_ReturnsNull()
    {
        // Arrange - invalid time format
        var message = "Build 12345 completed: Succeeded (Duration: 99:99:99)";

        // Act
        var result = PipelineMonitor.AzureDevOps.BuildStatusParser.Parse(message);

        // Assert
        Assert.IsNull(result);
    }
}
