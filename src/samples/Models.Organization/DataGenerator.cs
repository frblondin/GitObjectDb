using AutoFixture;
using Bogus;
using Bogus.DataSets;
using GitDotNet;
using GitObjectDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Models.Organization;

public class DataGenerator
{
    public DataGenerator(IConnection connection, uint totalCount, uint maxDepth)
    {
        Connection = connection;
        TotalCount = totalCount;
        MaxDepth = maxDepth;
        CountPerLevel = (uint)Math.Pow(totalCount, 1D / maxDepth);
    }

    public IConnection Connection { get; }

    public uint TotalCount { get; }

    public uint MaxDepth { get; }

    public uint CountPerLevel { get; }

    public async Task CreateInitDataAsync()
    {
        var random = new Random();
        var types = new List<OrganizationType>();
        var ids = new HashSet<UniqueId>();
        var transformations = await Connection.UpdateAsync("main", CreateDataAsync);
        var signature = CreateSignature();
        await transformations.CommitAsync(new("Initial commit", signature, signature));

        async Task CreateDataAsync(IChangeComposer composer)
        {
            await CreateOrganizationTypesAsync(composer);
            await CreateOrganizationNodesAsync(composer);
        }

        async Task CreateOrganizationTypesAsync(IChangeComposer composer)
        {
            types.Add(await composer.CreateOrUpdateAsync(new OrganizationType
            {
                Id = new UniqueId("site"),
                Label = "Site",
            }));
            types.Add(await composer.CreateOrUpdateAsync(new OrganizationType
            {
                Id = new UniqueId("region"),
                Label = "Region",
            }));
            types.Add(await composer.CreateOrUpdateAsync(new OrganizationType
            {
                Id = new UniqueId("zone"),
                Label = "Zone",
            }));
        }

        async Task CreateOrganizationNodesAsync(IChangeComposer composer, Organization? parentOrg = null, int level = 1)
        {
            for (int position = 1; position <= CountPerLevel; position++)
            {
                var (id, label) = GetUniqueValue(level);
                var node = await composer.CreateOrUpdateAsync(new Organization
                {
                    Id = id,
                    Label = label,
                    Type = types[random.Next(types.Count)],
                }, parent: parentOrg);
                if (level < MaxDepth)
                {
                    await CreateOrganizationNodesAsync(composer, node, level + 1);
                }
            }
        }

        (UniqueId Id, string Label) GetUniqueValue(int level)
        {
            var producer = CreateOrganizationLabel(level);
            UniqueId? id = default;
            string? label = default;
            while (id is null || ids.Contains((UniqueId)id))
            {
                label = producer.Invoke();
                id = new UniqueId(Regex.Replace(label, "[^A-Za-z0-9]", string.Empty));
            }
            ids.Add((UniqueId)id);
            return ((UniqueId)id, label!);
        }

        Func<string> CreateOrganizationLabel(int level)
        {
            var address = new Address();
            return level switch
            {
                1 => address.Country,
                2 => address.City,
                _ => address.StreetName,
            };
        }
    }

    private static Signature CreateSignature()
    {
        var person = new Person();
        return new Signature(person.FullName, person.Email, DateTimeOffset.Now);
    }

    public async Task UpdateRandomNodesAsync(int nodeCount, Func<Organization, Organization> update, string commitMessage)
    {
        var tip = await Connection.Repository.GetCommittishAsync("main");
        var transformations = await Connection.UpdateAsync("main", UpdateDataAsync);
        var signature = CreateSignature();
        await transformations.CommitAsync(new(commitMessage, signature, signature));

        async Task UpdateDataAsync(IChangeComposer composer)
        {
            var nodes = Connection.GetNodesAsync<Organization>(tip, isRecursive: true).ToEnumerable().ToList();
            var random = new Random();
            for (int i = 0; i < nodeCount; i++)
            {
                var node = nodes[random.Next(nodes.Count)];
                await composer.CreateOrUpdateAsync(update(node));
            }
        }
    }
}