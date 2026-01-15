// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace PipelineMonitor.Tests;

[TestClass]
public sealed class CliWrapProcessRunnerTests
{
    [TestMethod]
    public async Task RunAsync_ExecutesCommandSuccessfully()
    {
        // Arrange
        var runner = new CliWrapProcessRunner();

        // Act - use 'echo' command which is available on all platforms
        var result = await runner.RunAsync("echo", "Hello World");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        StringAssert.Contains(result.StandardOutput, "Hello World");
        Assert.AreEqual(string.Empty, result.StandardError);
    }

    [TestMethod]
    public async Task RunAsync_CapturesNonZeroExitCode()
    {
        // Arrange
        var runner = new CliWrapProcessRunner();

        // Act - use a command that will fail
        // Use 'sh -c "exit 42"' which works on Linux/Mac
        var result = await runner.RunAsync("sh", "-c \"exit 42\"");

        // Assert
        Assert.AreEqual(42, result.ExitCode);
    }

    [TestMethod]
    public async Task RunAsync_CapturesStandardError()
    {
        // Arrange
        var runner = new CliWrapProcessRunner();

        // Act - redirect output to stderr
        // Use 'sh -c' to redirect stderr
        var result = await runner.RunAsync("sh", "-c \"echo 'Error message' >&2\"");

        // Assert
        StringAssert.Contains(result.StandardError, "Error message");
    }

    [TestMethod]
    public async Task RunAsync_UsesWorkingDirectory()
    {
        // Arrange
        var runner = new CliWrapProcessRunner();
        var workingDir = "/tmp";

        // Act - run 'pwd' command to print working directory
        var result = await runner.RunAsync("pwd", "", workingDir);

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        Assert.IsTrue(result.StandardOutput.Trim().Equals("/tmp", StringComparison.OrdinalIgnoreCase));
    }
}
