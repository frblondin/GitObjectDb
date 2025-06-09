using GitDotNet;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace GitObjectDb.Internal;

[DebuggerDisplay("Transformations: {Transformations.Count}")]
[method: FactoryDelegate(typeof(Factories.ChangeComposerFactory))]
internal class ChangeComposer(IConnectionInternal connection,
                              string branchName,
                              IGitUpdateCommand gitUpdateFactory,
                              ICommitCommand commitCommandFactory) : IChangeComposerWithCommit
{
    public IConnectionInternal Connection { get; } = connection;

    public string BranchName { get; } = branchName;

    public IDictionary<DataPath, ITransformation> Transformations { get; } =
        new ConcurrentDictionary<DataPath, ITransformation>();

    public async Task<CommitEntry> CommitAsync(CommitDescription description,
        Action<ITransformation>? beforeProcessing = null) =>
        await commitCommandFactory.CommitAsync(
            this,
            description,
            beforeProcessing: beforeProcessing).ConfigureAwait(false);

    public Task<TNode> CreateOrUpdateAsync<TNode>(TNode node)
        where TNode : Node =>
        Task.FromResult(CreateOrUpdateItem(node, default));

    public Task<TNode> CreateOrUpdateAsync<TNode>(TNode node, DataPath? parent)
        where TNode : Node =>
        Task.FromResult(CreateOrUpdateItem(node, parent));

    public Task<TNode> CreateOrUpdateAsync<TNode>(TNode node, Node? parent)
        where TNode : Node =>
        Task.FromResult(CreateOrUpdateItem(node, parent?.Path));

    public Task<Resource> CreateOrUpdateAsync(Resource resource) =>
        Task.FromResult(CreateOrUpdateItem(resource, default));

    public Task RenameAsync(TreeItem item, DataPath newPath)
    {
        var transformation = new Transformation(
            newPath,
            item,
            gitUpdateFactory.Rename(item, newPath),
            $"Renaming {item.Path} to {newPath}.");
        Transformations[newPath] = transformation;
        return Task.CompletedTask;
    }

    public Task DeleteAsync<TItem>(TItem item)
        where TItem : TreeItem
    {
        RevertAsync(item.ThrowIfNoPath());
        return Task.CompletedTask;
    }

    public Task RevertAsync(DataPath path)
    {
        var transformation = new Transformation(
            path,
            default,
            gitUpdateFactory.Delete(path),
            $"Removing {path}.");
        Transformations[path] = transformation;
        return Task.CompletedTask;
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
            gitUpdateFactory.CreateOrUpdate(item),
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