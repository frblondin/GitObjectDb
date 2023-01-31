using AutoFixture;
using GitObjectDb.Model;
using GitObjectDb.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace GitObjectDb.Tests.Customization;
public class ReferenceCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register(() =>
        {
            var path = GitObjectDbFixture.GetAvailableFolderPath();
            var model = new ConventionBaseModelBuilder()
                .RegisterType<NodeWithReference>()
                .RegisterType<NodeWithReferenceOld>()
                .RegisterType<NodeWithMultipleReferences>()
                .RegisterType<NodeWithMultipleReferencesOld>()
                .AddDeprecatedNodeUpdater((old, targetType) =>
                {
                    return old switch
                    {
                        NodeWithReferenceOld n => (NodeWithReference)n,
                        NodeWithMultipleReferencesOld n => (NodeWithMultipleReferences)n,
                        _ => throw new NotImplementedException(old.GetType().Name),
                    };
                })
                .Build();
            fixture.Do<IServiceCollection>(services => services.AddSingleton(model));
            var repositoryFactory = fixture.Create<IServiceProvider>()
                .GetRequiredService<ConnectionFactory>();
            return repositoryFactory(path, model);
        });
    }
}

[GitFolder(FolderName = "Items")]
public record NodeWithReference : Node
{
    public string Name { get; set; }

    public NodeWithReference Reference { get; set; }
}

[GitFolder(FolderName = "Items")]
[IsDeprecatedNodeType(typeof(NodeWithReference))]
public record NodeWithReferenceOld : Node
{
    public string Name { get; set; }

    public NodeWithReference Reference { get; set; }

    public static implicit operator NodeWithReference(NodeWithReferenceOld old) =>
        old is not null ?
        new()
        {
            Id = old.Id,
            Path = old.Path,
            Name = old.Name,
            Reference = old.Reference,
        }
        : null;

    public static implicit operator NodeWithReferenceOld(NodeWithReference @new) =>
        @new is not null ?
        new()
        {
            Id = @new.Id,
            Path = @new.Path,
            Name = @new.Name,
            Reference = @new.Reference,
        }
        : null;
}

[GitFolder(FolderName = "Items")]
public record NodeWithMultipleReferences : Node
{
    public string Name { get; set; }

    public IList<NodeWithMultipleReferences> References { get; set; }
}

[GitFolder(FolderName = "Items")]
[IsDeprecatedNodeType(typeof(NodeWithMultipleReferences))]
public record NodeWithMultipleReferencesOld : Node
{
    public string Name { get; set; }

    public IList<NodeWithMultipleReferences> References { get; set; }

    public static implicit operator NodeWithMultipleReferences(NodeWithMultipleReferencesOld old) =>
        old is not null ?
        new()
        {
            Id = old.Id,
            Path = old.Path,
            Name = old.Name,
            References = old.References,
        }
        : null;

    public static implicit operator NodeWithMultipleReferencesOld(NodeWithMultipleReferences @new) =>
        @new is not null ?
        new()
        {
            Id = @new.Id,
            Path = @new.Path,
            Name = @new.Name,
            References = @new.References,
        }
        : null;
}
