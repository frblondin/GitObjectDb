using GitObjectDb;
using GitObjectDb.Api.GraphQL;
using GitObjectDb.Api.GraphQL.Loaders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenFga.Sdk.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models.Organization;
public record NodeWithPerms : Node
{
    private readonly OpenFgaApi? _openFga;
    private readonly CacheEntryStrategyProvider? _cacheEntryStrategyProvider;
    private readonly string _user;
    private readonly IMemoryCache? _memoryCache;

    public NodeWithPerms(IServiceProvider? serviceProvider = null)
    {
        _openFga = serviceProvider?.GetRequiredService<OpenFgaApi>();
        _memoryCache = serviceProvider?.GetRequiredService<IMemoryCache>();
        _cacheEntryStrategyProvider = serviceProvider?
            .GetRequiredService<IOptions<GitObjectDbGraphQLOptions>>().Value
            .CacheEntryStrategy;
        var context = serviceProvider?.GetRequiredService<IHttpContextAccessor>();
        _user = context?.HttpContext?.User.Claims?.FirstOrDefault(c => c.Type == "given_name")?.Value?.ToLower()!;
    }

    public IEnumerable<string> Permissions => ReadPermissions();

    private IEnumerable<string> ReadPermissions()
    {
        var result = _memoryCache?.GetOrCreateAsync((Path, _user), Execute) ?? Execute(default);
        return result.GetAwaiter().GetResult();

        async Task<IEnumerable<string>> Execute(ICacheEntry entry)
        {
            if (_openFga is null)
            {
                return Enumerable.Empty<string>();
            }
            var response = await _openFga.Read(new()
            {
                TupleKey = new()
                {
                    Object = $"doc:{Id}",
                    User = _user,
                },
                PageSize = 100,
            });
            if (entry is not null)
            {
                _cacheEntryStrategyProvider?.Invoke(entry);
            }
            return response.Tuples.Select(t => t.Key.Relation).ToList();
        }
    }
}
