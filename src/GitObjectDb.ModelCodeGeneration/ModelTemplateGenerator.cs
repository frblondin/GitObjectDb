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
    internal class ModelTemplateGenerator
    {
        public static IEnumerable<MemberDeclarationSyntax> Generate(ModelDescriptor descriptor, ImmutableArray<TemplateDescriptor> templateDescriptors)
        {
            foreach (var template in templateDescriptors)
            {
                var visited = new TemplateRewriter(descriptor).Visit(template.TypeDeclaration);
                yield return (MemberDeclarationSyntax)visited;
            }
        }

        private class TemplateRewriter : CSharpSyntaxRewriter
        {
            private readonly ModelDescriptor _descriptor;

            public TemplateRewriter(ModelDescriptor descriptor)
            {
                _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var toBeRemoved = GetMembersToRemove(node);
                var updated = node.WithMembers(RemoveMembers(node, toBeRemoved));
                if (IsModelTemplate(node.Identifier))
                {
                    updated = updated.WithIdentifier(Identifier(_descriptor.TypeIdentifier.Text));
                }
                return base.VisitClassDeclaration(updated);
            }

            private IList<PropertyDeclarationSyntax> GetMembersToRemove(ClassDeclarationSyntax node)
            {
                var modelTypeProperties = _descriptor.TypeDeclaration.Members.OfType<PropertyDeclarationSyntax>();
                var toBeRemoved = node.Members.OfType<PropertyDeclarationSyntax>().Where(
                    p => modelTypeProperties.Any(mp => mp.Identifier.Text == p.Identifier.Text));
                return toBeRemoved.ToList();
            }

            private static SyntaxList<MemberDeclarationSyntax> RemoveMembers(ClassDeclarationSyntax node, IList<PropertyDeclarationSyntax> toBeRemoved)
            {
                var members = node.Members;
                foreach (var remove in toBeRemoved)
                {
                    members = members.Remove(remove);
                }

                return members;
            }

            public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                return IsModelTemplate(node.Identifier) ?
                    base.VisitConstructorDeclaration(node.WithIdentifier(Identifier(_descriptor.TypeIdentifier.Text))) :
                    base.VisitConstructorDeclaration(node);
            }

            private bool IsModelTemplate(SyntaxToken token) => token.Text == "ModelTemplate";
        }
    }
}
