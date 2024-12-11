using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Transformations: {Transformations.Count}")]
internal class TransformationComposer : ITransformationComposerWithCommit
{
    private readonly IGitUpdateCommand _gitUpdateFactory;
    private readonly ICommitCommand _commitCommand;

    [FactoryDelegateConstructor(typeof(Factories.TransformationComposerFactory))]
    public TransformationComposer(IConnectionInternal connection,
                                  string branchName,
                                  IGitUpdateCommand gitUpdateFactory,
                                  ICommitCommand commitCommandFactory)
    {
        Connection = connection;
        BranchName = branchName;
        _gitUpdateFactory = gitUpdateFactory;
        _commitCommand = commitCommandFactory;
    }

    public IConnectionInternal Connection { get; }

    public string BranchName { get; }

    public IDictionary<DataPath, ITransformation> Transformations { get; } =
        new ConcurrentDictionary<DataPath, ITransformation>();

    public Commit Commit(CommitDescription description,
                         Action<ITransformation>? beforeProcessing = null) =>
        _commitCommand.Commit(
            this,
            description,
            beforeProcessing: beforeProcessing);

    public TNode CreateOrUpdate<TNode>(TNode node)
        where TNode : Node =>
        CreateOrUpdateItem(node, default);

    public TNode CreateOrUpdate<TNode>(TNode node, DataPath? parent)
        where TNode : Node =>
        CreateOrUpdateItem(node, parent);

    public TNode CreateOrUpdate<TNode>(TNode node, Node? parent)
        where TNode : Node =>
        CreateOrUpdateItem(node, parent?.Path);

    public Resource CreateOrUpdate(Resource resource) =>
        CreateOrUpdateItem(resource, default);

    public void Rename(TreeItem item, DataPath newPath)
    {
        var transformation = new Transformation(
            newPath,
            item,
            _gitUpdateFactory.Rename(item, newPath),
            $"Renaming {item.Path} to {newPath}.");
        Transformations[newPath] = transformation;
    }

    public void Delete<TItem>(TItem item)
        where TItem : TreeItem
    {
        Revert(item.ThrowIfNoPath());
    }

    public void Revert(DataPath path)
    {
        var transformation = new Transformation(
            path,
            default,
            _gitUpdateFactory.Delete(path),
            $"Removing {path}.");
        Transformations[path] = transformation;
    }

    protected TItem CreateOrUpdateItem<TItem>(TItem item, DataPath? parent = null)
        where TItem : TreeItem
    {
        var type = item.GetType();
        if (type.IsNode())
        {
            // Make sure that node type is defined in model
            Connection.Model.GetDescription(type);
        }

        var path = item is Node node ?
            UpdateNodePathIfNeeded(node, parent, Connection) :
            item.ThrowIfNoPath();

        var transformation = new Transformation(
            path,
            item,
            _gitUpdateFactory.CreateOrUpdate(item),
            $"Adding or updating {path}.");
        Transformations[path] = transformation;

        return item;
    }

    internal static DataPath UpdateNodePathIfNeeded(Node node, DataPath? parent, IConnection connection)
    {
        if (parent is not null)
        {
            ThrowIfWrongParentPath(parent, connection);

            var newPath = parent.AddChild(node, connection.Model, connection.Serializer.FileExtension);
            ThrowIfWrongExistingPath(node, newPath);
            node.Path = newPath;
        }
        return node.Path ??= DataPath.Root(node, connection.Model, connection.Serializer.FileExtension);
    }

    [ExcludeFromCodeCoverage]
    private static void ThrowIfWrongParentPath(DataPath parent, IConnection connection)
    {
        if (!parent.IsNode(connection.Serializer) || !parent.UseNodeFolders)
        {
            throw new GitObjectDbException("Parent path has not been set.");
        }
    }

    [ExcludeFromCodeCoverage]
    private static void ThrowIfWrongExistingPath(Node node, DataPath newPath)
    {
        if (node.Path is not null && !node.Path.Equals(newPath))
        {
            throw new GitObjectDbException("Node path has already been set. This generally means that a " +
                "node has been created multiple times. Make sure to reset cloned path value.");
        }
    }
}