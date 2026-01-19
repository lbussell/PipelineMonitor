// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Humanizer;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PipelineMonitor.AzureDevOps;

internal static class Display
{
    extension(LocalPipelineInfo pipeline)
    {
        public IRenderable SingleLineDisplay =>
            new Markup($"[blue]{Markup.Escape(pipeline.RelativePath)}[/] refers to pipeline [bold green]{Markup.Escape(pipeline.Name)}[/] [dim](ID: {pipeline.Id.Value})[/]");
    }

    extension(PipelineRunInfo run)
    {
        public IRenderable ResultSymbol => new ResultBadge(run.Result);

        public IRenderable RunDetails
        {
            get
            {
                var titleRow = new Markup($"#[bold]{Markup.Escape(run.Name)}[/] • {Markup.Escape(run.Commit?.Message ?? "")}");

                var metadataRow = new Columns([
                    new Markup($"[dim]Run by someone...[/]"),
                    new Markup("[dim]•[/]"),
                    new Markup($"[dim]branch/name[/]"),
                    new Markup("[dim]•[/]"),
                    new Markup($"[dim]{Markup.Escape(run.Commit?.Sha[..10] ?? "")}[/]"),
                ]);
                metadataRow.Expand = false;

                return new Rows([titleRow, metadataRow]);
            }
        }

        public IRenderable TimeDetails
        {
            get
            {
                var queueTime = run.Started?.Humanize() ?? "";
                var duration = run.Started.HasValue && run.Finished.HasValue
                    ? (run.Finished.Value - run.Started.Value).Humanize(precision: 2, minUnit: TimeUnit.Second)
                    : "";
                return new Rows([
                    new Markup($"[dim]{queueTime}[/]"),
                    new Markup($"[dim]{duration}[/]")
                ]);
            }
        }

        public IRenderable StagesSummary =>
            new Columns(
                run.Stages
                    .Select(s => s.Result switch
                    {
                        PipelineRunResult.Succeeded => new Markup("[bold green]✓[/]"),
                        PipelineRunResult.PartiallySucceeded => new Markup("[yellow]~[/]"),
                        PipelineRunResult.Failed => new Markup("[bold red]✗[/]"),
                        PipelineRunResult.Canceled => new Markup("[grey]/[/]"),
                        _ => new Markup("[grey]-[/]"),
                    })).Padding(0, 0, 0, 0);
    }

    extension(IEnumerable<PipelineRunInfo> runs)
    {
        public IRenderable ToTable()
        {
            var grid = new Grid()
                .AddColumn()
                .AddColumn()
                .AddColumn()
                .AddColumn(new GridColumn().NoWrap().RightAligned());

            grid.AddRow(new Markup(""), new Markup("[dim]Description[/]").PadBottom(), new Markup(""), new Markup(""));

            foreach (var run in runs)
                grid.AddRow(run.ResultSymbol, run.RunDetails.PadBottom(), run.StagesSummary, run.TimeDetails);

            return grid;
        }
    }

    private sealed class ResultBadge(PipelineRunResult result) : IRenderable
    {
        private static readonly (string Symbol, Color Color)[] Styles =
        [
            (" ", Color.Grey),    // None
            ("✓", Color.Green),   // Succeeded
            ("~", Color.Yellow),  // PartiallySucceeded
            ("✗", Color.Red),     // Failed
            ("/", Color.Grey),    // Canceled
        ];

        public Measurement Measure(RenderOptions options, int maxWidth)
        {
            return new(3, 3);
        }

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
