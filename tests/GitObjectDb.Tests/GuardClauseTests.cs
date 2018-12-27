using AutoFixture;
using AutoFixture.Idioms;
using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Models.Migration;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Validations;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests
{
    public class GuardClauseTests
    {
        private static Assembly Assembly { get; } = typeof(IModelObject).Assembly;

        /// <summary>
        /// Add missing type to <see cref="CommonTypeProviderCustomization"/> as needed in case of errors.
        /// </summary>
        /// <param name="fixture">The fixture.</param>
        /// <param name="assertion">The assertion.</param>
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(CommonTypeProviderCustomization), typeof(NSubstituteForAbstractTypesCustomization))]
        public void VerifyGuardForNullClauses(IFixture fixture, GuardClauseAssertion assertion)
        {
            fixture.Customizations.OfType<NSubstituteForAbstractTypesCustomization>().Single().ExcludeEnumerableTypes = false;
            var types = from t in Assembly.GetTypes()
                        where !t.IsEnum
                        where !typeof(Delegate).IsAssignableFrom(t)
                        where !Attribute.IsDefined(t, typeof(ExcludeFromGuardForNullAttribute))
                        where !t.Name.Contains("Template")
                        where t.GetTypeInfo().DeclaredConstructors.Any(c => c.IsPublic)
                        select t;
            assertion.Verify(types);
        }

        /// <summary>
        /// Provides some dummy values so that parameter values can be provided.
        /// </summary>
        private class CommonTypeProviderCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                fixture.Register(() => new GuardClauseAssertion(
                    fixture,
                    new FilterCommand(new NullReferenceBehaviorExpectation())));

                CustomizeModelObjects(fixture);
                CustomizeExpressionObjects(fixture);
                CustomizeIModelObject(fixture);
                CustomizeGitObjects(fixture);
                CustomizeValidationObjects(fixture);
            }

            private static void CustomizeModelObjects(IFixture fixture)
            {
                fixture.Inject(new RepositoryDescription(RepositoryFixture.SmallRepositoryPath));
                fixture.Register(UniqueId.CreateNew);
                fixture.Register<IObjectRepository>(fixture.Create<ObjectRepository>);
                fixture.Register<IMigration>(fixture.Create<DummyMigration>);
                fixture.Register<IModelObject>(fixture.Create<ObjectRepository>);
                fixture.Inject(fixture.Create<IServiceProvider>().GetRequiredService<ModelDataAccessorFactory>().Invoke(typeof(Page)));
                fixture.Inject<ConstructorParameterBinding.ChildProcessor>((name, children, @new, dataAccessor) => children);
                fixture.Inject<ConstructorParameterBinding.Clone>((@object, predicateReflector, processor) => @object);
                fixture.Inject((ObjectRepositoryContainer)fixture.Create<IServiceProvider>().GetRequiredService<IObjectRepositoryContainerFactory>().Create<ObjectRepository>(RepositoryFixture.SmallRepositoryPath));
            }

            private static void CustomizeExpressionObjects(IFixture fixture)
            {
                fixture.Inject(typeof(Page));
                fixture.Inject(ExpressionReflector.GetConstructor(() => new Page(default, default, default, default, default)));
                fixture.Inject(ExpressionReflector.GetProperty<Page>(p => p.Description));
                fixture.Inject((Expression)Expression.Default(typeof(object)));
                fixture.Inject((LambdaExpression)Expression.Lambda<Action>(Expression.Empty()));
            }

            private static void CustomizeIModelObject(IFixture fixture)
            {
                var modelObject = Substitute.For<IModelObject>();
                modelObject.Parent.Returns(default(IModelObject));
                fixture.Inject(modelObject);

                var objectRepository = Substitute.For<IObjectRepository>();
                objectRepository.CommitId.Returns(new ObjectId("5aac67e2cd74bb5df7e3ae23d803412b1004d12d"));
                fixture.Inject(objectRepository);
            }

            private static void CustomizeGitObjects(IFixture fixture)
            {
                fixture.Inject<Func<IRepository, Tree>>(r => r.Head.Tip.Tree);
                fixture.Inject(new ObjectId("2fa2540fecec8c4908fb0ccba825cdb903f09440"));
                fixture.Inject(Substitute.For<PatchEntryChanges>());
                fixture.Inject(Substitute.For<TreeEntryChanges>());
            }

            private static void CustomizeValidationObjects(IFixture fixture)
            {
                fixture.Register(() => new ValidationContext(fixture.Create<IModelObject>(), ValidationChain.Empty, ValidationRules.All));
            }
        }

        private class FilterCommand : IBehaviorExpectation
        {
            private readonly IBehaviorExpectation _origin;

            public FilterCommand(IBehaviorExpectation origin)
            {
                _origin = origin;
            }

            public void Verify(IGuardClauseCommand command)
            {
                if (command == null)
                {
                    throw new ArgumentNullException(nameof(command));
                }

                if (IsIteratorCommand(command) ||
                    IsMethodCommandToBeIgnored(command) ||
                    IsPropertySetToBeIgnored(command))
                {
                    return;
                }

                _origin.Verify(command);
            }

            private static bool IsIteratorCommand(IGuardClauseCommand command)
            {
                return command.GetType().Name.Contains("Iterator");
            }

            private static bool IsMethodCommandToBeIgnored(IGuardClauseCommand command)
            {
                var methodInvokeCommand = TryGetCommand<MethodInvokeCommand>(command);
                var method = (MethodBase)methodInvokeCommand?.ParameterInfo.Member;
                if (method == null)
                {
                    return false;
                }
                return method.DeclaringType.Assembly != Assembly || // Filter out inherited methods not being overridden
                       method.IsSpecialName ||
                       methodInvokeCommand.ParameterInfo.IsOptional ||
                       Attribute.IsDefined(methodInvokeCommand.ParameterInfo.Member, typeof(ExcludeFromGuardForNullAttribute)) ||
                       ExcludeType(methodInvokeCommand.ParameterInfo.Member.ReflectedType);
            }

            private static bool IsPropertySetToBeIgnored(IGuardClauseCommand command)
            {
                var propertySetCommand = TryGetCommand<PropertySetCommand>(command);
                return propertySetCommand != null &&
                    (ExcludeType(propertySetCommand.PropertyInfo.ReflectedType) ||
                    propertySetCommand.PropertyInfo.DeclaringType.Assembly != Assembly);
            }

            private static TGuardClauseCommand TryGetCommand<TGuardClauseCommand>(IGuardClauseCommand command)
                where TGuardClauseCommand : class, IGuardClauseCommand
            {
                var unwrappingCommand = command as ReflectionExceptionUnwrappingCommand;
                return (unwrappingCommand?.Command ?? command) as TGuardClauseCommand;
            }

            private static bool ExcludeType(Type type) => typeof(Exception).IsAssignableFrom(type);
        }
    }
}
