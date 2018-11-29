using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests.Tools
{
    internal static class ProjectExtensions
    {
        internal static async Task<Type> GenerateTypeAsync<TGenerator>(this Project source, Type unmodifiedType, CSharpCompilation compilation, MSBuildWorkspace workspace)
            where TGenerator : ICodeGenerator, new()
        {
            var modifiedTrees = await GenerateTypeSyntaxAsync<TGenerator>(source, unmodifiedType.Name, compilation, workspace);
            return InMemoryCompiler.Compile(modifiedTrees, unmodifiedType);
        }

        static async Task<IList<SyntaxTree>> GenerateTypeSyntaxAsync<TGenerator>(Project source, string name, CSharpCompilation compilation, MSBuildWorkspace workspace)
            where TGenerator : ICodeGenerator, new()
        {
            var (type, tree, document) = await source.FindTypeAsync($"{name}.cs", name);

            var result = await CodeGeneratorHelper.Generate<TGenerator>(type, document, source, compilation);
            var formattedResult = result.Select(m => (MemberDeclarationSyntax)Microsoft.CodeAnalysis.Formatting.Formatter.Format(m, workspace)).ToArray();
            var @namespace = type.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            var modifiedNamespace = @namespace.AddMembers(formattedResult);
            var modifiedTree = new PredicateRewriter((n, baseVisitor) => n == @namespace ? modifiedNamespace : baseVisitor(n))
                .Visit(tree.GetCompilationUnitRoot());

            return new[] { modifiedTree.SyntaxTree };
        }

        internal static async Task<(ClassDeclarationSyntax, CSharpSyntaxTree, Document)> FindTypeAsync(this Project source, string documentName, string name)
        {
            var document = source.Documents.Single(d => d.Name == documentName);
            var tree = (CSharpSyntaxTree)await document.GetSyntaxTreeAsync();
            var type = tree.GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == name) ??
                throw new KeyNotFoundException($"Type with name '{name}' could not be found.");
            return (type, tree, document);
        }
    }
}
