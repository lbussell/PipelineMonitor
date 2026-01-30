// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace PipelineMonitor.Authentication;

/// <summary>
/// Provides an authenticated connection to Azure DevOps.
/// </summary>
internal interface IVssConnectionProvider
{
    /// <summary>
    /// Gets an authenticated VssConnection for the specified organization.
    /// </summary>
    /// <param name="organization">
    /// URI of the Azure DevOps organization, example: "https://dev.azure.com/orgname"
    /// </param>
    /// <returns></returns>
    VssConnection GetConnection(Uri organization);
}

/// <inheritdoc/>
internal sealed class VssConnectionProvider(
    IAzureCredentialProvider azureCredentialProvider,
    ILogger<VssConnectionProvider> logger)
    : IVssConnectionProvider
{
    private readonly IAzureCredentialProvider _azureCredentialProvider = azureCredentialProvider;
    private readonly ILogger<VssConnectionProvider> _logger = logger;
    private readonly ConcurrentDictionary<string, Lazy<VssConnection>> _connectionCache = new();

    /// <inheritdoc/>
    public VssConnection GetConnection(Uri organization)
    {
        _logger.LogTrace("GetConnection called for organization: {Organization}", organization);

        var cacheKey = organization.AbsoluteUri;
        var isNew = !_connectionCache.ContainsKey(cacheKey);

        var lazyConnection = _connectionCache.GetOrAdd(
            cacheKey,
            _ => new Lazy<VssConnection>(() => CreateConnection(organization)));

        if (isNew)
            _logger.LogTrace("Created new connection for: {Organization}", organization);
        else
            _logger.LogTrace("Using cached connection for: {Organization}", organization);

        return lazyConnection.Value;
    }

    private VssConnection CreateConnection(Uri organization)
    {
        // This scope provides access to Azure DevOps Services REST API.
        // See https://learn.microsoft.com/rest/api/azure/devops/tokens/?view=azure-devops-rest-7.1&tabs=powershell#personal-access-tokens-pats
        const string AzureDevOpsScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

        _logger.LogTrace("Creating VssConnection for: {Organization}", organization);

        var credential = _azureCredentialProvider.GetCredential();
        var requestContext = new TokenRequestContext([AzureDevOpsScope]);

        _logger.LogTrace("Requesting Azure token for scope: {Scope}", AzureDevOpsScope);
        var tokenObject = credential.GetToken(requestContext, CancellationToken.None);

        _logger.LogTrace("Token acquired, expires: {ExpiresOn}", tokenObject.ExpiresOn);

        var vssCredential = new VssOAuthAccessTokenCredential(tokenObject.Token);
        var vssConnection = new VssConnection(baseUrl: organization, credentials: vssCredential);

        _logger.LogTrace("VssConnection created successfully for: {Organization}", organization);

        return vssConnection;
    }
}

internal static class VssConnectionProviderExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection TryAddVssConnectionProvider()
        {
            services.TryAddSingleton<IVssConnectionProvider, VssConnectionProvider>();
            services.TryAddAzureCredentialProvider();
            return services;
        }
    }
}
