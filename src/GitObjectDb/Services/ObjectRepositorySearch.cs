using GitObjectDb.Git;
using GitObjectDb.JsonConverters;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Services
{
    /// <inheritdoc/>
    internal class ObjectRepositorySearch : IObjectRepositorySearch
    {
        /// <inheritdoc/>
        public IList<IModelObject> Grep(IObjectRepository repository, string content, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return repository.RepositoryProvider.Execute(repository.RepositoryDescription, r =>
            {
                if (!r.Head.Tip.Id.Equals(repository.CommitId))
                {
                    throw new NotSupportedException("The current head commit id is different from the commit used by current instance.");
                }

                return Grep(repository, r.Head.Tip.Tree, content, comparison).ToList();
            });
        }

        private IEnumerable<IModelObject> Grep(IObjectRepository repository, Tree tree, string content, StringComparison comparison) =>
            tree.SelectMany(child =>
            {
                switch (child.TargetType)
                {
                    case TreeEntryTargetType.Blob:
                        var blob = (Blob)child.Target;
                        if (ContainsString(blob, content, comparison))
                        {
                            return repository.GetFromGitPath(child.Path).ToEnumerable();
                        }
                        break;
                    case TreeEntryTargetType.Tree:
                        var subTree = (Tree)child.Target;
                        return Grep(repository, subTree, content, comparison);
                }
                return Enumerable.Empty<IModelObject>();
            });

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
        public IList<IModelObject> Grep(IObjectRepositoryContainer container, string content, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return container.Repositories.SelectMany(r => Grep(r, content, comparison)).ToList();
        }

        /// <inheritdoc/>
        public IList<IModelObject> GetReferrers<TModel>(TModel node)
            where TModel : class, IModelObject
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var target = $@"""path"": ""{node.GetFolderPath()}""";
            return Grep(node.Container, target, StringComparison.OrdinalIgnoreCase);
        }
    }
}
