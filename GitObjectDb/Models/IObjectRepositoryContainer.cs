using FluentValidation.Results;
using GitObjectDb.Compare;
using GitObjectDb.Git;
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
        /// Tries to get an existing repository from its identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Found <see cref="IObjectRepository"/>, if any.</returns>
        IObjectRepository TryGetRepository(Guid id);

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
        IImmutableSet<TRepository> Repositories { get; }

        /// <summary>
        /// Gets the <typeparamref name="TRepository"/> with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        TRepository this[Guid id] { get; }

        /// <summary>
        /// Stores the content of this instance and all its children in a new Git repository.
        /// </summary>
        /// <param name="repository">The repository to be added.</param>
        /// <param name="signature">The signature.</param>
        /// <param name="message">The message.</param>
        /// <param name="backend">The backend (optional).</param>
        /// <param name="isBare">if set to <c>true</c> a bare Git repository will be created.</param>
        /// <returns>The commit identifier of the new repository HEAD.</returns>
        TRepository AddRepository(TRepository repository, Signature signature, string message, OdbBackend backend = null, bool isBare = false);

        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <param name="repository">The (possibly remote) repository to clone from. See the <see href="https://git-scm.com/docs/git-clone#URLS">Git urls</see> section below for more information on specifying repositories.</param>
        /// <param name="commitId">The commit id to clone.</param>
        /// <param name="backend">The backend (optional).</param>
        /// <returns>The loaded instance.</returns>
        TRepository Clone(string repository, ObjectId commitId = null, OdbBackend backend = null);

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
        /// Checkouts the specified branch name.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="LibGit2Sharp.Branch"/>.</returns>
        TRepository Checkout(TRepository repository, string branchName);

        /// <summary>
        /// Checkouts the specified branch name.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="LibGit2Sharp.Branch"/>.</returns>
        TRepository Checkout(Guid id, string branchName);

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the current commit.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <returns>The newly created <see cref="LibGit2Sharp.Branch"/>.</returns>
        TRepository Branch(TRepository repository, string branchName);

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the current commit.
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <returns>The newly created <see cref="LibGit2Sharp.Branch"/>.</returns>
        TRepository Branch(Guid id, string branchName);

        /// <summary>
        /// Merges changes from branch into the branch pointed at by HEAD..
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="IMetadataTreeMerge"/> instance used to apply the merge.</returns>
        IMetadataTreeMerge Merge(TRepository repository, string branchName);

        /// <summary>
        /// Merges changes from branch into the branch pointed at by HEAD..
        /// </summary>
        /// <param name="id">The repository id.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <returns>The <see cref="IMetadataTreeMerge"/> instance used to apply the merge.</returns>
        IMetadataTreeMerge Merge(Guid id, string branchName);
    }
}