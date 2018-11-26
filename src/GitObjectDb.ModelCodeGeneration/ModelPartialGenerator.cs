using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GitObjectDb.ModelCodeGeneration
{
    internal class ModelPartialGenerator : PartialGeneratorBase
    {
        private const string InitializeMethodName = "Initialize";

        protected ModelPartialGenerator(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors, CancellationToken cancellationToken)
            : base(descriptor, templateDescriptors, cancellationToken)
        {
        }

        public static TypeDeclarationSyntax Generate(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors, CancellationToken cancellationToken)
        {
            var generator = new ModelPartialGenerator(descriptor, templateDescriptors, cancellationToken);
            return generator.GenerateTypeDeclaration();
        }

        protected override SyntaxList<MemberDeclarationSyntax> GenerateMembers()
        {
            return
                SingletonList<MemberDeclarationSyntax>(
                    GenerateConstructor())
                .Add(
                    GenerateUpdateMethod())
#pragma warning disable SA1515 // Single-line comment must be preceded by blank line
#pragma warning disable SA1005 // Single line comments must begin with single space
                //.AddRange(
                //    GenerateMutators())
#pragma warning restore SA1005
#pragma warning restore SA1515
                .Add(GenerateValidatePartialMethod());
        }

        private ConstructorDeclarationSyntax GenerateConstructor()
        {
            return ConstructorDeclaration(Descriptor.TypeIdentifier)
                .AddModifiers(SyntaxKind.PublicKeyword)
                .WithAttributeLists(
                        SingletonList(
                            AttributeList(
                                SingletonSeparatedList(
                                    Attribute(
                                        QualifiedName(
                                            QualifiedName(
                                                IdentifierName("Newtonsoft"),
                                                IdentifierName("Json")),
                                            IdentifierName("JsonConstructor")))))))
                .WithParameters(
                    TemplateDescriptors.SelectMany(t =>
                        t.ConstructorDeclaration?.ParameterList.Parameters.Concat(
                            Descriptor.Entries.Select(CreateParameter))
                        ?? Enumerable.Empty<ParameterSyntax>()))
                .WithInitializer(
                    CreateCtorInitializer())
                .WithBodyStatements(
                    Descriptor.Entries.Select(CreateCtorAssignment)
                    .Concat(new[] { CreateInitializeInvocation() })
                    .Concat(new[] { CreateValidateInvocation() }));
            ConstructorInitializerSyntax CreateCtorInitializer()
            {
                return
                    ConstructorInitializer(
                        SyntaxKind.ThisConstructorInitializer,
                        ArgumentList(
                            SeparatedList(
                                TemplateDescriptors.SelectMany(t =>
                                    t.ConstructorDeclaration?.ParameterList.Parameters.Select(
                                        p => Argument(IdentifierName(p.Identifier.Text)))
                                    ?? Enumerable.Empty<ArgumentSyntax>()))));
            }
            StatementSyntax CreateCtorAssignment(ModelDescriptor.Entry entry)
            {
                ExpressionSyntax right = IdentifierName(entry.Identifier);
                if (entry.Type.GetText().ToString().Contains("LazyChildren"))
                {
                    right = ConditionalAccessExpression(
                        right,
                        InvocationExpression(
                            MemberBindingExpression(
                                IdentifierName("AttachToParent")))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        ThisExpression())))));
                }
                return
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName(entry.Identifier)),
                            right));
            }
            StatementSyntax CreateInitializeInvocation()
            {
                return
                    ExpressionStatement(
                        InvocationExpression(
                            IdentifierName(InitializeMethodName)));
            }
            StatementSyntax CreateValidateInvocation()
            {
                return
                    ExpressionStatement(
                        InvocationExpression(
                            IdentifierName(Names.Validate)));
            }
        }

        private MethodDeclarationSyntax GenerateUpdateMethod()
        {
            var arguments = Enumerable.Repeat(Argument(IdentifierName("_serviceProvider")), 1).Concat(
                Descriptor.Entries.Select(x =>
                    Argument(
                        IdentifierName(x.Identifier))));
            return MethodDeclaration(Descriptor.Type, Names.Update)
                .AddModifiers(SyntaxKind.PublicKeyword)
                .WithParameters(
                    Descriptor.Entries.Select(CreateParameter))
                .WithBodyStatements(
                    ReturnStatement(
                        ObjectCreationExpression(
                            Descriptor.Type)
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(arguments)))));
        }

        private IEnumerable<MemberDeclarationSyntax> GenerateMutators()
        {
            return Descriptor.Entries.Select(CreateRecordMutator);
            MethodDeclarationSyntax CreateRecordMutator(ModelDescriptor.Entry entry)
            {
                var arguments = Descriptor.Entries.Select(x =>
                {
                    return Argument(
                        IdentifierName(x.Identifier));
                });
                var mutator =
                    MethodDeclaration(
                        Descriptor.Type,
                        GetMutatorIdentifier())
                    .AddModifiers(SyntaxKind.PublicKeyword)
                    .WithParameters(
                        Parameter(
                            entry.Identifier)
                        .WithType(entry.Type))
                    .WithBodyStatements(
                        ReturnStatement(
                            InvocationExpression(
                                IdentifierName(Names.Update))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(arguments)))));
                return mutator;
                SyntaxToken GetMutatorIdentifier()
                {
                    return Identifier($"{Names.WithPrefix}{entry.Identifier.ValueText}");
                }
            }
        }

        private static MemberDeclarationSyntax GenerateValidatePartialMethod()
        {
            return
                MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    Names.Validate)
                .AddModifiers(SyntaxKind.PartialKeyword)
                .WithSemicolonToken();
        }

        private static ParameterSyntax CreateParameter(ModelDescriptor.Entry property)
        {
            return Parameter(
                    property.Identifier)
                .WithType(property.Type);
        }

        private ParameterListSyntax GenerateFullParameterList()
        {
            return ParameterList(
                    SeparatedList(
                        Descriptor.Entries.Select(CreateParameter)));
        }
    }
}
