// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PipelineMonitor.AzureDevOps;

internal static class Display
{
    extension(LocalPipelineInfo pipeline)
    {
        public IRenderable SingleLineDisplay =>
            new Markup($"[blue]{pipeline.RelativePath}[/] refers to pipeline [bold green]{pipeline.Name}[/] [dim](ID: {pipeline.Id.Value})[/]");
    }

    extension(PipelineRunInfo run)
    {
        public IRenderable ResultSymbol => new PipelineRunResultRenderable(run.Result);

        public IRenderable SingleLineDisplay {
            get
            {
                // var symbol = run.Result switch
                // {
                //     "succeeded" => "[green][[✓]][/]",
                //     "failed" => "[red][[✗]][/]",
                //     "canceled" => "[yellow][[!]][/]",
                //     _ => "[grey][[?]][/]"
                // };

                var titleRow = new Markup($"#[bold]{run.Name}[/] • {run.Commit?.Message}");

                var metadataRow = new Columns([
                    new Markup($"[dim]Manually run by <whoever>[/]"),
                    new Markup("[dim]•[/]"),
                    new Markup($"[dim]branch/name[/]"),
                    new Markup("[dim]•[/]"),
                    new Markup($"[dim]{run.Commit?.Sha[..10]}[/]"),
                ]);
                metadataRow.Expand = false;

                IEnumerable<IRenderable> content =
                [
                    titleRow,
                    metadataRow,
                ];

                return new Grid()
                    .AddColumn()
                    .AddColumn()
                    .AddRow(run.ResultSymbol, new Rows(content));

                // return new Markup($"Run [bold yellow]{run.Id}[/] - [green]{run.Result}[/] - [blue]{run.Result}[/] - [dim]{run.QueueTime:u}[/]");
            }
        }
    }

    private sealed class PipelineRunResultRenderable(PipelineRunResult result) : IRenderable
    {
        private static readonly (string Symbol, Color Color)[] Styles =
        [
            ("?", Color.Grey),    // None
            ("✓", Color.Green),   // Succeeded
            ("~", Color.Yellow),  // PartiallySucceeded
            ("✗", Color.Red),     // Failed
            ("!", Color.Grey),    // Canceled
        ];

        public Measurement Measure(RenderOptions options, int maxWidth) => new(3, 3);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var (symbol, color) = Styles[(int)result];
            var style = new Style(foreground: color);
            yield return new Segment("[", style);
            yield return new Segment(symbol, style);
            yield return new Segment("]", style);
        }
    }
}
