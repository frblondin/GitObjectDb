using GitObjectDb.Commands;
using GitObjectDb.Injection;
using GitObjectDb.Serialization.Json;
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
        private readonly UpdateTreeCommand _updateTreeCommand;
        private readonly CommitCommand _commitCommand;

        [FactoryDelegateConstructor(typeof(Factories.NodeTransformationComposerFactory))]
        public NodeTransformationComposer(UpdateTreeCommand updateTreeCommand, CommitCommand commitCommand, IConnectionInternal connection)
            : this(connection, new List<INodeTransformation>())
        {
            _updateTreeCommand = updateTreeCommand ?? throw new ArgumentNullException(nameof(updateTreeCommand));
            _commitCommand = commitCommand ?? throw new ArgumentNullException(nameof(commitCommand));
        }

        public NodeTransformationComposer(IConnectionInternal connection, List<INodeTransformation> transformations)
        {
            Connection = connection;
            Transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
        }

        public IConnectionInternal Connection { get; }

        public IList<INodeTransformation> Transformations { get; }

        public INodeTransformationComposer CreateOrUpdate(Node item, Node parent = null) =>
            CreateOrUpdate((ITreeItem)item, parent);

        public INodeTransformationComposer CreateOrUpdate(Resource item) =>
            CreateOrUpdate(item, default);

        public INodeTransformationComposer Delete(ITreeItem item)
        {
            var transformation = new NodeTransformation(
                _updateTreeCommand.Delete(item),
                $"Removing {item.Path.FolderPath}.");
            Transformations.Add(transformation);
            return this;
        }

        private INodeTransformationComposer CreateOrUpdate(ITreeItem item, Node parent = null)
        {
            if (item is Node node)
            {
                UpdateNodePathIfNeeded(node, parent);
            }
            else if (item.Path == null)
            {
                throw new InvalidOperationException("Item has no path defined.");
            }

            var transformation = new NodeTransformation(
                _updateTreeCommand.CreateOrUpdate(item),
                $"Adding or updating {item.Path.FolderPath}.");
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
                node.Path = DataPath.Root(node);
            }
        }

        public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false) =>
            _commitCommand.Commit(
                Connection.Repository,
                Transformations.Select(t => t.Transformation),
                message, author, committer, amendPreviousCommit);
    }
}
