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
        private readonly IModelDataAccessorProvider _dataAccessorProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositorySearch"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ObjectRepositorySearch(IServiceProvider serviceProvider)
        {
            _dataAccessorProvider = serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
        }

        /// <inheritdoc/>
        public IEnumerable<IModelObject> Grep(IObjectRepository repository, string content)
        {
            return repository.RepositoryProvider.Execute(repository.RepositoryDescription, r =>
            {
                if (!r.Head.Tip.Id.Equals(repository.CommitId))
                {
                    throw new NotSupportedException("The current head commit id is different from the commit used by current instance.");
                }

                return Grep(repository, r.Head.Tip.Tree, content);
            });
        }

        private IEnumerable<IModelObject> Grep(IObjectRepository repository, Tree tree, string content) =>
            tree.SelectMany(child =>
            {
                switch (child.TargetType)
                {
                    case TreeEntryTargetType.Blob:
                        var blob = (Blob)child.Target;
                        if (ContainsString(blob, content))
                        {
                            return repository.GetFromGitPath(child.Path).ToEnumerable();
                        }
                        break;
                    case TreeEntryTargetType.Tree:
                        var subTree = (Tree)child.Target;
                        return Grep(repository, subTree, content);
                }
                return Enumerable.Empty<IModelObject>();
            });

        private static bool ContainsString(Blob blob, string content)
        {
            using (var reader = new StreamReader(blob.GetContentStream()))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        return false;
                    }
                    if (line.IndexOf(content, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return true;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IModelObject> Grep(IObjectRepositoryContainer container, string content) =>
            container.Repositories.SelectMany(r => Grep(r, content));

        /// <inheritdoc/>
        public IEnumerable<IModelObject> GetReferrers<TModel>(TModel node)
            where TModel : class, IModelObject
        {
            var target = $@"""path"": ""{node.GetFolderPath()}""";
            return Grep(node.Container, target);
        }
    }
}
