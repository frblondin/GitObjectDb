using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
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
    private readonly UpdateFastInsertFile _updateFastInsertFile;
    private readonly ServiceResolver<CommitCommandType, ICommitCommand> _commitCommandFactory;

    [FactoryDelegateConstructor(typeof(Factories.TransformationComposerFactory))]
    public TransformationComposer(UpdateFastInsertFile updateFastInsertFile,
                                  ServiceResolver<CommitCommandType, ICommitCommand> commitCommandFactory,
                                  IConnectionInternal connection)
    {
        _updateFastInsertFile = updateFastInsertFile;
        _commitCommandFactory = commitCommandFactory;
        Connection = connection;
        Transformations = new List<ITransformation>();
    }

    public IConnectionInternal Connection { get; }

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
        where TItem : ITreeItem
    {
        var path = item.ThrowIfNoPath();

        var transformation = new Transformation(
            path,
            default,
            UpdateTreeCommand.Delete(item),
            UpdateFastInsertFile.Delete(item),
            $"Removing {path}.");
        Transformations.Add(transformation);
        return item;
    }

    public void Delete(DataPath path)
    {
        var transformation = new Transformation(
            path,
            default,
            UpdateTreeCommand.Delete(path),
            UpdateFastInsertFile.Delete(path),
            $"Removing {path}.");
        Transformations.Add(transformation);
    }

    private TItem CreateOrUpdateItem<TItem>(TItem item, DataPath? parent = null)
        where TItem : ITreeItem
    {
        var path = item is Node node ?
            UpdateNodePathIfNeeded(node, parent) :
            item.ThrowIfNoPath();

        var transformation = new Transformation(
            path,
            item,
            UpdateTreeCommand.CreateOrUpdate(item),
            _updateFastInsertFile.CreateOrUpdate(item),
            $"Adding or updating {path}.");
        Transformations.Add(transformation);
        return item;
    }

    private DataPath UpdateNodePathIfNeeded(Node node, DataPath? parent)
    {
        if (parent is not null)
        {
            ThrowIfWrongParentPath(parent);

            var newPath = parent.AddChild(node, Connection.Model);
            ThrowIfWrongExistingPath(node, newPath);
            node.Path = newPath;
        }
        return node.Path ??= DataPath.Root(node, Connection.Model);
    }

    Commit ITransformationComposer.Commit(CommitDescription description,
                                          Action<ITransformation>? beforeProcessing,
                                          CommitCommandType type) =>
        _commitCommandFactory.Invoke(type).Commit(
            Connection,
            this,
            description,
            beforeProcessing: beforeProcessing);

    internal TreeDefinition ApplyTransformations(ObjectDatabase dataBase,
                                                 Commit? commit,
                                                 Action<ITransformation>? beforeProcessing = null)
    {
        var result = commit is not null ? TreeDefinition.From(commit) : new TreeDefinition();
        var modules = new ModuleCommands(commit?.Tree);
        foreach (var transformation in Transformations.OfType<ITransformationInternal>())
        {
            beforeProcessing?.Invoke(transformation);
            transformation.TreeTransformation(commit?.Tree, modules, Connection.Serializer, dataBase, result);
        }

        if (modules.HasAnyChange)
        {
            using var stream = modules.CreateStream();
            var blob = dataBase.CreateBlob(stream);
            result.Add(ModuleCommands.ModuleFile, blob, Mode.NonExecutableFile);
        }

        return result;
    }

    internal void ApplyTransformations(Commit? commit,
                                       System.IO.StreamWriter writer,
                                       IList<string> commitIndex,
                                       Action<ITransformation>? beforeProcessing = null)
    {
        var modules = new ModuleCommands(commit?.Tree);
        foreach (var transformation in Transformations.OfType<ITransformationInternal>())
        {
            beforeProcessing?.Invoke(transformation);
            transformation.FastInsertTransformation(commit?.Tree, modules, Connection.Serializer, writer, commitIndex);
        }

        if (modules.HasAnyChange)
        {
            using var stream = modules.CreateStream();
            UpdateFastInsertFile.AddBlob(ModuleCommands.ModuleFile, stream, writer, commitIndex);
        }
    }

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
