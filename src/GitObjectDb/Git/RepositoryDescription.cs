using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GitObjectDb.Git
{
    /// <summary>
    /// Provides a description of a repository access.
    /// </summary>
    [DebuggerDisplay("Path = {Path}")]
    public sealed class RepositoryDescription : IEquatable<RepositoryDescription>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryDescription"/> class.
        /// </summary>
        /// <param name="path">The path to the repository.</param>
        /// <param name="backend">The backend.</param>
        /// <exception cref="ArgumentNullException">
        /// path
        /// or
        /// backend
        /// </exception>
        public RepositoryDescription(string path, OdbBackend backend = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Backend = backend;
        }

        /// <summary>
        /// Gets the path to the repository.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the backend (can be null).
        /// </summary>
        public OdbBackend Backend { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as RepositoryDescription);

        /// <inheritdoc/>
        public bool Equals(RepositoryDescription other) =>
            other != null &&
            string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase) &&
            Backend == other.Backend;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h1 = Path.GetHashCode();
            var h2 = Backend?.GetHashCode() ?? 0;
            return h2 != 0 ? ((h1 << 5) + h1) ^ h2 : h1;
        }
    }
}
