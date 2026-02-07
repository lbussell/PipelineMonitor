// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Reflection;
using ConsoleAppFramework;

namespace AzurePipelinesTool.Commands;

internal sealed class LlmsTxtCommand
{
    /// <summary>
    /// Print comprehensive tool documentation (run this first).
    /// </summary>
    [Command("llmstxt")]
    public void Execute()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SKILL.md")
            ?? throw new UserFacingException("Embedded SKILL.md resource not found.");

        using var reader = new StreamReader(stream);
        Console.Out.Write(reader.ReadToEnd());
    }
}
