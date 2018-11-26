using CodeGeneration.Roslyn;
using System;
using System.Diagnostics;

namespace GitObjectDb
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [CodeGenerationAttribute("GitObjectDb.ModelCodeGeneration.RepositoryGenerator, GitObjectDb.ModelCodeGeneration")]
    [Conditional("CodeGeneration")]
    public sealed class RepositoryAttribute : Attribute
    {
    }
}