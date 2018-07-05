using GitObjectDb.Git;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Root node of a metadata tree.
    /// </summary>
    /// <seealso cref="IMetadataObject" />
    public interface IInstance : IMetadataObject
    {
        /// <summary>
        /// Stores the content of this instance and all its children in a new Git repository.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <param name="message">The message.</param>
        /// <param name="path">The path.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="isBare">if set to <c>true</c> a bare Git repository will be created.</param>
        /// <returns>The <see cref="Commit"/> of the new repository HEAD.</returns>
        Commit SaveInNewRepository(Signature signature, string message, string path, RepositoryDescription repositoryDescription, bool isBare = false);

        /// <summary>
        /// Commits all changes by comparing the current instance with a new one.
        /// </summary>
        /// <param name="newInstance">The new instance containing all the changes that must be committed.</param>
        /// <param name="signature">The signature.</param>
        /// <param name="message">The message.</param>
        /// <param name="options">The options.</param>
        /// <returns>The <see cref="Commit"/> of the new repository HEAD.</returns>
        Commit Commit(AbstractInstance newInstance, Signature signature, string message, CommitOptions options = null);

        /// <summary>
        /// Tries getting a nested object from its Git path.
        /// </summary>
        /// <param name="path">The Git path.</param>
        /// <returns>The object, if any was found.</returns>
        IMetadataObject TryGetFromGitPath(string path);
    }
}
