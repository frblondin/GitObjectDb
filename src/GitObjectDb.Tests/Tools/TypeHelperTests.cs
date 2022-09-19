using GitObjectDb.Tools;
using NUnit.Framework;
using System.Collections.Generic;

namespace GitObjectDb.Tests.Tools
{
    public class TypeHelperTests
    {
        [Test]
        public void FindAssemblyDelimiterIndex()
        {
            // Act
            var assemblyQualifiedName = typeof(IEnumerable<IEnumerable<string>>).AssemblyQualifiedName;
            var index = TypeHelper.GetAssemblyDelimiterIndex(assemblyQualifiedName);

            // Arrange
            var assemblyName = typeof(IEnumerable<>).Assembly.GetName().Name;
            Assert.That(index, Is.EqualTo(assemblyQualifiedName.LastIndexOf(assemblyName, System.StringComparison.OrdinalIgnoreCase) - 2));
        }
    }
}
