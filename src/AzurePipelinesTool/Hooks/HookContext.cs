// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace AzurePipelinesTool.Hooks;

/// <summary>
/// JSON input passed to every hook via stdin.
/// </summary>
internal sealed record HookContext(
    [property: JsonPropertyName("org")] string Org,
    [property: JsonPropertyName("project")] string Project,
    [property: JsonPropertyName("pipelineId")] int PipelineId,
    [property: JsonPropertyName("pipelineName")] string PipelineName,
    [property: JsonPropertyName("ref")] string? Ref,
    [property: JsonPropertyName("buildId")] int? BuildId,
    [property: JsonPropertyName("parameters")] Dictionary<string, string> Parameters,
    [property: JsonPropertyName("variables")] Dictionary<string, string> Variables
);

/// <summary>
/// JSON output expected from a <c>pipeline_queue</c> hook.
/// </summary>
internal sealed record PipelineQueueHookResponse(
    [property: JsonPropertyName("approve")] bool Approve,
    [property: JsonPropertyName("reason")] string? Reason
);
