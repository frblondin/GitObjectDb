using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb
{
    /// <summary>Represents a node path.</summary>
    public sealed class Path : IEquatable<Path>
    {
        private readonly Lazy<string> _dataPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Path"/> class.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        internal Path(string folderPath)
        {
            var cleanedFolder = CleanupFolder(folderPath ?? throw new ArgumentNullException(nameof(folderPath)));
            FolderPath = cleanedFolder;
            _dataPath = new Lazy<string>(() =>
                string.IsNullOrEmpty(cleanedFolder) ? FileSystemStorage.DataFile : $"{cleanedFolder}/{FileSystemStorage.DataFile}");
        }

        /// <summary>Gets the folder path.</summary>
        public string FolderPath { get; }

        /// <summary>Gets the blob data path, holding the serialized representation of a node in the repository.</summary>
        public string DataPath => _dataPath.Value;

        /// <summary>Performs an implicit conversion from <see cref="Path"/> to <see cref="string"/>.</summary>
        /// <param name="path">The path.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(Path path) =>
            path.FolderPath;

        /// <summary>Performs an implicit conversion from <see cref="string"/> to <see cref="Path"/>.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Path(string value) =>
            new Path(value);

        /// <summary>Gets the root path of the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns>The root path.</returns>
        public static Path Root(Node node) => new Path(GetSuffix(node));

        internal static Stack<string> ToStack(Path path) =>
            string.IsNullOrEmpty(path?.FolderPath) ?
            new Stack<string>() :
            new Stack<string>(path.FolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries));

        internal static Path FromStack(Stack<string> stack) =>
            new Path(string.Join('/', stack.Reverse()));

        private static string CleanupFolder(string folder)
        {
            if (folder.EndsWith(FileSystemStorage.DataFile))
            {
                folder = folder.Substring(0, folder.Length - FileSystemStorage.DataFile.Length);
            }
            return folder.Trim('/');
        }

        /// <summary>Returns the path of a child being added to the current path.</summary>
        /// <param name="node">The node.</param>
        /// <returns>The path of a node being added to the path.</returns>
        public Path AddChild(Node node)
        {
            var path = string.IsNullOrEmpty(FolderPath) ?
                GetSuffix(node) :
                $"{FolderPath}/{GetSuffix(node)}";
            return new Path(path);
        }

        private static string GetSuffix(Node node)
        {
            var type = node.GetType();
            var attribute = GitPathAttribute.Get(type);
            var folder = attribute?.FolderName ?? type.Name;
            return string.IsNullOrEmpty(folder) ? node.Id.ToString() : $"{folder}/{node.Id}";
        }

        /// <inheritdoc/>
        public override string ToString() => FolderPath;

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            Equals(obj as Path);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            FolderPath.GetHashCode(StringComparison.Ordinal);

        /// <inheritdoc/>
        public bool Equals(Path other) =>
            string.Equals(FolderPath, other?.FolderPath, StringComparison.Ordinal);
    }
}
