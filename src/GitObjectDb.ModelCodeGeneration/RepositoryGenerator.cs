using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration
{
    public class RepositoryGenerator : ModelGenerator
    {
#pragma warning disable CA1801
        public RepositoryGenerator(AttributeData attributeData)
#pragma warning restore CA1801
            : base(attributeData)
        {
        }

        public RepositoryGenerator()
            : base()
        {
        }

        internal override IEnumerable<TemplateDescriptor> GetTemplateDescriptors()
        {
            return base.GetTemplateDescriptors().Concat(new[]
            {
                new TemplateDescriptor("GitObjectDb.ModelCodeGeneration.Resources.RepositoryTemplate.cs")
            });
        }

        internal override ModelDescriptor GetDescriptor(ClassDeclarationSyntax classDeclaration, ImmutableArray<TemplateDescriptor> templates)
        {
            var descriptor = base.GetDescriptor(classDeclaration, templates);
            return AddContainerProperty(descriptor);
        }

        private ModelDescriptor AddContainerProperty(ModelDescriptor descriptor)
        {
            var containerProperty = _modelTemplateDescriptor.TypeDeclaration.Members.OfType<PropertyDeclarationSyntax>()
                    .Single(p => p.Identifier.Text == "Container")
                    .ToRecordEntry();
            var entries = descriptor.Entries.Insert(0, containerProperty);

            return descriptor.WithEntries(entries);
        }
    }
}
