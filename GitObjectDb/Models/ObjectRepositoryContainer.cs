using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.IO;
using GitObjectDb.Validations;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    public abstract class ObjectRepositoryContainer : IObjectRepositoryContainer
    {
        /// <inheritdoc />
        public abstract string Path { get; }

        /// <inheritdoc />
        public abstract IMetadataObject GetFromGitPath(ObjectPath path);

        /// <inheritdoc />
        public abstract IMetadataObject TryGetFromGitPath(ObjectPath path);

        /// <summary>
        /// Reloads the repository and refreshes the reference in the container.
        /// </summary>
        /// <param name="previousRepository">The previous repository.</param>
        /// <param name="commit">The commit.</param>
        /// <returns>The loaded repository.</returns>
        internal abstract IObjectRepository ReloadRepository(IObjectRepository previousRepository, ObjectId commit);
    }
}
