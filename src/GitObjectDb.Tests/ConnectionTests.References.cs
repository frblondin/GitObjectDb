using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

namespace GitObjectDb.Tests.Serialization.Json
{
    public partial class ConnectionTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
        public void ReferencesAreSupported(IServiceProvider serviceProvider, string name, Signature signature)
        {
            // Arrange
            var sut = CreateRepository(serviceProvider);
            DataPath path = default;
            sut.Update(c =>
            {
                var node1 = c.CreateOrUpdate(new NodeWithReference
                {
                    Name = name,
                });
                var node2 = c.CreateOrUpdate(new NodeWithReference
                {
                    Reference = node1,
                });
                path = node2.Path;
            }).Commit("foo", signature, signature);

            // Act
            var result = sut.Lookup<NodeWithReference>(path);

            // Act, Assert
            Assert.That(result.Reference, Is.InstanceOf<NodeWithReference>());
            Assert.That(((NodeWithReference)result.Reference).Name, Is.EqualTo(name));
        }

        private IConnection CreateRepository(IServiceProvider serviceProvider)
        {
            var path = GitObjectDbFixture.GetAvailableFolderPath();
            var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
            var model = serviceProvider.GetRequiredService<IDataModel>();
            var result = (IConnectionInternal)repositoryFactory(path, model);
            return result;
        }

        [GitFolder(FolderName = "Applications")]
        public record NodeWithReference : Node
        {
            public string Name { get; set; }

            public Node Reference { get; set; }
        }
    }
}
