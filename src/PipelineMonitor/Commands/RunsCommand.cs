// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;
using PipelineMonitor.AzureDevOps;
using Spectre.Console;

namespace PipelineMonitor.Commands;

internal sealed class RunsCommand(
    IAnsiConsole ansiConsole,
    PipelineResolver pipelineResolver,
    PipelinesService pipelinesService)
{
    private readonly IAnsiConsole _ansiConsole = ansiConsole;
    private readonly PipelineResolver _pipelineResolver = pipelineResolver;
    private readonly PipelinesService _pipelinesService = pipelinesService;

    [Command("runs")]
    public async Task ExecuteAsync(
        [Argument] string definitionPath,
        int top = 10)
    {
        var pipeline = await _pipelineResolver.GetLocalPipelineAsync(definitionPath);

        var pipelineRuns = _pipelinesService.GetRunsAsync(pipeline, top);

        var table = new Table()
            .AddColumn("Result")
            .AddColumn("Name")
            .AddColumn("Commit")
            .AddColumn("State")
            .Border(TableBorder.Simple);

        await _ansiConsole
            .Live(table)
            .StartAsync(async context =>
            {
                await foreach (var run in pipelineRuns)
                {
                    var resultText = run.Result switch
                    {
                        PipelineRunResult.Succeeded => "[green]✓[/]",
                        PipelineRunResult.PartiallySucceeded => "[yellow]~[/]",
                        PipelineRunResult.Failed => "[red]✗[/]",
                        PipelineRunResult.Canceled => "[grey]/[/]",
                        _ => "[grey]-[/]",
                    };

                    var commitMessage = run.Commit?.Message ?? "";
                    table.AddRow(
                        resultText,
                        Markup.Escape(run.Name),
                        Markup.Escape(commitMessage),
                        run.State);
                    context.Refresh();
                }
            });
    }
}
