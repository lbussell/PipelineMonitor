// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Moq;

namespace PipelineMonitor.Tests;

[TestClass]
public sealed class GitRemoteUrlProviderTests
{
    [TestMethod]
    public async Task GetRemoteUrlAsync_ReturnsUrlWhenGitCommandSucceeds()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunner>();
        mockProcessRunner
            .Setup(p => p.RunAsync("git", "remote get-url origin", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 0,
                StandardOutput = "https://github.com/lbussell/PipelineMonitor.git\n",
                StandardError = ""
            });

        var provider = new GitRemoteUrlProvider(mockProcessRunner.Object);

        // Act
        var result = await provider.GetRemoteUrlAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://github.com/lbussell/PipelineMonitor.git", result);
    }

    [TestMethod]
    public async Task GetRemoteUrlAsync_ReturnsNullWhenGitCommandFails()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunner>();
        mockProcessRunner
            .Setup(p => p.RunAsync("git", "remote get-url origin", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 1,
                StandardOutput = "",
                StandardError = "fatal: No such remote 'origin'"
            });

        var provider = new GitRemoteUrlProvider(mockProcessRunner.Object);

        // Act
        var result = await provider.GetRemoteUrlAsync();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetRemoteUrlAsync_ReturnsNullWhenOutputIsEmpty()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunner>();
        mockProcessRunner
            .Setup(p => p.RunAsync("git", "remote get-url origin", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 0,
                StandardOutput = "",
                StandardError = ""
            });

        var provider = new GitRemoteUrlProvider(mockProcessRunner.Object);

        // Act
        var result = await provider.GetRemoteUrlAsync();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetRemoteUrlAsync_UsesCustomRemoteName()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunner>();
        mockProcessRunner
            .Setup(p => p.RunAsync("git", "remote get-url upstream", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 0,
                StandardOutput = "https://github.com/upstream/repo.git",
                StandardError = ""
            });

        var provider = new GitRemoteUrlProvider(mockProcessRunner.Object);

        // Act
        var result = await provider.GetRemoteUrlAsync("upstream");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://github.com/upstream/repo.git", result);
    }

    [TestMethod]
    public async Task GetRemoteUrlAsync_UsesWorkingDirectory()
    {
        // Arrange
        var workingDir = "/path/to/repo";
        var mockProcessRunner = new Mock<IProcessRunner>();
        mockProcessRunner
            .Setup(p => p.RunAsync("git", "remote get-url origin", workingDir, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 0,
                StandardOutput = "https://github.com/lbussell/PipelineMonitor.git",
                StandardError = ""
            });

        var provider = new GitRemoteUrlProvider(mockProcessRunner.Object);

        // Act
        var result = await provider.GetRemoteUrlAsync(workingDirectory: workingDir);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://github.com/lbussell/PipelineMonitor.git", result);
    }

    [TestMethod]
    public void Constructor_ThrowsArgumentNullException_WhenProcessRunnerIsNull()
    {
        // Act & Assert
        try
        {
            _ = new GitRemoteUrlProvider(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException ex)
        {
            Assert.AreEqual("processRunner", ex.ParamName);
        }
    }

    [TestMethod]
    public async Task GetRemoteUrlAsync_TrimsWhitespaceFromOutput()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunner>();
        mockProcessRunner
            .Setup(p => p.RunAsync("git", "remote get-url origin", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult
            {
                ExitCode = 0,
                StandardOutput = "  https://github.com/lbussell/PipelineMonitor.git  \n\r",
                StandardError = ""
            });

        var provider = new GitRemoteUrlProvider(mockProcessRunner.Object);

        // Act
        var result = await provider.GetRemoteUrlAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://github.com/lbussell/PipelineMonitor.git", result);
    }
}
