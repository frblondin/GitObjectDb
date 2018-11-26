using GitObjectDb.Git;
using GitObjectDb.Models.Migration;
using GitObjectDb.Models.Rebase;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Root node of a model tree.
    /// </summary>
    /// <seealso cref="IModelObject" />
    public interface IObjectRepository : IModelObject
    {
        /// <summary>
        /// Gets the version.
        /// </summary>
        System.Version Version { get; }

        /// <summary>
        /// Gets the dependencies.
        /// </summary>
        IImmutableList<RepositoryDependency> Dependencies { get; }

        /// <summary>
        /// Gets the migrations.
        /// </summary>
        ILazyChildren<IMigration> Migrations { get; }

        /// <summary>
        /// Gets the <see cref="LibGit2Sharp.Commit"/> id this instance has been load from.
        /// </summary>
        ObjectId CommitId { get; }

        /// <summary>
        /// Gets the repository provider.
        /// </summary>
        IRepositoryProvider RepositoryProvider { get; }

        /// <summary>
        /// Gets the repository description.
        /// </summary>
        RepositoryDescription RepositoryDescription { get; }

        /// <summary>
        /// Sets the repository data.
        /// </summary>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit the repository is referring to.</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void SetRepositoryData(RepositoryDescription repositoryDescription, ObjectId commitId);

        /// <summary>
        /// Tries getting a nested object from its Git path.
        /// </summary>
        /// <param name="path">The Git path.</param>
        /// <returns>The object, if any was found.</returns>
        IModelObject TryGetFromGitPath(string path);

        /// <summary>
        /// Gets a nested object from its Git path.
        /// </summary>
        /// <param name="path">The Git path.</param>
        /// <returns>The object, if any was found.</returns>
        IModelObject GetFromGitPath(string path);

        /// <summary>
        /// Gets the objects having a reference to this instance.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="node">The object.</param>
        /// <returns>All objects having a link whose target is the <paramref name="node"/>.</returns>
        IEnumerable<IModelObject> GetReferrers<TModel>(TModel node)
            where TModel : class, IModelObject;
    }
}
