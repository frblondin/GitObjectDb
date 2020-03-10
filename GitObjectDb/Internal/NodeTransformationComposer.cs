using GitObjectDb.Commands;
using GitObjectDb.Injection;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("Transformations: {Transformations.Count}")]
    internal class NodeTransformationComposer : INodeTransformationComposer
    {
        private readonly CommitCommand _commitCommand;

        [FactoryDelegateConstructor(typeof(Factories.NodeTransformationComposerFactory))]
        public NodeTransformationComposer(CommitCommand commitCommand, IConnectionInternal connection)
            : this(connection, new List<INodeTransformation>())
        {
            _commitCommand = commitCommand ?? throw new ArgumentNullException(nameof(commitCommand));
        }

        public NodeTransformationComposer(IConnectionInternal connection, List<INodeTransformation> transformations)
        {
            Connection = connection;
            Transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
        }

        public IConnectionInternal Connection { get; }

        public IList<INodeTransformation> Transformations { get; }

        public INodeTransformationComposer Create(Node node, Node parent) =>
            CreateOrUpdate(node, parent);

        public INodeTransformationComposer Update(Node node) =>
            CreateOrUpdate(node);

        public INodeTransformationComposer Delete(Node node)
        {
            var transformation = new NodeTransformation(
                UpdateTreeCommand.Delete(node),
                $"Removing {node.Path.FolderPath}.");
            Transformations.Add(transformation);
            return this;
        }

        private INodeTransformationComposer CreateOrUpdate(Node node, Node parent = null)
        {
            UpdateNodePathIfNeeded(node, parent);

            var transformation = new NodeTransformation(
                UpdateTreeCommand.CreateOrUpdate(node),
                $"Adding or updating {node.Path.FolderPath}.");
            Transformations.Add(transformation);
            return this;
        }

        private static void UpdateNodePathIfNeeded(Node node, Node parent)
        {
            if (parent != null)
            {
                if (node.Path != null)
                {
                    throw new GitObjectDbException("Node path has already been set. This generally means that a node has been created multiple times.");
                }
                node.Path = parent.Path.AddChild(node);
            }
            if (node.Path == null)
            {
                node.Path = Path.Root(node);
            }
        }

        public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false) =>
            _commitCommand.Commit(
                Connection.Repository,
                Transformations.Select(t => t.Transformation),
                message, author, committer, amendPreviousCommit);
    }
}
