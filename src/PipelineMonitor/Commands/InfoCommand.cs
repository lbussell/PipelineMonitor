// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using PipelineMonitor.AzureDevOps;
using Spectre.Console;

namespace PipelineMonitor.Commands;

internal sealed class InfoCommand(
    IAnsiConsole ansiConsole,
    IInteractionService interactionService,
    PipelinesService pipelinesService)
{
    [Command("info")]
    public async Task ShowPipelineInfoAsync([Argument] string definitionPath)
    {
        var pipeline = await GetLocalPipelineAsync(definitionPath);
        if (pipeline is null) return;

        ansiConsole.Write(pipeline.SingleLineDisplay);
        ansiConsole.WriteLine();
    }

    internal async Task<LocalPipelineInfo?> GetLocalPipelineAsync(string definitionPath)
    {
        var pipelines = await GetLocalPipelinesAsync();
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

    private async Task<IEnumerable<LocalPipelineInfo>> GetLocalPipelinesAsync()
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
