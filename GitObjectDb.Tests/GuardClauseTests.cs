using AutoFixture;
using AutoFixture.Idioms;
using AutoFixture.NUnit3;
using GitObjectDb.Attributes;
using GitObjectDb.Migrations;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Migrations;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Tests.Git
{
    public class GuardClauseTests
    {
        /// <summary>
        /// Add missing type to <see cref="CommonTypeProviderCustomization"/> as needed in case of errors.
        /// </summary>
        /// <param name="assertion">The assertion.</param>
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(CommonTypeProviderCustomization), typeof(NSubstituteForAbstractTypesCustomization))]
        public void VerifyGuardForNullClauses(GuardClauseAssertion assertion)
        {
            var types = from t in typeof(IMetadataObject).Assembly.GetTypes()
                        where !t.IsEnum
                        where !typeof(Delegate).IsAssignableFrom(t)
                        where !Attribute.IsDefined(t, typeof(ExcludeFromGuardForNullAttribute))
                        select t;
            assertion.Verify(types);
        }

        /// <summary>
        /// Provides some dummy values so that parameter values can be provided.
        /// </summary>
        class CommonTypeProviderCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                fixture.Register(() => new GuardClauseAssertion(
                    fixture,
                    new FilterCommand(new NullReferenceBehaviorExpectation())));

                fixture.Inject(typeof(string));
                fixture.Inject(ExpressionReflector.GetConstructor(() => new Page(default, default, default, default, default)));
                fixture.Inject(ExpressionReflector.GetProperty<Page>(p => p.Description));
                fixture.Inject((Expression)Expression.Default(typeof(object)));
                fixture.Register<AbstractInstance>(() => fixture.Create<Instance>());
                fixture.Register<AbstractMigration>(() => fixture.Create<Migration>());
                fixture.Register<AbstractModel>(() => fixture.Create<Instance>());
                fixture.Inject<ConstructorParameterBinding.ChildProcessor>((name, children, @new, dataAccessor) => children);
                fixture.Inject<ConstructorParameterBinding.Clone>((@object, predicateReflector, processor) => @object);
                CustomizeGitObjects(fixture);
                CustomizeJsonObjects(fixture);
            }

            static void CustomizeGitObjects(IFixture fixture)
            {
                fixture.Inject<Func<IRepository, Tree>>(r => r.Head.Tip.Tree);
                fixture.Inject(new ObjectId("2fa2540fecec8c4908fb0ccba825cdb903f09440"));
                fixture.Inject(Substitute.For<PatchEntryChanges>());
                fixture.Inject(Substitute.For<TreeEntryChanges>());
            }

            static void CustomizeJsonObjects(IFixture fixture)
            {
                fixture.Inject(JObject.Parse(@"{""a"": ""b""}"));
                fixture.Register<JToken>(() => fixture.Create<JObject>());
            }
        }

        class FilterCommand : IBehaviorExpectation
        {
            readonly IBehaviorExpectation _origin;

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

            static bool IsIteratorCommand(IGuardClauseCommand command)
            {
                return command.GetType().Name.Contains("Iterator");
            }

            static bool IsMethodCommandToBeIgnored(IGuardClauseCommand command)
            {
                var methodInvokeCommand = TryGetCommand<MethodInvokeCommand>(command);
                return methodInvokeCommand != null &&
                    (methodInvokeCommand.ParameterInfo.IsOptional ||
                     Attribute.IsDefined(methodInvokeCommand.ParameterInfo.Member, typeof(ExcludeFromGuardForNullAttribute)) ||
                     ExcludeType(methodInvokeCommand.ParameterInfo.Member.ReflectedType));
            }

            static bool IsPropertySetToBeIgnored(IGuardClauseCommand command)
            {
                var propertySetCommand = TryGetCommand<PropertySetCommand>(command);
                return propertySetCommand != null && ExcludeType(propertySetCommand.PropertyInfo.ReflectedType);
            }

            static TGuardClauseCommand TryGetCommand<TGuardClauseCommand>(IGuardClauseCommand command)
                where TGuardClauseCommand : class, IGuardClauseCommand
            {
                var unwrappingCommand = command as ReflectionExceptionUnwrappingCommand;
                return (unwrappingCommand?.Command ?? command) as TGuardClauseCommand;
            }

            static bool ExcludeType(Type type) =>
                typeof(Exception).IsAssignableFrom(type);
        }
    }
}
