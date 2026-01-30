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
        var pipeline = await CommandHelpers.GetLocalPipelineAsync(
            definitionPath, interactionService, pipelinesService);
        if (pipeline is null) return;

        ansiConsole.Write(pipeline.SingleLineDisplay);
        ansiConsole.WriteLine();
    }
}
