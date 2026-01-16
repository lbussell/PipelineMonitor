// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using ConsoleAppFramework;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PipelineMonitor;
using PipelineMonitor.AzureDevOps;
using PipelineMonitor.Logging;

using Spectre.Console;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.SetBasePath(AppContext.BaseDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Services.TryAddPipelinesService();
builder.Services.TryAddOrganizationDiscoveryService();
builder.Services.TryAddRepoInfoResolver();
builder.Services.TryAddInteractionService();

builder.Logging.ClearProviders();
builder.Logging.AddFileLogger(builder.Configuration);
builder.Logging.AddLogLocationOnExit();

var consoleAppBuilder = builder.ToConsoleAppBuilder();
consoleAppBuilder.Add<App>();

var runTask = consoleAppBuilder.RunAsync(args);
await runTask;

internal sealed class App(
    IAnsiConsole ansiConsole,
    IInteractionService interactionService)
{
    private readonly IAnsiConsole _ansiConsole = ansiConsole;
    private readonly IInteractionService _interactionService = interactionService;

    [Command("discover")]
    public async Task DiscoverPipelinesAsync([FromServices] PipelinesService pipelinesService)
    {
        var pipelinesTask = pipelinesService
            .GetLocalPipelinesAsync()
            .ToListAsync()
            .AsTask();

        List<LocalPipelineInfo> pipelines = await _interactionService
            .ShowStatusAsync("Loading pipelines...", () => pipelinesTask);

        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Definition")
            .AddColumn("Pipeline");

        foreach (var pipeline in pipelines)
            table.AddRow(
                $"[blue]{pipeline.RelativePath}[/]",
                $"[bold green]{pipeline.Name}[/]");

        _ansiConsole.Write(table);
    }
}
