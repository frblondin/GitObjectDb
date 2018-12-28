using GitObjectDb.Models.Merge;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Validations;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Container container one or more repositories and the dependencies.
    /// </summary>
    public interface IObjectRepositoryContainer
    {
        /// <summary>
        /// Gets the filesystem path containing all repositories.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the repositories being managed by the container.
        /// </summary>
        IEnumerable<IObjectRepository> Repositories { get; }

        /// <summary>
        /// Tries to get an existing repository from its identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Found <see cref="IObjectRepository"/>, if any.</returns>
        IObjectRepository TryGetRepository(UniqueId id);

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
        /// Validates the specified rules.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <returns>A <see cref="ValidationResult"/> object containing any validation failures.</returns>
        ValidationResult Validate(ValidationRules rules = ValidationRules.All);
    }

    /// <summary>
    /// Container container one or more repositories and the dependencies.
    /// </summary>
    /// <typeparam name="TRepository">The type of the object repository.</typeparam>
    public interface IObjectRepositoryContainer<TRepository> : IObjectRepositoryContainer
        where TRepository : IObjectRepository
    {
        /// <summary>
        /// Gets all repositories.
        /// </summary>
        new IImmutableSet<TRepository> Repositories { get; }

        /// <summary>
        /// Gets the <typeparamref name="TRepository"/> with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        TRepository this[UniqueId id] { get; }

        /// <summary>
        /// Stores the content of this instance and all its children in a new Git repository.
        /// </summary>
        /// <param name="repository">The repository to be added.</param>
        /// <param name="signature">The signature.</param>
        /// <param name="message">The message.</param>
        /// <param name="backend">The backend (optional).</param>
        /// <param name="isBare">if set to <c>true</c> a bare Git repository will be created.</param>
        /// <returns>The commit identifier of the new repository HEAD.</returns>
        TRepository AddRepository(TRepository repository, Signature signature, string message, Func<OdbBackend> backend = null, bool isBare = false);

        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <param name="repository">The (possibly remote) repository to clone from. See the <see href="https://git-scm.com/docs/git-clone#URLS">Git urls</see> section below for more information on specifying repositories.</param>
        /// <param name="commitId">The commit id to clone.</param>
        /// <param name="backend">The backend (optional).</param>
        /// <returns>The loaded instance.</returns>
        TRepository Clone(string repository, ObjectId commitId = null, Func<OdbBackend> backend = null);

        /// <summary>
        /// Commits all changes by comparing the current instance with a new one.
        /// </summary>
        /// <param name="repository">The new repository containing all the changes that must be committed.</param>
        /// <param name="signature">The signature.</param>
        /// <param name="message">The message.</param>
        /// <param name="options">The options.</param>
        /// <returns>The commit identifier of the new repository HEAD.</returns>
        TRepository Commit(IObjectRepository repository, Signature signature, string message, CommitOptions options = null);

        /// <summary>
        /// Update remote repository along with associated objects.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="remoteName">Name of the remote.</param>
        /// <param name="options">The options.</param>
        void Push(UniqueId id, string remoteName = null, PushOptions options = null);

        /// <summary>
        /// Checkouts the specified branch name.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <param name="createNewBranch">Create a new branch.</param>
        /// <param name="committish">The revparse spec for the target commit.</param>
        /// <returns>The newly created <typeparamref name="TRepository"/>.</returns>
        TRepository Checkout(UniqueId id, string branchName, bool createNewBranch = false, string committish = null);

        /// <summary>
        /// Download objects and refs from the remote repository.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="options">The options.</param>
        /// <returns>The remote HEAD commit <typeparamref name="TRepository"/>.</returns>
        TRepository Fetch(UniqueId id, FetchOptions options = null);

        /// <summary>
        /// Download objects and refs from all remote branches.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="options">The options.</param>
        void FetchAll(UniqueId id, FetchOptions options = null);

        /// <summary>
        /// Download objects and refs from the remote repository.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="options">The options.</param>
        /// <returns>The <see cref="IObjectRepositoryMerge"/> instance used to apply the merge.</returns>
        IObjectRepositoryMerge Pull(UniqueId id, FetchOptions options = null);

        /// <summary>
        /// Merges changes from branch into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="IObjectRepositoryMerge"/> instance used to apply the merge.</returns>
        IObjectRepositoryMerge Merge(UniqueId id, string branchName);

        /// <summary>
        /// Rebases changes from branch into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="IObjectRepositoryMerge"/> instance used to apply the merge.</returns>
        IObjectRepositoryRebase Rebase(UniqueId id, string branchName);
    }
}