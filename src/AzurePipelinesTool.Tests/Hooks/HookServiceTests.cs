// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using AzurePipelinesTool.Display;
using AzurePipelinesTool.Hooks;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AzurePipelinesTool.Tests.Hooks;

[TestClass]
public sealed class HookServiceTests
{
    private static HookContext MakeContext() =>
        new(
            Org: "acme",
            Project: "widgets",
            PipelineId: 123,
            PipelineName: "build-and-test",
            Ref: "refs/heads/main",
            BuildId: null,
            Parameters: [],
            Variables: []
        );

    private static (HookService Service, StringWriter Writer) CreateService(
        HooksConfig config,
        IProcessRunner? processRunner = null
    )
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            ColorSystem = ColorSystemSupport.NoColors,
        });
        var interactionService = new InteractionService(console);
        var options = Options.Create(config);
        var runner = processRunner ?? new FakeProcessRunner([]);
        var service = new HookService(options, runner, console, interactionService);
        return (service, writer);
    }

    [TestMethod]
    public async Task PipelineQueueHook_Approve_DoesNotThrow()
    {
        var config = new HooksConfig
        {
            PipelineQueue =
            [
                new HookConfig
                {
                    Name = "test-hook",
                    Command = "echo",
                    Args = [],
                    TimeoutSeconds = 10,
                },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(0, """{"approve":true}""", "")]);
        var (service, _) = CreateService(config, runner);

        await service.RunPipelineQueueHooksAsync(MakeContext());
    }

    [TestMethod]
    public async Task PipelineQueueHook_Block_ThrowsUserFacingException()
    {
        var config = new HooksConfig
        {
            PipelineQueue =
            [
                new HookConfig
                {
                    Name = "block-hook",
                    Command = "echo",
                    Args = [],
                    TimeoutSeconds = 10,
                },
            ],
        };

        var runner = new FakeProcessRunner(
            [new ProcessResult(0, """{"approve":false,"reason":"Frozen"}""", "")]
        );
        var (service, _) = CreateService(config, runner);

        var ex = await Assert.ThrowsExactlyAsync<UserFacingException>(
            () => service.RunPipelineQueueHooksAsync(MakeContext())
        );

        StringAssert.Contains(ex.Message, "block-hook");
        StringAssert.Contains(ex.Message, "Frozen");
    }

    [TestMethod]
    public async Task PipelineQueueHook_InvalidJson_OnFailureWarn_WarnsAndContinues()
    {
        var config = new HooksConfig
        {
            PipelineQueue =
            [
                new HookConfig
                {
                    Name = "bad-json-hook",
                    Command = "echo",
                    Args = [],
                    TimeoutSeconds = 10,
                    OnFailure = HookFailureAction.Warn,
                },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(0, "not json at all", "")]);
        var (service, writer) = CreateService(config, runner);

        // Should not throw; should display a warning.
        await service.RunPipelineQueueHooksAsync(MakeContext());

        StringAssert.Contains(writer.ToString(), "bad-json-hook");
    }

    [TestMethod]
    public async Task PipelineQueueHook_InvalidJson_OnFailureFail_Throws()
    {
        var config = new HooksConfig
        {
            PipelineQueue =
            [
                new HookConfig
                {
                    Name = "fail-hook",
                    Command = "echo",
                    Args = [],
                    TimeoutSeconds = 10,
                    OnFailure = HookFailureAction.Fail,
                },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(0, "not json", "")]);
        var (service, _) = CreateService(config, runner);

        await Assert.ThrowsExactlyAsync<UserFacingException>(
            () => service.RunPipelineQueueHooksAsync(MakeContext())
        );
    }

    [TestMethod]
    public async Task PipelineQueueHook_NonZeroExitCode_OnFailureWarn_WarnsAndContinues()
    {
        var config = new HooksConfig
        {
            PipelineQueue =
            [
                new HookConfig
                {
                    Name = "exit1-hook",
                    Command = "echo",
                    Args = [],
                    TimeoutSeconds = 10,
                    OnFailure = HookFailureAction.Warn,
                },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(1, "", "error")]);
        var (service, writer) = CreateService(config, runner);

        await service.RunPipelineQueueHooksAsync(MakeContext());

        StringAssert.Contains(writer.ToString(), "exit1-hook");
    }

    [TestMethod]
    public async Task PipelineCompleteHook_Runs_StdoutIgnored()
    {
        var callCount = 0;
        var config = new HooksConfig
        {
            PipelineComplete =
            [
                new HookConfig
                {
                    Name = "complete-hook",
                    Command = "echo",
                    Args = [],
                    TimeoutSeconds = 10,
                },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(0, "ignored output", "")], () => callCount++);
        var (service, _) = CreateService(config, runner);

        await service.RunPipelineCompleteHooksAsync(MakeContext());

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task PipelineSuccessHook_Runs()
    {
        var callCount = 0;
        var config = new HooksConfig
        {
            PipelineSuccess =
            [
                new HookConfig { Name = "success-hook", Command = "echo", Args = [], TimeoutSeconds = 10 },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(0, "", "")], () => callCount++);
        var (service, _) = CreateService(config, runner);

        await service.RunPipelineSuccessHooksAsync(MakeContext());

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task PipelineFailHook_Runs()
    {
        var callCount = 0;
        var config = new HooksConfig
        {
            PipelineFail =
            [
                new HookConfig { Name = "fail-hook", Command = "echo", Args = [], TimeoutSeconds = 10 },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(0, "", "")], () => callCount++);
        var (service, _) = CreateService(config, runner);

        await service.RunPipelineFailHooksAsync(MakeContext());

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task NoHooksConfigured_DoesNothing()
    {
        var (service, _) = CreateService(new HooksConfig());

        await service.RunPipelineQueueHooksAsync(MakeContext());
        await service.RunPipelineCompleteHooksAsync(MakeContext());
        await service.RunPipelineSuccessHooksAsync(MakeContext());
        await service.RunPipelineFailHooksAsync(MakeContext());
    }

    [TestMethod]
    public async Task PipelineQueueHook_NoHooks_Approves()
    {
        var (service, _) = CreateService(new HooksConfig());

        // Should not throw when no hooks configured.
        await service.RunPipelineQueueHooksAsync(MakeContext());
    }

    [TestMethod]
    public async Task PipelineQueueHook_OnFailureIgnore_Silently_Continues()
    {
        var config = new HooksConfig
        {
            PipelineQueue =
            [
                new HookConfig
                {
                    Name = "ignored-hook",
                    Command = "echo",
                    Args = [],
                    TimeoutSeconds = 10,
                    OnFailure = HookFailureAction.Ignore,
                },
            ],
        };

        var runner = new FakeProcessRunner([new ProcessResult(1, "", "error")]);
        var (service, writer) = CreateService(config, runner);

        await service.RunPipelineQueueHooksAsync(MakeContext());

        // No warning output expected.
        Assert.AreEqual("", writer.ToString().Trim());
    }

    /// <summary>
    /// A fake <see cref="IProcessRunner"/> that returns preset results in sequence.
    /// </summary>
    private sealed class FakeProcessRunner(IReadOnlyList<ProcessResult> results, Action? onCall = null)
        : IProcessRunner
    {
        private int _index;

        public Task<ProcessResult> ExecuteAsync(
            string executable,
            string arguments,
            bool allowNonZeroExitCode = false,
            CancellationToken cancellationToken = default
        )
        {
            onCall?.Invoke();
            var result = _index < results.Count ? results[_index++] : new ProcessResult(0, "", "");
            return Task.FromResult(result);
        }

        public Task<ProcessResult> ExecuteAsync(
            string executable,
            IReadOnlyList<string> arguments,
            string? stdinInput = null,
            bool allowNonZeroExitCode = false,
            CancellationToken cancellationToken = default
        )
        {
            onCall?.Invoke();
            var result = _index < results.Count ? results[_index++] : new ProcessResult(0, "", "");
            return Task.FromResult(result);
        }
    }
}
