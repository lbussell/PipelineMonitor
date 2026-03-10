// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace AzurePipelinesTool.Display;

/// <summary>
/// A progress column that displays elapsed time with support for an initial offset.
/// This allows showing the real wall-clock duration of a pipeline stage, even when
/// the user starts watching after the stage has already been running.
/// </summary>
internal sealed class OffsetElapsedTimeColumn : ProgressColumn
{
    private readonly Dictionary<int, TimeSpan> _offsets = [];

    public Style Style { get; set; } = Color.Blue;

    /// <summary>
    /// Sets an initial time offset for a progress task so the elapsed display
    /// reflects actual pipeline duration rather than starting from zero.
    /// </summary>
    public void SetOffset(ProgressTask task, TimeSpan offset) => _offsets[task.Id] = offset;

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        _offsets.TryGetValue(task.Id, out var offset);

        // For finished tasks, the offset holds the exact duration — don't use ElapsedTime
        // because the Spectre.Console stopwatch may not be fully frozen after StopTask().
        if (task.IsFinished)
        {
            return offset == TimeSpan.Zero
                ? new Markup("--:--:--")
                : new Text($"{offset:hh\\:mm\\:ss}", Style);
        }

        // For running tasks, offset is the head start and ElapsedTime adds the live portion.
        var elapsed = task.ElapsedTime;
        var total = elapsed is null && offset == TimeSpan.Zero
            ? (TimeSpan?)null
            : (elapsed ?? TimeSpan.Zero) + offset;

        if (total is null)
        {
            return new Markup("--:--:--");
        }

        return total.Value.TotalHours > 99
            ? new Markup("**:**:**")
            : new Text($"{total.Value:hh\\:mm\\:ss}", Style);
    }

    public override int? GetColumnWidth(RenderOptions options) => 8;
}
