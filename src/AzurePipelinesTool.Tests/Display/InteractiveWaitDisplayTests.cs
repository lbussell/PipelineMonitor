// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using AzurePipelinesTool.Display;

namespace AzurePipelinesTool.Tests.Display;

[TestClass]
public class InteractiveWaitDisplayTests
{
    [TestMethod]
    public void BuildWebUrl_SimpleOrgAndProject() =>
        Assert.AreEqual(
            "https://dev.azure.com/myorg/myproject/_build/results?buildId=42",
            InteractiveWaitDisplay.BuildWebUrl("myorg", "myproject", 42));

    [TestMethod]
    public void BuildWebUrl_EncodesSpecialCharacters() =>
        Assert.AreEqual(
            "https://dev.azure.com/my%20org/my%20project/_build/results?buildId=100",
            InteractiveWaitDisplay.BuildWebUrl("my org", "my project", 100));

    [TestMethod]
    public void BuildWebUrl_RealWorldOrgAndProject() =>
        Assert.AreEqual(
            "https://dev.azure.com/dnceng/internal/_build/results?buildId=12345",
            InteractiveWaitDisplay.BuildWebUrl("dnceng", "internal", 12345));
}
