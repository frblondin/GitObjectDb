using GitObjectDb.Git;
using GitObjectDb.Models.Migration;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Root node of a metadata tree.
    /// </summary>
    /// <seealso cref="IMetadataObject" />
    public interface IObjectRepository : IMetadataObject
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
        IMetadataObject TryGetFromGitPath(ObjectPath path);

        /// <summary>
        /// Gets a nested object from an <see cref="ObjectPath"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The object, if any was found.</returns>
        IMetadataObject GetFromGitPath(ObjectPath path);

        /// <summary>
        /// Tries getting a nested object from its Git path.
        /// </summary>
        /// <param name="path">The Git path.</param>
        /// <returns>The object, if any was found.</returns>
        IMetadataObject TryGetFromGitPath(string path);

        /// <summary>
        /// Gets a nested object from its Git path.
        /// </summary>
        /// <param name="path">The Git path.</param>
        /// <returns>The object, if any was found.</returns>
        IMetadataObject GetFromGitPath(string path);
    }
}
