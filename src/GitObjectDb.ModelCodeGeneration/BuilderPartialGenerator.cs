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
    internal class BuilderPartialGenerator : PartialGeneratorBase
    {
        internal const string ServiceProviderCamelCase = "serviceProvider";

        protected BuilderPartialGenerator(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors, CancellationToken cancellationToken)
            : base(descriptor, templateDescriptors, cancellationToken)
        {
        }

        public ImmutableArray<(ParameterSyntax Parameter, IdentifierNameSyntax FieldOrProperty)> AdditionalParameters { get; }

        public static TypeDeclarationSyntax Generate(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors, CancellationToken cancellationToken)
        {
            var generator = new BuilderPartialGenerator(descriptor, templateDescriptors, cancellationToken);
            return generator.GenerateTypeDeclaration();
        }

        protected override SyntaxList<MemberDeclarationSyntax> GenerateMembers()
        {
            return
                SingletonList<MemberDeclarationSyntax>(
                    GenerateToBuilderMethod())
                .Add(GenerateBuilder());
        }

        private ClassDeclarationSyntax GenerateBuilder()
        {
            return
                ClassDeclaration(Names.Builder)
                .AddModifiers(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword)
                .WithMembers(GenerateBuilderMembers());

            SyntaxList<MemberDeclarationSyntax> GenerateBuilderMembers()
            {
                return List<MemberDeclarationSyntax>()
                    .AddRange(Descriptor.Entries.SelectMany(GetPropertyMembers))
                    .Add(GetBuilderToImmutableMethod())
                    .Add(GetServiceProviderField())
                    .Add(GetBuilderConstructor());
            }
        }

        private IEnumerable<MemberDeclarationSyntax> GetPropertyMembers(ModelDescriptor.Entry entry)
        {
            return CreateSimpleProperty();

            IEnumerable<PropertyDeclarationSyntax> CreateSimpleProperty()
            {
                yield return
                    PropertyDeclaration(
                        entry.Type,
                        entry.Identifier)
                    .AddModifiers(SyntaxKind.PublicKeyword)
                    .WithAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken());
            }
        }

        private MethodDeclarationSyntax GetBuilderToImmutableMethod()
        {
            return
                MethodDeclaration(
                    Descriptor.Type,
                    Names.ToImmutable)
                .AddModifiers(SyntaxKind.PublicKeyword)
                .WithBody(
                    Block(
                        ReturnStatement(
                            ObjectCreationExpression(Descriptor.Type)
                            .WithArgumentList(
                                CreateArgumentList()))));
            ArgumentListSyntax CreateArgumentList()
            {
                return
                    ArgumentList(
                        SeparatedList(
                            Enumerable.Repeat(Argument(IdentifierName($"_{ServiceProviderCamelCase}")), 1).Concat(
                            Descriptor.Entries.Select(
                                entry => Argument(IdentifierName(entry.Identifier))))));
            }
        }

        private MethodDeclarationSyntax GenerateToBuilderMethod()
        {
            return MethodDeclaration(
                    IdentifierName(Names.Builder),
                    Names.ToBuilder)
                .AddModifiers(SyntaxKind.PublicKeyword)
                .WithBody(
                    Block(
                        CreateStatements()));
            IEnumerable<StatementSyntax> CreateStatements()
            {
                yield return
                    ReturnStatement(
                        ObjectCreationExpression(
                            IdentifierName(Names.Builder))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(new[]
                                {
                                    Argument(
                                        IdentifierName($"_{ServiceProviderCamelCase}"))
                                })))
                        .WithInitializer(
                            InitializerExpression(
                                SyntaxKind.ObjectInitializerExpression,
                                SeparatedList(
                                    Descriptor.Entries.Select(CreateInitializerForEntry)))));
            }

            ExpressionSyntax CreateInitializerForEntry(ModelDescriptor.Entry entry)
            {
                return
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(entry.Identifier),
                        IdentifierName(entry.Identifier));
            }
        }

        private static FieldDeclarationSyntax GetServiceProviderField()
        {
            return FieldDeclaration(
                VariableDeclaration(
                    IdentifierName(nameof(IServiceProvider)))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier($"_{ServiceProviderCamelCase}")))))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.ReadOnlyKeyword)));
        }

        private static ConstructorDeclarationSyntax GetBuilderConstructor()
        {
            return ConstructorDeclaration(Names.Builder)
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                Identifier(ServiceProviderCamelCase))
                            .WithType(
                                IdentifierName(nameof(IServiceProvider))))))
                .WithBody(
                    Block(
                        SingletonList<StatementSyntax>(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName($"_{ServiceProviderCamelCase}"),
                                    IdentifierName(ServiceProviderCamelCase))))));
        }
    }
}
