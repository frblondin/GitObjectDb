using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Transformations: {Transformations.Count}")]
internal class TransformationComposer : ITransformationComposer
{
    private readonly IGitUpdateCommand _gitUpdateFactory;
    private readonly ICommitCommand _commitCommand;

    [FactoryDelegateConstructor(typeof(Factories.TransformationComposerFactory))]
    public TransformationComposer(IConnectionInternal connection,
                                  string branchName,
                                  CommitCommandType type,
                                  ServiceResolver<CommitCommandType, IGitUpdateCommand> gitUpdateFactory,
                                  ServiceResolver<CommitCommandType, ICommitCommand> commitCommandFactory)
    {
        Connection = connection;
        BranchName = branchName;
        Type = type;
        _gitUpdateFactory = gitUpdateFactory.Invoke(type);
        _commitCommand = commitCommandFactory.Invoke(type);
        Transformations = new List<ITransformation>();
    }

    public IConnectionInternal Connection { get; }

    public string BranchName { get; }

    public CommitCommandType Type { get; }

    public IList<ITransformation> Transformations { get; }

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

    public TItem Delete<TItem>(TItem item)
        where TItem : TreeItem
    {
        var path = item.ThrowIfNoPath();

        var transformation = new Transformation(
            path,
            default,
            _gitUpdateFactory.Delete(item.ThrowIfNoPath()),
            $"Removing {path}.");
        Transformations.Add(transformation);
        return item;
    }

    public void Delete(DataPath path)
    {
        var transformation = new Transformation(
            path,
            default,
            _gitUpdateFactory.Delete(path),
            $"Removing {path}.");
        Transformations.Add(transformation);
    }

    public void Rename(TreeItem item, DataPath newPath)
    {
        var transformation = new Transformation(
            newPath,
            item,
            _gitUpdateFactory.Rename(item, newPath),
            $"Renaming {item.Path} to {newPath}.");
        Transformations.Add(transformation);
    }

    private TItem CreateOrUpdateItem<TItem>(TItem item, DataPath? parent = null)
        where TItem : TreeItem
    {
        var type = item.GetType();
        if (type.IsNode())
        {
            // Make sure that node type is defined in model
            Connection.Model.GetDescription(type);
        }

        var path = item is Node node ?
            UpdateNodePathIfNeeded(node, parent) :
            item.ThrowIfNoPath();

        var transformation = new Transformation(
            path,
            item,
            _gitUpdateFactory.CreateOrUpdate(item),
            $"Adding or updating {path}.");
        Transformations.Add(transformation);
        return item;
    }

    private DataPath UpdateNodePathIfNeeded(Node node, DataPath? parent)
    {
        if (parent is not null)
        {
            ThrowIfWrongParentPath(parent);

            var newPath = parent.AddChild(node, Connection.Model, Connection.Serializer.FileExtension);
            ThrowIfWrongExistingPath(node, newPath);
            node.Path = newPath;
        }
        return node.Path ??= DataPath.Root(node, Connection.Model, Connection.Serializer.FileExtension);
    }

    Commit ITransformationComposer.Commit(CommitDescription description,
                                          Action<ITransformation>? beforeProcessing) =>
        _commitCommand.Commit(
            this,
            description,
            beforeProcessing: beforeProcessing);

    [ExcludeFromCodeCoverage]
    private static void ThrowIfWrongParentPath(DataPath parent)
    {
        if (!parent.IsNode || !parent.UseNodeFolders)
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
