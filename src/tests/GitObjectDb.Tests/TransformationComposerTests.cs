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
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(Customization))]
    public void ThrowExceptionForUndefinedTypes(ITransformationComposer sut)
    {
        // Act, assert
        Assert.Throws<GitObjectDbException>(
            () => sut.CreateOrUpdate(new UnregisteredNode()));
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
            fixture.Register<ITransformationComposer>(() =>
                fixture.Create<Factories.TransformationComposerFactory>().Invoke(
                    fixture.Create<IConnectionInternal>(),
                    fixture.Create<string>()));
        }
    }
}
