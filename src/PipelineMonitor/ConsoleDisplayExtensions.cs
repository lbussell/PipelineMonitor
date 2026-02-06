// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Spectre.Console;

namespace PipelineMonitor;

internal static class ConsoleDisplayExtensions
{
    extension(IAnsiConsole ansiConsole)
    {
        public void Display(LocalPipelineInfo pipeline)
        {
            ansiConsole.MarkupLineInterpolated($"[underline]{pipeline.Name}[/]");
            ansiConsole.WriteLine($"File: {pipeline.RelativePath}");
            ansiConsole.WriteLine($"ID: {pipeline.Id.Value}");
        }
    }
}
