using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests.Tools
{
    internal static class MSBuildHelper
    {
        static readonly Lazy<VisualStudioInstance> _visualStudioInstance = new Lazy<VisualStudioInstance>(() =>
            MSBuildLocator.QueryVisualStudioInstances()
                .FirstOrDefault(i => i.Version.Major == 15 && i.Version.Minor == 0));

        internal static VisualStudioInstance VisualStudioInstance => _visualStudioInstance.Value;

        internal static async Task<(IList<WorkspaceDiagnostic>, MSBuildWorkspace, Project, CSharpCompilation)> LoadProjectAsync(string solutionPath, string projectName)
        {
            var workspace = MSBuildWorkspace.Create();
            var diagnostics = new List<WorkspaceDiagnostic>();
            workspace.WorkspaceFailed += (sender, e) => diagnostics.Add(e.Diagnostic);
            var ignored = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);
            ignored = typeof(ModelAttribute);
            ignored = typeof(ValueTuple);
            var solution = await workspace.OpenSolutionAsync(solutionPath);

            var project = solution.Projects.Single(p => p.Name == projectName);
            var compilation = (CSharpCompilation)await project.GetCompilationAsync();

            return (diagnostics, workspace, project, compilation);
        }
    }
}
