using AutoFixture;
using FakeItEasy;
using GitObjectDb.Comparison;
using GitObjectDb.Queries;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Queries
{
    [Parallelizable(ParallelScope.All)]
    public class QueryNodesTest
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization))]
        public void GetRootNodes(IFixture fixture)
        {
            // Arrange
            var (sut, repository) = Arrange(fixture);

            // Act
            var applications = sut.Execute(repository, default, repository.Head.Tip.Tree).OfType<Application>().ToList();

            // Assert
            Assert.That(applications, Has.Count.EqualTo(SoftwareCustomization.DefaultApplicationCount));
            Assert.That(applications[0].Path, Is.Not.Null);
            Assert.That(applications[0].Name, Is.Not.Null);
            Assert.That(applications[0].Description, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization))]
        public void GetChildNodes(IFixture fixture, Application application)
        {
            // Arrange
            var (sut, repository) = Arrange(fixture);
            var tree = repository.Head.Tip.Tree[application.Path.FolderPath].Target.Peel<Tree>();

            // Act
            var tables = sut.Execute(repository, application.Path, tree).OfType<Table>().ToList();

            // Assert
            Assert.That(tables, Has.Count.EqualTo(SoftwareCustomization.DefaultTablePerApplicationCount));
            Assert.That(tables[0].Path, Is.Not.Null);
            Assert.That(tables[0].Name, Is.Not.Null);
            Assert.That(tables[0].Description, Is.Not.Null);
        }

        private static (IQuery<DataPath, Tree, IEnumerable<Node>>, Repository) Arrange(IFixture fixture) =>
        (
            fixture.Create<IQuery<DataPath, Tree, IEnumerable<Node>>>(),
            fixture.Create<Repository>()
        );
    }
}
