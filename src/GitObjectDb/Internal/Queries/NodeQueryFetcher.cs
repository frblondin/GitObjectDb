using GitObjectDb.Injection;
using GitObjectDb.Serialization.Json;
using GitObjectDb.Serialization.Json.Converters;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Internal.Queries
{
    internal class NodeQueryFetcher
    {
        private static readonly MethodInfo _isOfTypeMethod = ExpressionReflector.GetMethod(() =>
            IsOfType(default, string.Empty));

        private readonly IQuery<Tree, DataPath, ITreeItem> _loader;
        private readonly INodeSerializer _serializer;
        private readonly Repository _repository;
        private readonly Tree _tree;
        private readonly Node _parent;
        private readonly bool _isRecursive;
        private readonly IList<Expression> _predicateExpressions = new List<Expression>();
        private readonly Lazy<Predicate<(DataPath, Func<Stream>)>?> _predicate;

        [FactoryDelegateConstructor(typeof(Factories.NodeQueryFetcherFactory))]
        internal NodeQueryFetcher(
            IQuery<Tree, DataPath, ITreeItem> loader, INodeSerializer serializer,
            Repository repository, Tree tree, Node parent, bool isRecursive)
        {
            _loader = loader;
            _serializer = serializer;
            _repository = repository;
            _tree = tree;
            _parent = parent;
            _isRecursive = isRecursive;
            _predicate = new Lazy<Predicate<(DataPath, Func<Stream>)>?>(CreatePredicate);
        }

        internal bool IncompatiblePreedicate { get; private set; }

        internal void AddPredicate(Expression predicate) =>
            _predicateExpressions.Add(predicate);

        internal void MarkAsIncompatiblePredicate() =>
            IncompatiblePreedicate = true;

        private Predicate<(DataPath, Func<Stream>)>? CreatePredicate()
        {
            if (IncompatiblePreedicate || !_predicateExpressions.Any())
            {
                return null;
            }
            else
            {
                Expression? body = default;
                foreach (var part in _predicateExpressions)
                {
                    if (body == null)
                    {
                        body = part;
                    }
                    else
                    {
                        body = Expression.AndAlso(body, part);
                    }
                }
                var lambda = Expression.Lambda<Predicate<(DataPath, Func<Stream>)>>(
                    body, NodeQueryPredicateVisitor.NewParameter);
                return lambda.Compile();
            }
        }

        internal static Expression CreateIsOfTypePredicate(Type type) =>
            Expression.Call(
                _isOfTypeMethod,
                NodeQueryPredicateVisitor.NewParameter,
                Expression.Constant(NonScalarConverter.BindToName(type)));

        private static bool IsOfType((DataPath Path, Func<Stream> Stream) data, string type)
        {
            using var reader = new StreamReader(data.Stream(), Encoding.Default,
                detectEncodingFromByteOrderMarks: true, bufferSize: 128, leaveOpen: true);
            string line;
            int i = 0;
            while ((line = reader.ReadLine()) != null && ++i < 3)
            {
                if (line.Contains(type, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        internal IEnumerable<Node> Fetch()
        {
            var entries = new Stack<(Tree Tree, DataPath Path)>();
            FetchDirectChildren(_tree, _parent?.Path, entries);

            while (entries.Count > 0)
            {
                var current = entries.Pop();
                if (Include(current.Tree, current.Path, out var node) && node != null)
                {
                    yield return node;
                }

                if (_isRecursive)
                {
                    FetchDirectChildren(current.Tree, current.Path, entries);
                }
            }
        }

        private bool Include(Tree tree, DataPath path, out Node? node)
        {
            var stream = new Lazy<Stream>(() => tree[path.FileName].Target.Peel<Blob>().GetContentStream());
            if (_predicate.Value == null || _predicate.Value((path, GetStream)))
            {
                node = _serializer.Deserialize(GetStream(), path).Node;
                return true;
            }
            else
            {
                node = null;
                return false;
            }

            Stream GetStream()
            {
                // Whenever we read the stream we want to start from first position
                stream.Value.Position = 0L;

                return stream.Value;
            }
        }

        private static void FetchDirectChildren(Tree tree, DataPath? path, Stack<(Tree, DataPath)> entries)
        {
            foreach (var folderChildTree in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree))
            {
                foreach (var childFolder in folderChildTree.Target.Peel<Tree>().Where(e => e.TargetType == TreeEntryTargetType.Tree))
                {
                    if (UniqueId.TryParse(childFolder.Name, out var id))
                    {
                        var nestedTree = childFolder.Target.Peel<Tree>();
                        if (nestedTree.Any(e => e.Name == FileSystemStorage.DataFile))
                        {
                            var childPath =
                                path?.AddChild(folderChildTree.Name, id) ??
                                DataPath.Root(folderChildTree.Name, id);
                            entries.Push((nestedTree, childPath));
                        }
                    }
                }
            }
        }
    }
}
