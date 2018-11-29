using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace GitObjectDb.ModelCodeGeneration
{
    internal partial class ModelDescriptor
    {
        public ModelDescriptor(TypeSyntax type, SyntaxToken typeIdentifier, ImmutableArray<Entry> entries, TypeDeclarationSyntax typeDeclaration)
        {
            Type = type;
            TypeIdentifier = typeIdentifier;
            Entries = entries;
            TypeDeclaration = typeDeclaration;
        }

        public TypeSyntax Type { get; }

        public SyntaxToken TypeIdentifier { get; }

        public ImmutableArray<Entry> Entries { get; }

        public TypeDeclarationSyntax TypeDeclaration { get; }

        public ModelDescriptor WithEntries(ImmutableArray<Entry> entries)
        {
            return new ModelDescriptor(Type, TypeIdentifier, entries, TypeDeclaration);
        }

        internal abstract class Entry
        {
            public Entry(SyntaxToken identifier, TypeSyntax type)
            {
                Identifier = identifier;
                Type = type;
            }

            public SyntaxToken Identifier { get; }

            public TypeSyntax Type { get; }
        }

        internal class SimpleEntry : Entry
        {
            public SimpleEntry(SyntaxToken identifier, TypeSyntax type)
                : base(identifier, type)
            {
            }
        }
    }
}
