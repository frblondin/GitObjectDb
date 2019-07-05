using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests.Tools
{
    internal static class CodeGeneratorHelper
    {
        private const string DefaultFilePathPrefix = "Test";
        private const string CSharpDefaultFileExt = "cs";
        private const string TestProjectName = "TestProject";

        private static readonly string _coreAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        internal static ImmutableArray<MetadataReference> MetadataReferences { get; } =
            new[]
            {
            "netstandard.dll",
            "System.dll",
            "System.Core.dll",
#if NETCOREAPP
            "System.Private.CoreLib.dll",
#else
            "mscorlib.dll",
#endif
            "System.Runtime.dll",
            }.Select(x => MetadataReference.CreateFromFile(Path.Combine(_coreAssemblyPath, x)))
            .Concat<MetadataReference>(new[]
            {
                typeof(CSharpCompilation).Assembly,
                typeof(CodeGenerationAttributeAttribute).Assembly,
                typeof(DataContractAttribute).Assembly,
                typeof(ModelAttribute).Assembly,
                typeof(Models.IModelObject).Assembly,
                typeof(LibGit2Sharp.Repository).Assembly,
                typeof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions).Assembly,
                typeof(IImmutableList<>).Assembly,
                typeof(Newtonsoft.Json.JsonReader).Assembly,
                typeof(LibGit2Sharp.Repository).Assembly,
                typeof(ExcludeFromCodeCoverageAttribute).Assembly,
                typeof(IServiceProvider).Assembly,
                typeof(Enumerable).Assembly,
            }.Select(x => MetadataReference.CreateFromFile(x.Location)))
            .ToImmutableArray();

        internal static async Task<Type> GenerateTypeAsync<TGenerator>(string source, string name)
            where TGenerator : ICodeGenerator, new()
        {
            var (project, result) = await TransformAsync<TGenerator>(source);

            var solution = AddDocumentToSolution(project, result);
            var compilation = (CSharpCompilation)await solution.Projects.Single().GetCompilationAsync();
            var assembly = GenerateAssembly(compilation);
            return assembly.GetTypes().Single(t => t.Name == name);
        }

        private static Solution AddDocumentToSolution(Project project, SyntaxTree result)
        {
            var newFileName = $"{DefaultFilePathPrefix}_generated.{CSharpDefaultFileExt}";
            var documentId = DocumentId.CreateNewId(project.Id, debugName: newFileName);
            return project.Solution.AddDocument(documentId, newFileName, SourceText.From(result.ToString()));
        }

        public static async Task<(Project, SyntaxTree)> TransformAsync<TGenerator>(string source)
            where TGenerator : ICodeGenerator, new()
        {
            var project = CreateProject(source);
            var document = project.Documents.Single();
            var tree = await document.GetSyntaxTreeAsync();
            var compilation = (CSharpCompilation)await document.Project.GetCompilationAsync();
            var diagnostics = compilation.GetDiagnostics();
            Assert.That(diagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning), Is.Empty);
            var progress = new Progress<Diagnostic>();
            var typeSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var result = await TransformAsync<TGenerator>(compilation, tree, progress);
            return (project, result);
        }

        public static async Task<SyntaxTree> TransformAsync<TGenerator>(
            CSharpCompilation compilation, SyntaxTree inputDocument, IProgress<Diagnostic> progress)
            where TGenerator : ICodeGenerator, new()
        {
            var inputSemanticModel = compilation.GetSemanticModel(inputDocument);
            var inputCompilationUnit = inputDocument.GetCompilationUnitRoot();

            var emittedExterns = inputCompilationUnit
                .Externs
                .Select(x => x.WithoutTrivia())
                .ToImmutableArray();

            var emittedUsings = inputCompilationUnit
                .Usings
                .Select(x => x.WithoutTrivia())
                .ToImmutableArray();

            var emittedAttributeLists = ImmutableArray<AttributeListSyntax>.Empty;
            var emittedMembers = ImmutableArray<MemberDeclarationSyntax>.Empty;

            var memberNodes = inputDocument
                .GetRoot()
                .DescendantNodesAndSelf(n => n is CompilationUnitSyntax || n is NamespaceDeclarationSyntax || n is TypeDeclarationSyntax)
                .OfType<CSharpSyntaxNode>();

            foreach (var memberNode in memberNodes)
            {
                var generator = new TGenerator();
                var context = new TransformationContext(
                    memberNode,
                    inputSemanticModel,
                    compilation,
                    null,
                    emittedUsings,
                    emittedExterns);

                var richGenerator = generator as IRichCodeGenerator ?? new EnrichingCodeGeneratorProxy(generator);

                var emitted = await richGenerator.GenerateRichAsync(context, progress, CancellationToken.None);

                emittedExterns = emittedExterns.AddRange(emitted.Externs);
                emittedUsings = emittedUsings.AddRange(emitted.Usings);
                emittedAttributeLists = emittedAttributeLists.AddRange(emitted.AttributeLists);
                emittedMembers = emittedMembers.AddRange(emitted.Members);
            }

            var compilationUnit =
                SyntaxFactory.CompilationUnit(
                        SyntaxFactory.List(emittedExterns),
                        SyntaxFactory.List(emittedUsings),
                        SyntaxFactory.List(emittedAttributeLists),
                        SyntaxFactory.List(emittedMembers))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                    .NormalizeWhitespace();

            return compilationUnit.SyntaxTree;
        }

        private static Assembly GenerateAssembly(CSharpCompilation compilation)
        {
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                Assert.That(result.Diagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning), Is.Empty);
                ms.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(ms.ToArray());
            }
        }

        private static Project CreateProject(params string[] sources)
        {
            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithProjectParseOptions(projectId, new CSharpParseOptions(preprocessorSymbols: new[] { "SOMETHING_ACTIVE" }))
                .AddMetadataReferences(projectId, MetadataReferences);

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = DefaultFilePathPrefix + count + "." + CSharpDefaultFileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }
            return solution.GetProject(projectId);
        }

        internal static string FindFile(string fileName)
        {
            var solutionPath = PathTools.FindParentDirectoryFile(
                new DirectoryInfo(TestContext.CurrentContext.TestDirectory),
                "*.sln");
            return Directory.EnumerateFiles(
                Path.GetDirectoryName(solutionPath),
                fileName,
                SearchOption.AllDirectories).Single();
        }
    }
}
