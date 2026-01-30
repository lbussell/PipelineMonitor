// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using PipelineMonitor.AzureDevOps;

namespace PipelineMonitor.Commands;

internal static class CommandHelpers
{
    public static async Task<LocalPipelineInfo?> GetLocalPipelineAsync(
        string definitionPath,
        IInteractionService interactionService,
        PipelinesService pipelinesService)
    {
        var pipelines = await GetLocalPipelinesAsync(interactionService, pipelinesService);
        var pipelineFile = new FileInfo(definitionPath);

        if (!pipelineFile.Exists)
        {
            interactionService.DisplayError($"Definition file '{definitionPath}' does not exist.");
            return null;
        }

        var matchingPipelines = pipelines
            .Where(pipeline =>
                pipeline.DefinitionFile.FullName.Equals(pipelineFile.FullName))
            .ToList();

        var pipelineInfo = matchingPipelines.FirstOrDefault();

        if (matchingPipelines.Count > 1)
            pipelineInfo = await interactionService.SelectAsync(
                "Multiple pipelines found for the specified definition file. Please select one:",
                matchingPipelines,
                pipeline => pipeline.Name);

        if (pipelineInfo is null)
            interactionService.DisplayError($"No pipeline found for definition file '{definitionPath}'.");

        return pipelineInfo;
    }

    private static async Task<IEnumerable<LocalPipelineInfo>> GetLocalPipelinesAsync(
        IInteractionService interactionService,
        PipelinesService pipelinesService)
    {
        var pipelinesTask = pipelinesService
            .GetLocalPipelinesAsync()
            .ToListAsync()
            .AsTask();

        List<LocalPipelineInfo> pipelines = await interactionService
            .ShowStatusAsync("Loading Pipelines...", () => pipelinesTask);

        return pipelines;
    }
}
