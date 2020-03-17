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
            var (sut, repository) = Arrange<IQuery<Node, string, IEnumerable<Node>>>(fixture);

            // Act
            var applications = sut.Execute(repository, default, default).OfType<Application>().ToList();

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
            var (sut, repository) = Arrange<IQuery<Node, string, IEnumerable<Node>>>(fixture);

            // Act
            var tables = sut.Execute(repository, application, default).OfType<Table>().ToList();

            // Assert
            Assert.That(tables, Has.Count.EqualTo(SoftwareCustomization.DefaultTablePerApplicationCount));
            Assert.That(tables[0].Path, Is.Not.Null);
            Assert.That(tables[0].Name, Is.Not.Null);
            Assert.That(tables[0].Description, Is.Not.Null);
        }

        private static (TQuery, Repository) Arrange<TQuery>(IFixture fixture) =>
        (
            fixture.Create<TQuery>(),
            fixture.Create<Repository>()
        );
    }
}
