using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Threading;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Services
{
    /// <inheritdoc/>
    internal class ObjectRepositorySearch : IObjectRepositorySearch
    {
        /// <inheritdoc/>
        public IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepository repository, string content, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return GrepAsync(repository, content, CancellationToken.None, comparison);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepository repository, string content, CancellationToken cancellationToken, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return repository.RepositoryProvider.ExecuteAsyncEnumerator(repository.RepositoryDescription, r =>
            {
                if (!r.Head.Tip.Id.Equals(repository.CommitId))
                {
                    throw new NotSupportedException("The current head commit id is different from the commit used by current instance.");
                }

                return GrepAsync(repository, r.Head.Tip.Tree, content, comparison, cancellationToken);
            });
        }

        private async IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepository repository, Tree tree, string content, StringComparison comparison, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var child in tree)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                switch (child.TargetType)
                {
                    case TreeEntryTargetType.Blob:
                        var blob = (Blob)child.Target;
                        if (ContainsString(blob, content, comparison))
                        {
                            yield return repository.GetFromGitPathAsync(child.Path);
                        }
                        break;
                    case TreeEntryTargetType.Tree:
                        var subTree = (Tree)child.Target;
                        await foreach (var nested in GrepAsync(repository, subTree, content, comparison, cancellationToken).WithCancellation(cancellationToken))
                        {
                            yield return nested;
                        }
                        break;
                }
            }
        }

        private static bool ContainsString(Blob blob, string content, StringComparison comparison)
        {
            using (var reader = new StreamReader(blob.GetContentStream()))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.IndexOf(content, comparison) != -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepositoryContainer container, string content, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return GrepAsync(container, content, CancellationToken.None, comparison);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepositoryContainer container, string content, [EnumeratorCancellation] CancellationToken cancellationToken, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            foreach (var r in container.Repositories)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                await foreach (var child in GrepAsync(r, content, cancellationToken, comparison).WithCancellation(cancellationToken))
                {
                    yield return child;
                }
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<IModelObject> GetReferrers<TModel>(TModel node)
            where TModel : class, IModelObject
        {
            return GetReferrersAsync(node, CancellationToken.None);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<IModelObject> GetReferrersAsync<TModel>(TModel node, CancellationToken cancellationToken)
            where TModel : class, IModelObject
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var target = $@"""path"": ""{node.GetFolderPath()}""";
            return GrepAsync(node.Container, target, StringComparison.OrdinalIgnoreCase);
        }
    }
}
