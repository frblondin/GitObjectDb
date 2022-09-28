using AutoFixture;
using Bogus;
using Bogus.DataSets;
using GitObjectDb;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

    public void CreateInitData()
    {
        var random = new Random();
        var types = new List<OrganizationType>();
        var ids = new HashSet<UniqueId>();
        var transformations = Connection.Update("main", CreateData);
        var signature = CreateSignature();
        transformations.Commit(new("Initial commit", signature, signature));

        void CreateData(ITransformationComposer composer)
        {
            CreateOrganizationTypes(composer);
            CreateOrganizationNodes(composer);
        }

        void CreateOrganizationTypes(ITransformationComposer composer)
        {
            types.Add(composer.CreateOrUpdate(new OrganizationType
            {
                Id = new UniqueId("site"),
                Label = "Site",
            }));
            types.Add(composer.CreateOrUpdate(new OrganizationType
            {
                Id = new UniqueId("region"),
                Label = "Region",
            }));
            types.Add(composer.CreateOrUpdate(new OrganizationType
            {
                Id = new UniqueId("zone"),
                Label = "Zone",
            }));
        }

        void CreateOrganizationNodes(ITransformationComposer composer, Organization? parentOrg = null, int level = 1)
        {
            Enumerable.Range(1, (int)CountPerLevel).ForEach(position =>
            {
                var (id, label) = GetUniqueValue(level);
                var node = composer.CreateOrUpdate(new Organization
                {
                    Id = id,
                    Label = label,
                    Type = types[random.Next(types.Count)],
                }, parent: parentOrg);
                if (level < MaxDepth)
                {
                    CreateOrganizationNodes(composer, node, level + 1);
                }
            });
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

    public void UpdateRandomNodes(int nodeCount, Func<Organization, Organization> update, string commitMessage)
    {
        var transformations = Connection.Update("main", UpdateData);
        var signature = CreateSignature();
        transformations.Commit(new(commitMessage, signature, signature));

        void UpdateData(ITransformationComposer composer)
        {
            var nodes = Connection.GetNodes<Organization>("main", isRecursive: true).ToList();
            var random = new Random();
            for (int i = 0; i < nodeCount; i++)
            {
                var node = nodes[random.Next(nodes.Count)];
                composer.CreateOrUpdate(update(node));
            }
        }
    }
}