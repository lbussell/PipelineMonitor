// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace PipelineMonitor.Tests.AzureDevOps;

[TestClass]
public class PipelineLogParserTests
{
    [TestMethod]
    public void Parse_ValidLogEntry_ReturnsCorrectInfo()
    {
        // Arrange
        var logLine = "2024-01-15T10:30:45.1234567Z [INFO] Pipeline started";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineLogParser.Parse(logLine);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(DateTimeOffset.Parse("2024-01-15T10:30:45.1234567Z"), result.Timestamp);
        Assert.AreEqual("INFO", result.Level);
        Assert.AreEqual("Pipeline started", result.Message);
    }

    [TestMethod]
    public void Parse_ErrorLevel_ReturnsCorrectInfo()
    {
        // Arrange
        var logLine = "2024-12-25T23:59:59.9999999Z [ERROR] Build failed with error code 1";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineLogParser.Parse(logLine);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(DateTimeOffset.Parse("2024-12-25T23:59:59.9999999Z"), result.Timestamp);
        Assert.AreEqual("ERROR", result.Level);
        Assert.AreEqual("Build failed with error code 1", result.Message);
    }

    [TestMethod]
    public void Parse_WarningLevel_ReturnsCorrectInfo()
    {
        // Arrange
        var logLine = "2024-06-15T12:00:00.0000000Z [WARNING] Deprecated API usage detected";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineLogParser.Parse(logLine);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("WARNING", result.Level);
        Assert.AreEqual("Deprecated API usage detected", result.Message);
    }

    [TestMethod]
    public void Parse_MessageWithSpecialCharacters_ReturnsCorrectInfo()
    {
        // Arrange
        var logLine = "2024-01-01T00:00:00.0Z [DEBUG] Processing file: /path/to/file.txt (size: 1024 bytes)";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineLogParser.Parse(logLine);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("DEBUG", result.Level);
        Assert.AreEqual("Processing file: /path/to/file.txt (size: 1024 bytes)", result.Message);
    }

    [TestMethod]
    public void Parse_InvalidLogEntry_ReturnsNull()
    {
        // Arrange
        var logLine = "Invalid log entry without proper format";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineLogParser.Parse(logLine);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNull()
    {
        // Arrange
        var logLine = string.Empty;

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineLogParser.Parse(logLine);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_InvalidTimestamp_ReturnsNull()
    {
        // Arrange - invalid date like Feb 30
        var logLine = "2024-02-30T10:30:45.1234567Z [INFO] Invalid date";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineLogParser.Parse(logLine);

        // Assert
        Assert.IsNull(result);
    }
}
