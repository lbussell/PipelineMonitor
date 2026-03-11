// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace AzurePipelinesTool.Hooks;

/// <summary>
/// Defines what happens when a hook execution fails.
/// </summary>
internal enum HookFailureAction
{
    /// <summary>Log a warning and continue.</summary>
    Warn,

    /// <summary>Treat as a hard failure (block for pipeline_queue; abort for others).</summary>
    Fail,

    /// <summary>Silently ignore the failure and continue.</summary>
    Ignore,
}

/// <summary>
/// Configuration for a single hook.
/// </summary>
internal sealed class HookConfig
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 30;
    public HookFailureAction OnFailure { get; set; } = HookFailureAction.Warn;
}

/// <summary>
/// Configuration for all lifecycle hooks, read from the <c>hooks</c> section of config.json.
/// </summary>
internal sealed class HooksConfig
{
    /// <summary>Hooks that run before a pipeline is queued. May block queuing.</summary>
    public List<HookConfig> PipelineQueue { get; set; } = [];

    /// <summary>Hooks that always run when a watched pipeline finishes.</summary>
    public List<HookConfig> PipelineComplete { get; set; } = [];

    /// <summary>Hooks that run when a watched pipeline succeeds.</summary>
    public List<HookConfig> PipelineSuccess { get; set; } = [];

    /// <summary>Hooks that run when a watched pipeline fails or is canceled.</summary>
    public List<HookConfig> PipelineFail { get; set; } = [];
}
