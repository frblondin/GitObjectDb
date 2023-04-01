using FakeItEasy;
using GitObjectDb.Api.OData.Model;
using GitObjectDb.Api.OData.Tests.Model;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static GitObjectDb.Api.OData.Tests.Model.BasicModel;

namespace GitObjectDb.Api.OData.Tests;
internal class ServiceConfigurationTests
{
    [Test]
    public void DataProviderAndEmitterGetRegistered()
    {
        // Act
        var serviceProvider = new ServiceCollection()
            .AddMemoryCache()
            .AddSingleton(CreateDataModel(typeof(BasicModel).GetNestedTypes()))
            .AddGitObjectDb()
            .AddSingleton(A.Fake<IQueryAccessor>())
            .AddGitObjectDbOData()
            .BuildServiceProvider();

        // Arrange
        Assert.Multiple(() =>
        {
            Assert.That(serviceProvider.GetService<DataProvider>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<DtoTypeEmitter>(), Is.Not.Null);
        });
    }
}
