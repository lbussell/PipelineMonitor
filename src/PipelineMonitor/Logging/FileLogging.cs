// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace PipelineMonitor.Logging;

internal static class FileLogging
{
    /// <summary>
    /// Configures Microsoft.Extensions.Logging to output structured logs to the specified file.
    /// </summary>
    /// <remarks>
    /// It is advisable to clear existing logging providers .ClearProviders() before invoking this method.
    /// </remarks>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder loggingBuilder)
    {
        const string LogFileName = "pipelinemon";
        const string LogFileExtension = ".txt";

        // TODO: Get log file path from Microsoft.Extensions.Configuration
        var logDirectory = Environment.CurrentDirectory;

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var timestampedPath = Path.Combine(logDirectory, $"{LogFileName}-{timestamp}{LogFileExtension}");

        var fileTarget = new FileTarget("textlog")
        {
            FileName = timestampedPath,
        };

        var config = new LoggingConfiguration();
        config.AddTarget(fileTarget);
        config.AddRuleForAllLevels(fileTarget);

        var builder = loggingBuilder.AddNLog(config);
        return builder;
    }
}
