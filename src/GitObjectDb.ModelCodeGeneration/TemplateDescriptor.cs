using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.ModelCodeGeneration
{
    internal sealed class TemplateDescriptor
    {
        internal TemplateDescriptor(string resourceName)
        {
            if (resourceName == null)
            {
                throw new ArgumentNullException(nameof(resourceName));
            }
            var model = GetResourceContent(resourceName);
            SyntaxNode = CSharpSyntaxTree.ParseText(model).GetRoot();
            TypeDeclaration = SyntaxNode.DescendantNodes().OfType<TypeDeclarationSyntax>().First();
            ConstructorDeclaration = TypeDeclaration.Members.OfType<ConstructorDeclarationSyntax>().SingleOrDefault();
        }

        public SyntaxNode SyntaxNode { get; }

        public TypeDeclarationSyntax TypeDeclaration { get; }

        public ConstructorDeclarationSyntax ConstructorDeclaration { get; }

        private static string GetResourceContent(string resource)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
