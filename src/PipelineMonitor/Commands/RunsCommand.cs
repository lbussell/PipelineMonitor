// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using PipelineMonitor.AzureDevOps;
using Spectre.Console;

namespace PipelineMonitor.Commands;

internal sealed class RunsCommand(
    IAnsiConsole ansiConsole,
    InfoCommand infoCommand,
    PipelinesService pipelinesService)
{
    [Command("runs")]
    public async Task ShowRunsAsync(
        [Argument] string definitionPath,
        int top = 10)
    {
        var pipeline = await infoCommand.GetLocalPipelineAsync(definitionPath);
        if (pipeline is null) return;

        var pipelineRuns = pipelinesService.GetRunsAsync(pipeline, top);

        var descriptionColumn = new TableColumn("Description");
        var table = new Table()
            .AddColumn("")
            .AddColumn(descriptionColumn)
            .AddColumn("Stages")
            .AddColumn(new TableColumn("").RightAligned())
            .Border(TableBorder.Horizontal);

        await ansiConsole
            .Live(table)
            .StartAsync(async context =>
            {
                await foreach (var run in pipelineRuns)
                {
                    table.AddRow(run.ResultSymbol, run.RunDetails, run.StagesSummary, run.TimeDetails);
                    context.Refresh();
                }
            });
    }
}
