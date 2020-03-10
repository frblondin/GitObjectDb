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
        public void GetRootNodes(Repository repository)
        {
            // Arrange
            var connection = A.Fake<IConnectionInternal>(x => x.Strict());
            A.CallTo(() => connection.Repository).Returns(repository);

            // Act
            var applications = new QueryNodes().Execute(repository, default(Node), null).OfType<Application>().ToList();

            // Assert
            Assert.That(applications, Has.Count.EqualTo(SoftwareCustomization.DefaultApplicationCount));
            Assert.That(applications[0].Path, Is.Not.Null);
            Assert.That(applications[0].Name, Is.Not.Null);
            Assert.That(applications[0].Description, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization))]
        public void GetChildNodes(Repository repository, Application application)
        {
            // Arrange
            var connection = A.Fake<IConnectionInternal>(x => x.Strict());
            A.CallTo(() => connection.Repository).Returns(repository);

            // Act
            var tables = new QueryNodes().Execute(repository, application, null).OfType<Table>().ToList();

            // Assert
            Assert.That(tables, Has.Count.EqualTo(SoftwareCustomization.DefaultTablePerApplicationCount));
            Assert.That(tables[0].Path, Is.Not.Null);
            Assert.That(tables[0].Name, Is.Not.Null);
            Assert.That(tables[0].Description, Is.Not.Null);
        }
    }
}
