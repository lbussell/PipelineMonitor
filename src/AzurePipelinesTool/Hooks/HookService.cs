// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text.Json;
using AzurePipelinesTool.Display;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AzurePipelinesTool.Hooks;

/// <summary>
/// Executes configured lifecycle hooks and handles their output.
/// </summary>
internal sealed class HookService(
    IOptions<HooksConfig> hooksConfig,
    IProcessRunner processRunner,
    IAnsiConsole ansiConsole,
    InteractionService interactionService
)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HooksConfig _hooksConfig = hooksConfig.Value;
    private readonly IProcessRunner _processRunner = processRunner;
    private readonly IAnsiConsole _ansiConsole = ansiConsole;
    private readonly InteractionService _interactionService = interactionService;

    /// <summary>
    /// Runs all configured <c>pipeline_queue</c> hooks. Throws <see cref="UserFacingException"/> if
    /// any hook blocks the pipeline run.
    /// </summary>
    public async Task RunPipelineQueueHooksAsync(HookContext context, CancellationToken cancellationToken = default)
    {
        foreach (var hook in _hooksConfig.PipelineQueue)
        {
            var stdinJson = JsonSerializer.Serialize(context);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(hook.TimeoutSeconds));

            ProcessResult result;
            try
            {
                result = await _processRunner.ExecuteAsync(
                    hook.Command,
                    hook.Args,
                    stdinInput: stdinJson,
                    allowNonZeroExitCode: true,
                    cancellationToken: cts.Token
                );
            }
            catch (Exception ex) when (ex is not UserFacingException)
            {
                HandleHookExecutionFailure(hook, ex.Message);
                continue;
            }

            if (result.ExitCode != 0)
            {
                HandleHookExecutionFailure(hook, $"exited with code {result.ExitCode}");
                continue;
            }

            PipelineQueueHookResponse? response;
            try
            {
                response = JsonSerializer.Deserialize<PipelineQueueHookResponse>(
                    result.StandardOutput,
                    JsonOptions
                );
            }
            catch (JsonException)
            {
                response = null;
            }

            if (response is null)
            {
                // Valid exit code but stdout is not parseable JSON — treat as execution failure.
                HandleHookExecutionFailure(hook, "hook produced invalid or missing JSON output");
                continue;
            }

            if (!response.Approve)
            {
                var reason = string.IsNullOrWhiteSpace(response.Reason)
                    ? string.Empty
                    : $" Reason: {response.Reason}";
                throw new UserFacingException(
                    $"Pipeline queuing blocked by hook '{hook.Name}'.{reason}"
                );
            }
        }
    }

    /// <summary>
    /// Runs all configured <c>pipeline_complete</c> hooks. Stdout is ignored.
    /// </summary>
    public Task RunPipelineCompleteHooksAsync(HookContext context, CancellationToken cancellationToken = default) =>
        RunNotificationHooksAsync(_hooksConfig.PipelineComplete, context, cancellationToken);

    /// <summary>
    /// Runs all configured <c>pipeline_success</c> hooks. Stdout is ignored.
    /// </summary>
    public Task RunPipelineSuccessHooksAsync(HookContext context, CancellationToken cancellationToken = default) =>
        RunNotificationHooksAsync(_hooksConfig.PipelineSuccess, context, cancellationToken);

    /// <summary>
    /// Runs all configured <c>pipeline_fail</c> hooks. Stdout is ignored.
    /// </summary>
    public Task RunPipelineFailHooksAsync(HookContext context, CancellationToken cancellationToken = default) =>
        RunNotificationHooksAsync(_hooksConfig.PipelineFail, context, cancellationToken);

    private async Task RunNotificationHooksAsync(
        IReadOnlyList<HookConfig> hooks,
        HookContext context,
        CancellationToken cancellationToken
    )
    {
        foreach (var hook in hooks)
        {
            var stdinJson = JsonSerializer.Serialize(context);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(hook.TimeoutSeconds));

            ProcessResult result;
            try
            {
                result = await _processRunner.ExecuteAsync(
                    hook.Command,
                    hook.Args,
                    stdinInput: stdinJson,
                    allowNonZeroExitCode: true,
                    cancellationToken: cts.Token
                );
            }
            catch (Exception ex) when (ex is not UserFacingException)
            {
                HandleHookExecutionFailure(hook, ex.Message);
                continue;
            }

            if (result.ExitCode != 0)
                HandleHookExecutionFailure(hook, $"exited with code {result.ExitCode}");
        }
    }

    private void HandleHookExecutionFailure(HookConfig hook, string reason)
    {
        var message = $"Hook '{hook.Name}' failed: {reason}.";

        switch (hook.OnFailure)
        {
            case HookFailureAction.Fail:
                throw new UserFacingException(message);
            case HookFailureAction.Warn:
                _interactionService.DisplayWarning(message);
                break;
            case HookFailureAction.Ignore:
            default:
                break;
        }
    }
}
