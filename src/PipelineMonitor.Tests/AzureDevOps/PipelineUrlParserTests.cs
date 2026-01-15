// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace PipelineMonitor.Tests.AzureDevOps;

[TestClass]
public class PipelineUrlParserTests
{
    [TestMethod]
    public void Parse_ValidUrl_ReturnsCorrectInfo()
    {
        // Arrange
        var url = "https://dev.azure.com/myorg/myproject/_build/results?buildId=12345";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineUrlParser.Parse(url);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("myorg", result.Organization);
        Assert.AreEqual("myproject", result.Project);
        Assert.AreEqual(12345, result.BuildId);
    }

    [TestMethod]
    public void Parse_ValidUrlCaseInsensitive_ReturnsCorrectInfo()
    {
        // Arrange
        var url = "HTTPS://DEV.AZURE.COM/MyOrg/MyProject/_build/results?buildId=67890";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineUrlParser.Parse(url);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("MyOrg", result.Organization);
        Assert.AreEqual("MyProject", result.Project);
        Assert.AreEqual(67890, result.BuildId);
    }

    [TestMethod]
    public void Parse_InvalidUrl_ReturnsNull()
    {
        // Arrange
        var url = "https://example.com/invalid";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineUrlParser.Parse(url);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_MissingBuildId_ReturnsNull()
    {
        // Arrange
        var url = "https://dev.azure.com/myorg/myproject/_build/results";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineUrlParser.Parse(url);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsNull()
    {
        // Arrange
        var url = string.Empty;

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineUrlParser.Parse(url);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Parse_BuildIdOverflow_ReturnsNull()
    {
        // Arrange - build ID larger than int.MaxValue
        var url = "https://dev.azure.com/myorg/myproject/_build/results?buildId=999999999999999999";

        // Act
        var result = PipelineMonitor.AzureDevOps.PipelineUrlParser.Parse(url);

        // Assert
        Assert.IsNull(result);
    }
}
