using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests.Tools
{
    internal static class CodeGeneratorHelper
    {
        internal static async Task<SyntaxList<MemberDeclarationSyntax>> Generate<TGenerator>(ClassDeclarationSyntax type, Document document, Project project, CSharpCompilation compilation)
            where TGenerator : ICodeGenerator, new()
        {
            var generator = new TGenerator();
            var semanticModel = await document.GetSemanticModelAsync();
            var context = new TransformationContext(
                type,
                semanticModel,
                compilation,
                Path.GetDirectoryName(project.FilePath),
                type.Ancestors().OfType<UsingDirectiveSyntax>(),
                type.Ancestors().OfType<ExternAliasDirectiveSyntax>());
            return await generator.GenerateAsync(context, Substitute.For<IProgress<Diagnostic>>(), CancellationToken.None);
        }
    }
}
