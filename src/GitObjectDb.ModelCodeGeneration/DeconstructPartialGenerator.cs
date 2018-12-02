// Original work Copyright (c) 2018 https://github.com/amis92/RecordGenerator

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GitObjectDb.ModelCodeGeneration
{
    internal class DeconstructPartialGenerator : PartialGeneratorBase
    {
        protected DeconstructPartialGenerator(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors, CancellationToken cancellationToken)
            : base(descriptor, templateDescriptors, cancellationToken)
        {
        }

        public static TypeDeclarationSyntax Generate(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors, CancellationToken cancellationToken)
        {
            var generator = new DeconstructPartialGenerator(descriptor, templateDescriptors, cancellationToken);
            return generator.GenerateTypeDeclaration();
        }

        protected override SyntaxList<MemberDeclarationSyntax> GenerateMembers()
        {
            return SingletonList(GenerateDeconstruct());
        }

        private MemberDeclarationSyntax GenerateDeconstruct()
        {
            return
                MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)), Names.Deconstruct)
                .AddModifiers(SyntaxKind.PublicKeyword)
                .WithParameters(
                    Descriptor.Entries.Select(CreateParameter))
                .WithBodyStatements(
                    Descriptor.Entries.Select(CreateAssignment));
            ParameterSyntax CreateParameter(ModelDescriptor.Entry entry)
            {
                return
                    Parameter(entry.Identifier.ToLowerFirstLetter())
                    .WithType(entry.Type)
                    .AddModifiers(Token(SyntaxKind.OutKeyword));
            }

            StatementSyntax CreateAssignment(ModelDescriptor.Entry entry)
            {
                return
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(entry.Identifier.ToLowerFirstLetter()),
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName(entry.Identifier))));
            }
        }
    }
}
