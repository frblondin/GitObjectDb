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
        /// Tries getting a nested object from an <see cref="ObjectPath"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The object, if any was found.</returns>
        IModelObject TryGetFromGitPath(ObjectPath path);

        /// <summary>
        /// Gets a nested object from an <see cref="ObjectPath"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The object, if any was found.</returns>
        IModelObject GetFromGitPath(ObjectPath path);

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
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="processor">The function.</param>
        /// <returns>The result of the function call.</returns>
        TResult Execute<TResult>(Func<IRepository, TResult> processor);

        /// <summary>
        /// Calls the provided function processing.
        /// </summary>
        /// <param name="processor">The function.</param>
        void Execute(Action<IRepository> processor);
    }
}
