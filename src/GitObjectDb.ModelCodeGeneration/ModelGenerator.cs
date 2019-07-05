// Original work Copyright (c) 2018 https://github.com/amis92/RecordGenerator

using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.ModelCodeGeneration
{
    public class ModelGenerator : ICodeGenerator
    {
        internal readonly TemplateDescriptor _modelTemplateDescriptor = new TemplateDescriptor("GitObjectDb.ModelCodeGeneration.Resources.ModelTemplate.cs");

#pragma warning disable CA1801
        public ModelGenerator(AttributeData attributeData)
#pragma warning restore CA1801
            : this()
        {
        }

        public ModelGenerator()
        {
        }

        internal virtual IEnumerable<TemplateDescriptor> GetTemplateDescriptors()
        {
            yield return _modelTemplateDescriptor;
        }

        internal virtual ModelDescriptor GetDescriptor(ClassDeclarationSyntax classDeclaration, ImmutableArray<TemplateDescriptor> templates)
        {
            return classDeclaration.ToRecordDescriptor(templates);
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var templates = GetTemplateDescriptors().ToImmutableArray();

            var generatedMembers = SyntaxFactory.List<MemberDeclarationSyntax>();
            if (context.ProcessingNode is ClassDeclarationSyntax classDeclaration)
            {
                var descriptor = GetDescriptor(classDeclaration, templates);
                generatedMembers = generatedMembers.AddRange(GenerateRecordPartials(descriptor));
            }
            return Task.FromResult(generatedMembers);
            IEnumerable<MemberDeclarationSyntax> GenerateRecordPartials(ModelDescriptor descriptor)
            {
                foreach (var transformedTemplate in ModelTemplateGenerator.Generate(descriptor, templates))
                {
                    yield return transformedTemplate;
                }
                yield return ModelPartialGenerator.Generate(descriptor, templates, cancellationToken);
                yield return BuilderPartialGenerator.Generate(descriptor, templates, cancellationToken);
                yield return DeconstructPartialGenerator.Generate(descriptor, templates, cancellationToken);
            }
        }
    }
}
