using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace GitObjectDb.ModelCodeGeneration.Tests.Tools
{
    internal class PredicateRewriter : CSharpSyntaxRewriter
    {
        public PredicateRewriter(Func<SyntaxNode, Func<SyntaxNode, SyntaxNode>, SyntaxNode> function)
        {
            Function = function;
        }

        public Func<SyntaxNode, Func<SyntaxNode, SyntaxNode>, SyntaxNode> Function { get; }

        public override SyntaxNode Visit(SyntaxNode node) => Function(node, n => base.Visit(n));
    }
}
