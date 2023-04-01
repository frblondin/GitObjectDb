using AutoFixture;
using GitObjectDb.Api.OData.Model;
using GitObjectDb.Tests.Assets.Tools;
using Microsoft.OData.Edm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static GitObjectDb.Api.OData.Tests.Model.BasicModel;

namespace GitObjectDb.Api.OData.Tests.Model;
public class EdmModelConverterTests
{
    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void QuerySimpleNodes(IEnumerable<Type> dtoTypes)
    {
        // Act
        var edmModel = dtoTypes.ConvertToEdm();

        // Assert
        Assert.Multiple(() =>
        {
            foreach (var type in dtoTypes)
            {
                Assert.That(
                    edmModel.SchemaElements.ToList(),
                    Has.One.Items.Matches<IEdmSchemaElement>(
                        e => e.FullName().Equals(type.FullName, StringComparison.Ordinal)));
            }
        });
    }

    private class Customization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var emitter = new DtoTypeEmitter(CreateDataModel(typeof(BasicModel).GetNestedTypes()));
            fixture.Inject(emitter.TypeDescriptions.Select(d => d.DtoType));
        }
    }
}
