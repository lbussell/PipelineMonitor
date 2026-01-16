// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console;

namespace PipelineMonitor;

internal interface IInteractionService
{
    Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action);
}

internal sealed class InteractionService(IAnsiConsole ansiConsole) : IInteractionService
{
    private readonly IAnsiConsole _ansiConsole = ansiConsole;

    public async Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        // TODO: avoid spinners in non-interactive environments
        return await _ansiConsole.Status()
            .Spinner(Spinner.Known.Default)
            .StartAsync(statusText, (context) => action());
    }
}

internal static class InteractionServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection TryAddInteractionService()
        {
            services.TryAddSingleton<IInteractionService, InteractionService>();
            services.TryAddSingleton(_ => AnsiConsole.Console);
            return services;
        }
    }
}
