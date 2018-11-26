using GitObjectDb.ModelCodeGeneration.Tests.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests
{
    [SetUpFixture]
    public class TestProjectFixture
    {
        public static IList<WorkspaceDiagnostic> Diagnostics { get; private set; }

        public static MSBuildWorkspace Workspace { get; private set; }

        public static Project Project { get; private set; }

        public static CSharpCompilation Compilation { get; private set; }

        public static IServiceProvider ServiceProvider { get; } = Substitute.For<IServiceProvider>();

        [OneTimeSetUp]
        public async Task LoadProject()
        {
            EnsureViableAssembliesAreLoaded();

            var path = PathTools.FindParentDirectoryFile(
                new DirectoryInfo(TestContext.CurrentContext.TestDirectory), "*.sln");
            (Diagnostics, Workspace, Project, Compilation) = await MSBuildHelper.LoadProjectAsync(
                path, "GitObjectDb.ModelCodeGeneration.Tests");
        }

        private static void EnsureViableAssembliesAreLoaded()
        {
            Console.WriteLine($"{typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly.FullName} loaded.");
            Console.WriteLine($"{typeof(ModelAttribute).Assembly.FullName} loaded.");
            Console.WriteLine($"{typeof(ValueTuple).Assembly.FullName} loaded.");
            Console.WriteLine($"{typeof(Models.IModelObject).Assembly.FullName} loaded.");
            Console.WriteLine($"{typeof(LibGit2Sharp.Repository).Assembly.FullName} loaded.");
            Console.WriteLine($"{typeof(Newtonsoft.Json.JsonReader).Assembly.FullName} loaded.");
            Console.WriteLine($"{typeof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions).Assembly.FullName} loaded.");
            Console.WriteLine($"{typeof(IImmutableList<>).Assembly.FullName} loaded.");
        }

        [OneTimeTearDown]
        public void Unload()
        {
            Workspace?.Dispose();
        }
    }
}
