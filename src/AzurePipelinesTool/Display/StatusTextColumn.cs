// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace AzurePipelinesTool.Display;

/// <summary>
/// A progress column that displays a status label (e.g. "Running...", "Succeeded", "Failed")
/// to the right of each progress bar row.
/// </summary>
internal sealed class StatusTextColumn : ProgressColumn
{
    private readonly Dictionary<int, string> _statuses = [];

    /// <summary>
    /// Sets the Spectre markup string to display for the given progress task.
    /// </summary>
    public void SetStatus(ProgressTask task, string markup) => _statuses[task.Id] = markup;

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        _statuses.TryGetValue(task.Id, out var markup);
        return new Markup(markup ?? string.Empty);
    }
}
