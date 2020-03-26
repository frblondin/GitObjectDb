using GitObjectDb.Commands;
using GitObjectDb.Injection;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("Transformations: {Transformations.Count}")]
    internal class NodeTransformationComposer : INodeTransformationComposer
    {
        private readonly UpdateTreeCommand _updateTreeCommand;
        private readonly CommitCommand _commitCommand;

        [FactoryDelegateConstructor(typeof(Factories.NodeTransformationComposerFactory))]
        public NodeTransformationComposer(UpdateTreeCommand updateTreeCommand, CommitCommand commitCommand, IConnectionInternal connection)
        {
            _updateTreeCommand = updateTreeCommand;
            _commitCommand = commitCommand;
            Connection = connection;
            Transformations = new List<INodeTransformation>();
        }

        public IConnectionInternal Connection { get; }

        public IList<INodeTransformation> Transformations { get; }

        public INodeTransformationComposer CreateOrUpdate(Node node, Node? parent = null) =>
            CreateOrUpdate((ITreeItem)node, parent);

        public INodeTransformationComposer CreateOrUpdate(Resource resource) =>
            CreateOrUpdate(resource, default);

        public INodeTransformationComposer Delete(ITreeItem item)
        {
            if (item.Path is null)
            {
                throw new InvalidOperationException("Item has no path defined.");
            }
            var transformation = new NodeTransformation(
                _updateTreeCommand.Delete(item),
                $"Removing {item.Path.FolderPath}.");
            Transformations.Add(transformation);
            return this;
        }

        private INodeTransformationComposer CreateOrUpdate(ITreeItem item, Node? parent = null)
        {
            var path = item.Path;
            if (item is Node node)
            {
                path = UpdateNodePathIfNeeded(node, parent);
            }
            else if (path is null)
            {
                throw new InvalidOperationException("Item has no path defined.");
            }

            var transformation = new NodeTransformation(
                _updateTreeCommand.CreateOrUpdate(item),
                $"Adding or updating {path.FolderPath}.");
            Transformations.Add(transformation);
            return this;
        }

        private static DataPath UpdateNodePathIfNeeded(Node node, Node? parent)
        {
            if (!(parent is null))
            {
                if (parent.Path is null)
                {
                    throw new GitObjectDbException("Parent path has not been set.");
                }
                if (!(node.Path is null))
                {
                    throw new GitObjectDbException("Node path has already been set. This generally means that a node has been created multiple times.");
                }
                node.Path = parent.Path.AddChild(node);
            }
            if (node.Path is null)
            {
                node.Path = DataPath.Root(node);
            }
            return node.Path;
        }

        public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false) =>
            _commitCommand.Commit(
                Connection.Repository,
                Transformations.Select(t => t.Transformation),
                message, author, committer, amendPreviousCommit);
    }
}
