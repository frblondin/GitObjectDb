using AutoFixture;
using FakeItEasy;
using GitObjectDb.Internal;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;

namespace GitObjectDb.Tests;
internal class TransformationComposerTests
{
    [Test]
    public void ThrowExceptionForUndefinedTypes()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultServiceProviderCustomization()).Customize(new Customization());
        var sut = fixture.Create<IChangeComposer>();

        // Act, assert
        Assert.Throws<GitObjectDbException>(
            () => sut.CreateOrUpdateAsync(new UnregisteredNode()));
    }

    public record UnregisteredNode : Node
    {
    }

    public class Customization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject(new ConventionBaseModelBuilder().Build());
            fixture.Inject(A.Fake<IConnectionInternal>(o =>
                o.ConfigureFake(fake =>
                    A.CallTo(() => fake.Model).Returns(fixture.Create<IDataModel>()))));
            fixture.Register<IChangeComposer>(() =>
                fixture.Create<Factories.ChangeComposerFactory>().Invoke(
                    fixture.Create<IConnectionInternal>(),
                    fixture.Create<string>()));
        }
    }
}
