// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text.Json;
using ConsoleAppFramework;
using PipelineMonitor.AzureDevOps;
using Spectre.Console;

namespace PipelineMonitor.Commands;

internal sealed class VariablesCommand(
    IAnsiConsole ansiConsole,
    IInteractionService interactionService,
    IPipelineInteractionService pipelineInteractionService,
    PipelinesService pipelinesService)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    [Command("variables")]
    public async Task ShowVariablesAsync([Argument] string definitionPath)
    {
        var pipeline = await pipelineInteractionService.GetLocalPipelineAsync(definitionPath);
        if (pipeline is null) return;

        var variablesTask = pipelinesService.GetVariablesAsync(pipeline);
        var variables = await interactionService.ShowStatusAsync("Loading variables...", () => variablesTask);

        if (variables.Count == 0)
        {
            interactionService.DisplayWarning("No variables defined in this pipeline.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Name")
            .AddColumn("Value")
            .AddColumn("Secret")
            .AddColumn("Allow Override");

        foreach (var variable in variables)
        {
            var valueDisplay = variable.IsSecret ? "[dim]***[/]" : (variable.Value ?? string.Empty).EscapeMarkup();
            var secretDisplay = variable.IsSecret ? "[yellow]Yes[/]" : "No";
            var allowOverrideDisplay = variable.AllowOverride ? "Yes" : "No";

            table.AddRow(
                $"[blue]{variable.Name.EscapeMarkup()}[/]",
                valueDisplay,
                secretDisplay,
                allowOverrideDisplay);
        }

        ansiConsole.Write(table);
        ansiConsole.WriteLine();
    }

    [Command("variables export")]
    public async Task ExportVariablesAsync(
        [Argument] string definitionPath,
        [Argument] string outputFile)
    {
        var pipeline = await pipelineInteractionService.GetLocalPipelineAsync(definitionPath);
        if (pipeline is null) return;

        var variablesTask = pipelinesService.GetVariablesAsync(pipeline);
        var variables = await interactionService.ShowStatusAsync("Loading variables...", () => variablesTask);

        var json = JsonSerializer.Serialize(variables, JsonOptions);

        await File.WriteAllTextAsync(outputFile, json);
        interactionService.DisplaySuccess($"Exported {variables.Count} variable(s) to {outputFile}");
    }

    [Command("variables import")]
    public async Task ImportVariablesAsync(
        [Argument] string definitionPath,
        [Argument] string inputFile,
        bool clear = false)
    {
        var pipeline = await pipelineInteractionService.GetLocalPipelineAsync(definitionPath);
        if (pipeline is null) return;

        if (!File.Exists(inputFile))
        {
            interactionService.DisplayError($"Input file '{inputFile}' does not exist.");
            return;
        }

        List<PipelineVariableInfo>? variables;
        try
        {
            var json = await File.ReadAllTextAsync(inputFile);
            variables = JsonSerializer.Deserialize<List<PipelineVariableInfo>>(json);
        }
        catch (JsonException ex)
        {
            interactionService.DisplayError($"Failed to parse JSON file: {ex.Message}");
            return;
        }

        if (variables is null || variables.Count == 0)
        {
            interactionService.DisplayWarning("No variables found in the input file.");
            return;
        }

        await interactionService.ShowStatusAsync("Setting variables...", async () =>
        {
            await pipelinesService.SetVariablesAsync(pipeline, variables, clear);
            return true;
        });

        var actionDescription = clear ? "replaced with" : "imported";
        interactionService.DisplaySuccess($"Successfully {actionDescription} {variables.Count} variable(s).");
    }
}
