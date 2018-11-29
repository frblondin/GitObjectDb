using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration.Tests.Tools
{
    internal static class InMemoryCompiler
    {
        internal static Type Compile(IEnumerable<SyntaxTree> trees, Type unmodifiedType)
        {
            var assemblyName = Path.GetRandomFileName();
            var references = (from a in AppDomain.CurrentDomain.GetAssemblies()
                              where !a.IsDynamic
                              where !string.IsNullOrEmpty(a.Location)
                              select MetadataReference.CreateFromFile(a.Location)).ToList();
            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: trees,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var ms = new MemoryStream())
            {
                var emitted = compilation.Emit(ms);

                if (!emitted.Success)
                {
#pragma warning disable CA2201 // Do not raise reserved exception types
                    var failures = (from d in emitted.Diagnostics
                                    where d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error
                                    select new Exception($"{d.Id}: {d.GetMessage()}")).ToList();
#pragma warning restore CA2201 // Do not raise reserved exception types
                    throw failures.Count == 1 ? failures.First() : throw new AggregateException("Error while compiling dynamic assembly.", failures);
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return Assembly.Load(ms.ToArray()).GetTypes().Single(t => t.Name == unmodifiedType.Name);
                }
            }
        }
    }
}
