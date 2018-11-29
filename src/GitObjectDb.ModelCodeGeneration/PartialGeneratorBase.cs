using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GitObjectDb.ModelCodeGeneration
{
    internal abstract class PartialGeneratorBase
    {
        protected PartialGeneratorBase(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors, CancellationToken cancellationToken)
        {
            Descriptor = descriptor;
            TemplateDescriptors = templateDescriptors;
            CancellationToken = cancellationToken;
        }

        protected ModelDescriptor Descriptor { get; }

        protected ImmutableArray<TemplateDescriptor> TemplateDescriptors { get; }

        protected CancellationToken CancellationToken { get; }

        public virtual ClassDeclarationSyntax GenerateTypeDeclaration()
        {
            return
                ClassDeclaration(
                    GenerateTypeIdentifier())
                .WithTypeParameterList(
                    GenerateTypeParameterList())
                .WithBaseList(
                    GenerateBaseList())
                .WithModifiers(
                    GenerateModifiers())
                .WithMembers(
                    GenerateMembers());
        }

        protected virtual TypeParameterListSyntax GenerateTypeParameterList()
        {
            return Descriptor.TypeDeclaration.TypeParameterList?.WithoutTrivia();
        }

        protected virtual SyntaxToken GenerateTypeIdentifier()
        {
            return Descriptor.TypeIdentifier.WithoutTrivia();
        }

        protected virtual SyntaxTokenList GenerateModifiers()
        {
            return TokenList(Token(SyntaxKind.PartialKeyword));
        }

        protected virtual SyntaxList<MemberDeclarationSyntax> GenerateMembers()
        {
            return List<MemberDeclarationSyntax>();
        }

        protected virtual BaseListSyntax GenerateBaseList()
        {
            return null;
        }
    }
}
