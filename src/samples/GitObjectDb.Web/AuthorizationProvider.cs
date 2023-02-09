using GitObjectDb.Api.GraphQL;
using GitObjectDb.Api.GraphQL.Loaders;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenFga.Sdk.Api;
using OpenFga.Sdk.Model;
using System.Security.Claims;

namespace GitObjectDb.Web;

internal class AuthorizationProvider : IAuthorizationProvider
{
    private readonly OpenFgaApi _openFga;
    private readonly CacheEntryStrategyProvider _cacheEntryStrategyProvider;
    private readonly IMemoryCache _memoryCache;

    public AuthorizationProvider(OpenFgaApi openFga, IMemoryCache memoryCache, IOptions<GitObjectDbGraphQLOptions> options)
    {
        _openFga = openFga;
        _cacheEntryStrategyProvider = options.Value.CacheEntryStrategy;
        _memoryCache = memoryCache;
    }

    public async Task<IEnumerable<string>> GetPermissionsAsync(Node node, IEnumerable<Claim> claims)
    {
        var user = GetUser(claims);
        var request = new ReadRequest()
        {
            TupleKey = new($"doc:{node.Id}", user: user),
            PageSize = 100,
        };
        return await GetOrCreateAsync(
            (nameof(GetPermissionsAsync), node.Path, node.TreeId, user),
            async () => (await _openFga.Read(request))
                .Tuples
                .Select(t => t.Key.Relation)
                .ToList());
    }

    public async Task<bool> IsAuthorizedAsync(Node node, IEnumerable<Claim> claims, string action)
    {
        var user = GetUser(claims);
        var request = new CheckRequest()
        {
            TupleKey = new($"doc:{node.Id}", action, user),
        };
        return await GetOrCreateAsync(
            (nameof(IsAuthorizedAsync), node.Path, node.TreeId, user, action),
            async () => (await _openFga.Check(request)).Allowed);
    }

#pragma warning disable CS8603 // Possible null reference return.
    private async Task<TResult> GetOrCreateAsync<TResult>(object key, Func<Task<TResult>> factory) =>
        await _memoryCache.GetOrCreateAsync(
            key,
            async entry =>
            {
                _cacheEntryStrategyProvider.Invoke(entry);
                return await factory();
            });
#pragma warning restore CS8603 // Possible null reference return.

    private static string GetUser(IEnumerable<Claim> claims)
    {
        const string givenNameClaim = "given_name";
        return claims.FirstOrDefault(c => c.Type == givenNameClaim)?.Value?.ToLower() ??
            throw new NotSupportedException($"'{givenNameClaim}' claim could not be found in current user context.");
    }
}
