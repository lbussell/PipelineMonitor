// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using PipelineMonitor.Authentication;

namespace PipelineMonitor.AzureDevOps;

internal sealed class PipelinesService(
    IVssConnectionProvider vssConnectionProvider,
    IRepoInfoResolver repoInfoResolver)
{
    private readonly IVssConnectionProvider _vssConnectionProvider = vssConnectionProvider;
    private readonly IRepoInfoResolver _repoInfoResolver = repoInfoResolver;

    public async Task<PipelineInfo> GetPipelineAsync(OrganizationInfo org, ProjectInfo project, PipelineId id)
    {
        var connection = _vssConnectionProvider.GetConnection(org.Uri);
        var client = connection.GetClient<PipelinesHttpClient>();
        var pipeline = await client.GetPipelineAsync(project: project.Name, pipelineId: id.Value);
        var result = new PipelineInfo(pipeline.Name, new PipelineId(pipeline.Id), pipeline.Url, pipeline.Folder);
        return result;
    }

    public async IAsyncEnumerable<LocalPipelineInfo> GetLocalPipelinesAsync()
    {
        var repoInfo = await _repoInfoResolver.ResolveAsync();
        if (repoInfo.Organization is null
            || repoInfo.Project is null
            || repoInfo.Repository is null)
        {
            yield break;
        }

        var connection = _vssConnectionProvider.GetConnection(repoInfo.Organization.Uri);
        var buildsClient = connection.GetClient<BuildHttpClient>();

        var buildDefinitions = await buildsClient.GetFullDefinitionsAsync2(
            repositoryId: repoInfo.Repository.Id.ToString(),
            project: repoInfo.Project.Name,
            repositoryType: "TfsGit");

        foreach (var buildDefinition in buildDefinitions)
        {
            // Ignore non-YAML pipeline definitions for now.
            if (buildDefinition.Process is not YamlProcess yamlBuildProcess)
            {
                continue;
            }

            var relativePath = yamlBuildProcess.YamlFilename;
            // Path.Join vs. Path.Combine: YamlProcess.YamlFilename has a leading
            // slash, which causes Path.Combine to ignore the first argument.
            // TODO: Extract Environment.CurrentDirectory into a service.
            var pipelineFilePath = Path.Join(Environment.CurrentDirectory, relativePath);

            var pipeline = new LocalPipelineInfo(
                Name: buildDefinition.Name,
                DefinitionFile: new FileInfo(pipelineFilePath),
                Id: new(buildDefinition.Id));

            yield return pipeline;
        }
    }

    public async IAsyncEnumerable<PipelineInfo> GetAllPipelinesAsync(OrganizationInfo org, ProjectInfo project)
    {
        var connection = _vssConnectionProvider.GetConnection(org.Uri);
        var client = connection.GetClient<PipelinesHttpClient>();

        string? continuationToken = null;
        do
        {
            List<Pipeline> response = await client.ListPipelinesAsync(project.Name, continuationToken, top: 10);

            foreach (var pipeline in response)
            {
                yield return new PipelineInfo(
                    Name: pipeline.Name,
                    Id: new PipelineId(pipeline.Id),
                    Url: pipeline.Url,
                    Folder: pipeline.Folder);
            }

            if (response is IPagedList pagedListResponse)
            {
                continuationToken = pagedListResponse.ContinuationToken;
            }

        } while (!string.IsNullOrEmpty(continuationToken));
    }
}

internal static class PipelinesServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection TryAddPipelinesService()
        {
            services.TryAddSingleton<PipelinesService>();
            services.TryAddVssConnectionProvider();
            services.TryAddRepoInfoResolver();
            return services;
        }
    }

    extension(ResolvedRepoInfo repoInfo)
    {
        public bool IsComplete => repoInfo is
        {
            Organization: not null,
            Project: not null,
            Repository: not null,
        };
    }
}
