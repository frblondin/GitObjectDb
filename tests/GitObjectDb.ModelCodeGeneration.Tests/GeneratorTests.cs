using GitObjectDb.Git;
using GitObjectDb.ModelCodeGeneration.Tests.TestCases;
using GitObjectDb.ModelCodeGeneration.Tests.Tools;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using GitObjectDb.Validations;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests
{
    public class GeneratorTests
    {
        private static readonly UniqueId _id = UniqueId.CreateNew();

        [Test]
        [TestCaseSource(typeof(GeneratorTests), nameof(TestCases))]
        public async Task UseModelGenerator(string typeName, Func<Type, dynamic> activator, (string Name, object Value)[] propertyValues)
        {
            // Act
            var file = CodeGeneratorHelper.FindFile($"{typeName}.cs");
            var generatedType = await CodeGeneratorHelper.GenerateTypeAsync<ModelGenerator>(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file));
            var instance = activator(generatedType);

            // Assert
            foreach (var expected in propertyValues)
            {
                var property = generatedType.GetProperty(expected.Name);
                var value = property.GetValue(instance);
                Assert.That(value, Is.EqualTo(expected.Value));
            }
        }

        [Test]
        [TestCaseSource(typeof(GeneratorTests), nameof(TestCases))]
        public async Task UseRepositoryGenerator(string typeName, Func<Type, dynamic> activator, (string Name, object Value)[] propertyValues)
        {
            // Act
            var file = CodeGeneratorHelper.FindFile($"{typeName}.cs");
            var generatedType = await CodeGeneratorHelper.GenerateTypeAsync<RepositoryGenerator>(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file));
            var instance = activator(generatedType);

            // Assert
            foreach (var expected in propertyValues)
            {
                var property = generatedType.GetProperty(expected.Name);
                var value = property.GetValue(instance);
                Assert.That(value, Is.EqualTo(expected.Value));
            }
        }

        public static IEnumerable TestCases => new[]
        {
            new TestCaseData(
                nameof(OneProperty),
                new Func<Type, dynamic>(t => Activator.CreateInstance(t, GetConstructorArguments(t).ToArray())),
                new(string, object)[]
                {
                    (nameof(IModelObject.Id), _id),
                    (nameof(IModelObject.Name), "foo"),
                    (nameof(OneProperty.String1), "foo")
                })
        };

        private static IEnumerable<object> GetConstructorArguments(Type type)
        {
            foreach (var parameter in type.GetConstructors().Single().GetParameters())
            {
                if (typeof(IServiceProvider).IsAssignableFrom(parameter.ParameterType))
                {
                    yield return CreateServiceProvider();
                }
                else if (parameter.ParameterType == typeof(string))
                {
                    yield return "foo";
                }
                else if (parameter.ParameterType == typeof(UniqueId))
                {
                    yield return _id;
                }
                else if (parameter.ParameterType.IsInterface)
                {
                    yield return Substitute.For(new[] { parameter.ParameterType }, Array.Empty<object>());
                }
                else if (parameter.ParameterType == typeof(Version))
                {
                    yield return new Version(1, 0);
                }
                else
                {
                    throw new NotSupportedException(parameter.ParameterType.ToString());
                }
            }
        }

        private static IServiceProvider CreateServiceProvider()
        {
            var result = Substitute.For<IServiceProvider>();
            result.GetService(typeof(IValidator)).Returns(Substitute.For<IValidator>());
            result.GetService(typeof(IModelDataAccessorProvider)).Returns(Substitute.For<IModelDataAccessorProvider>());
            result.GetService(typeof(IRepositoryProvider)).Returns(Substitute.For<IRepositoryProvider>());
            result.GetService(typeof(IObjectRepositorySearch)).Returns(Substitute.For<IObjectRepositorySearch>());
            return result;
        }
    }
}
