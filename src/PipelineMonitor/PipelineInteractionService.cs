// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PipelineMonitor.AzureDevOps;

namespace PipelineMonitor;

internal interface IPipelineInteractionService
{
    Task<LocalPipelineInfo?> GetLocalPipelineAsync(string definitionPath);
}

internal sealed class PipelineInteractionService(
    IInteractionService interactionService,
    PipelinesService pipelinesService,
    ILogger<PipelineInteractionService> logger)
    : IPipelineInteractionService
{
    public async Task<LocalPipelineInfo?> GetLocalPipelineAsync(string definitionPath)
    {
        logger.LogTrace("GetLocalPipelineAsync called with definitionPath: {DefinitionPath}", definitionPath);

        var pipelines = await GetLocalPipelinesAsync();
        var pipelineFile = new FileInfo(definitionPath);

        logger.LogTrace("Looking for pipeline file: {FullName}, Exists: {Exists}",
            pipelineFile.FullName, pipelineFile.Exists);

        if (!pipelineFile.Exists)
        {
            interactionService.DisplayError($"Definition file '{definitionPath}' does not exist.");
            return null;
        }

        var matchingPipelines = pipelines
            .Where(pipeline =>
                pipeline.DefinitionFile.FullName.Equals(pipelineFile.FullName))
            .ToList();

        logger.LogTrace("Found {MatchingCount} matching pipeline(s) for {FullName}",
            matchingPipelines.Count, pipelineFile.FullName);

        var pipelineInfo = matchingPipelines.FirstOrDefault();

        if (matchingPipelines.Count > 1)
            pipelineInfo = await interactionService.SelectAsync(
                "Multiple pipelines found for the specified definition file. Please select one:",
                matchingPipelines,
                pipeline => pipeline.Name);

        if (pipelineInfo is null)
        {
            logger.LogTrace("No pipeline found for definition file: {DefinitionPath}", definitionPath);
            interactionService.DisplayError($"No pipeline found for definition file '{definitionPath}'.");
        }
        else
        {
            logger.LogTrace("Returning pipeline: {PipelineName} (Id: {PipelineId})",
                pipelineInfo.Name, pipelineInfo.Id.Value);
        }

        return pipelineInfo;
    }

    private async Task<IEnumerable<LocalPipelineInfo>> GetLocalPipelinesAsync()
    {
        logger.LogTrace("GetLocalPipelinesAsync called");

        var pipelinesTask = pipelinesService
            .GetLocalPipelinesAsync()
            .ToListAsync()
            .AsTask();

        List<LocalPipelineInfo> pipelines = await interactionService
            .ShowStatusAsync("Loading Pipelines...", () => pipelinesTask);

        logger.LogTrace("GetLocalPipelinesAsync returning {Count} pipeline(s)", pipelines.Count);

        return pipelines;
    }
}

internal static class PipelineInteractionServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection TryAddPipelineInteractionService()
        {
            services.TryAddInteractionService();
            services.TryAddPipelinesService();
            services.TryAddSingleton<IPipelineInteractionService, PipelineInteractionService>();
            return services;
        }
    }
}
