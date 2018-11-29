using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GitObjectDb.ModelCodeGeneration
{
    internal static class ModelDescriptorExtensions
    {
        public static ModelDescriptor.Entry ToRecordEntry(this PropertyDeclarationSyntax property)
        {
            return new ModelDescriptor.SimpleEntry(
                property.Identifier.WithoutTrivia(),
                property.Type.WithoutTrivia());
        }

        public static ModelDescriptor ToRecordDescriptor(this ClassDeclarationSyntax typeDeclaration, ImmutableArray<TemplateDescriptor> templateDescriptors)
        {
            return new ModelDescriptor(
                typeDeclaration.GetTypeSyntax().WithoutTrivia(),
                typeDeclaration.Identifier.WithoutTrivia(),
                typeDeclaration.GetEntries(templateDescriptors),
                typeDeclaration.WithoutTrivia());
        }

        public static ModelDescriptor ToRecordDescriptor(this StructDeclarationSyntax typeDeclaration, ImmutableArray<TemplateDescriptor> templateDescriptors)
        {
            return new ModelDescriptor(
                typeDeclaration.GetTypeSyntax().WithoutTrivia(),
                typeDeclaration.Identifier.WithoutTrivia(),
                typeDeclaration.GetEntries(templateDescriptors),
                typeDeclaration.WithoutTrivia());
        }

        private static ImmutableArray<ModelDescriptor.Entry> GetEntries(this TypeDeclarationSyntax typeDeclaration, ImmutableArray<TemplateDescriptor> templateDescriptors)
        {
            return templateDescriptors.SelectMany(t => t.TypeDeclaration.Members.GetRecordProperties().AsRecordEntries()).Concat(
                typeDeclaration.Members.GetRecordProperties().AsRecordEntries())
                .ToImmutableArray();
        }

        private static IEnumerable<PropertyDeclarationSyntax> GetRecordProperties(this SyntaxList<MemberDeclarationSyntax> members)
        {
            return members
                .OfType<PropertyDeclarationSyntax>()
                .Where(
                    propSyntax => propSyntax.IsRecordViable());
        }

        private static IEnumerable<ModelDescriptor.Entry> AsRecordEntries(this IEnumerable<PropertyDeclarationSyntax> properties)
        {
            return properties
                .Select(p => p.ToRecordEntry());
        }

        public static QualifiedNameSyntax ToNestedBuilderType(this NameSyntax type)
        {
            return QualifiedName(
                    type,
                    IdentifierName(Names.Builder));
        }

        public static SyntaxToken ToLowerFirstLetter(this SyntaxToken identifier)
        {
            return Identifier(identifier.Text.ToLowerFirstLetter());
        }

        public static string ToLowerFirstLetter(this string name)
        {
            return string.IsNullOrEmpty(name)
                ? name
                : $"{char.ToLowerInvariant(name[0])}{name.Substring(1)}";
        }
    }
}
