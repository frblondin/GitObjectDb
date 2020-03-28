using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Internal.Queries
{
    internal class LoadItem : IQuery<Tree, DataPath, ITreeItem>
    {
        private readonly INodeSerializer _serializer;

        public LoadItem(INodeSerializer serializer)
        {
            _serializer = serializer;
        }

        public ITreeItem Execute(Repository repository, Tree tree, DataPath path) =>
            path.FileName == FileSystemStorage.DataFile ?
                LoadNode(tree, path) :
                LoadResource(tree, path);

        private ITreeItem LoadNode(Tree tree, DataPath path)
        {
            var blob = tree[path.FileName].Target.Peel<Blob>();
            return _serializer.Deserialize(blob.GetContentStream(), path).Node;
        }

        private static ITreeItem LoadResource(Tree tree, DataPath path)
        {
            var blob = tree[path.FileName].Target.Peel<Blob>();
            return new Resource(path, blob);
        }
    }
}
