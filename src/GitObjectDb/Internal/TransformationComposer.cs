using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("Transformations: {Transformations.Count}")]
    internal class TransformationComposer : ITransformationComposer
    {
        private readonly UpdateTreeCommand _updateTreeCommand;
        private readonly CommitCommand _commitCommand;

        [FactoryDelegateConstructor(typeof(Factories.TransformationComposerFactory))]
        public TransformationComposer(UpdateTreeCommand updateTreeCommand, CommitCommand commitCommand, IConnectionInternal connection)
        {
            _updateTreeCommand = updateTreeCommand;
            _commitCommand = commitCommand;
            Connection = connection;
            Transformations = new List<ITransformation>();
        }

        public IConnectionInternal Connection { get; }

        public IList<ITransformation> Transformations { get; }

        public TNode CreateOrUpdate<TNode>(TNode node, Node? parent = null)
            where TNode : Node =>
            CreateOrUpdateItem(node, parent);

        public Resource CreateOrUpdate(Resource resource) =>
            CreateOrUpdateItem(resource, default);

        public TItem Delete<TItem>(TItem item)
            where TItem : ITreeItem
        {
            if (item.Path is null)
            {
                throw new InvalidOperationException("Item has no path defined.");
            }
            var transformation = new Transformation(
                UpdateTreeCommand.Delete(item),
                $"Removing {item.Path.FolderPath}.");
            Transformations.Add(transformation);
            return item;
        }

        public void Delete(DataPath path)
        {
            var transformation = new Transformation(
                UpdateTreeCommand.Delete(path),
                $"Removing {path}.");
            Transformations.Add(transformation);
        }

        private TItem CreateOrUpdateItem<TItem>(TItem item, Node? parent = null)
            where TItem : ITreeItem
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

            var transformation = new Transformation(
                _updateTreeCommand.CreateOrUpdate(item),
                $"Adding or updating {path.FolderPath}.");
            Transformations.Add(transformation);
            return item;
        }

        private static DataPath UpdateNodePathIfNeeded(Node node, Node? parent)
        {
            if (parent is not null)
            {
                if (parent.Path is null)
                {
                    throw new GitObjectDbException("Parent path has not been set.");
                }
                var newPath = parent.Path.AddChild(node);
                if (node.Path is not null && !node.Path.Equals(newPath))
                {
                    throw new GitObjectDbException("Node path has already been set. This generally means that a node has been created multiple times.");
                }
                node.Path = newPath;
            }
            if (node.Path is null)
            {
                node.Path = DataPath.Root(node);
            }
            return node.Path;
        }

        public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false, Action<ITransformation>? beforeProcessing = null) =>
            _commitCommand.Commit(
                Connection.Repository,
                Transformations,
                message, author, committer, amendPreviousCommit,
                beforeProcessing: beforeProcessing);
    }
}
