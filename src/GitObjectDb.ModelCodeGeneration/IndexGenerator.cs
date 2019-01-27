using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitObjectDb.ModelCodeGeneration
{
    public class IndexGenerator : ModelGenerator
    {
        internal readonly TemplateDescriptor _indexTemplateDescriptor = new TemplateDescriptor("GitObjectDb.ModelCodeGeneration.Resources.IndexTemplate.cs");

#pragma warning disable CA1801
        public IndexGenerator(AttributeData attributeData)
#pragma warning restore CA1801
            : base(attributeData)
        {
        }

        public IndexGenerator()
            : base()
        {
        }

        internal override IEnumerable<TemplateDescriptor> GetTemplateDescriptors()
        {
            return base.GetTemplateDescriptors().Concat(new[]
            {
                _indexTemplateDescriptor
            });
        }

        internal override ModelDescriptor GetDescriptor(ClassDeclarationSyntax classDeclaration, ImmutableArray<TemplateDescriptor> templates)
        {
            var descriptor = base.GetDescriptor(classDeclaration, templates);
            return AddContainerProperty(descriptor);
        }

        private ModelDescriptor AddContainerProperty(ModelDescriptor descriptor)
        {
            var valuesProperty = _indexTemplateDescriptor.TypeDeclaration.Members.OfType<PropertyDeclarationSyntax>()
                    .Single(p => p.Identifier.Text == "Values")
                    .ToRecordEntry(true);
            var entries = descriptor.Entries.Add(valuesProperty);

            return descriptor.WithEntries(entries);
        }
    }
}
