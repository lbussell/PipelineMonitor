// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using PipelineMonitor.AzureDevOps;
using Spectre.Console;

namespace PipelineMonitor.Commands;

internal sealed class DiscoverCommand(
    IAnsiConsole ansiConsole,
    IInteractionService interactionService,
    PipelinesService pipelinesService)
{
    [Command("discover")]
    public async Task DiscoverPipelinesAsync()
    {
        var pipelinesTask = pipelinesService
            .GetLocalPipelinesAsync()
            .ToListAsync()
            .AsTask();

        IReadOnlyList<LocalPipelineInfo> pipelines = await interactionService
            .ShowStatusAsync("Loading pipelines...", () => pipelinesTask);

        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Definition")
            .AddColumn("Pipeline");

        foreach (var pipeline in pipelines)
            table.AddRow(
                $"[blue]{pipeline.RelativePath}[/]",
                $"[bold green]{pipeline.Name}[/]");

        ansiConsole.Write(table);
        ansiConsole.WriteLine();
    }
}
