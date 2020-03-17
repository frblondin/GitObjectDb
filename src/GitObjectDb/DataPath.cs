using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb
{
    /// <summary>Represents a data path.</summary>
    public sealed class DataPath : IEquatable<DataPath>
    {
        private readonly Lazy<string> _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPath"/> class.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="fileName">The file name containing data.</param>
        internal DataPath(string folderPath, string fileName)
        {
            var cleanedFolder = CleanupFolder(
                folderPath ?? throw new ArgumentNullException(nameof(folderPath)),
                fileName);
            FolderPath = cleanedFolder;
            FileName = fileName;

            _filePath = new Lazy<string>(() =>
                string.IsNullOrEmpty(cleanedFolder) ? FileName : $"{FolderPath}/{FileName}");
        }

        /// <summary>Gets the folder path.</summary>
        public string FolderPath { get; }

        /// <summary>Gets the blob data path, holding the serialized representation of a node in the repository.</summary>
        public string FilePath => _filePath.Value;

        /// <summary>Gets the name of the file containing data.</summary>
        public string FileName { get; }

        /// <summary>Gets the root path of the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns>The root path.</returns>
        public static DataPath Root(Node node) => new DataPath(GetSuffix(node), FileSystemStorage.DataFile);

        internal static Stack<string> ToStack(DataPath path) =>
            string.IsNullOrEmpty(path?.FolderPath) ?
            new Stack<string>() :
            new Stack<string>(path.FolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries));

        internal static DataPath FromStack(Stack<string> stack, string dataFile) =>
            new DataPath(string.Join('/', stack.Reverse()), dataFile);

        internal static DataPath FromGitBlobPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("message", nameof(path));
            }

            var separator = path.LastIndexOf('/');
            return separator != -1 ?
                new DataPath(path.Substring(0, separator), path.Substring(separator + 1)) :
                new DataPath(string.Empty, path);
        }

        private static string CleanupFolder(string folder, string fileName)
        {
            if (folder.EndsWith(fileName))
            {
                folder = folder.Substring(0, folder.Length - fileName.Length);
            }
            return folder.Trim('/');
        }

        /// <summary>Returns the path of a child being added to the current path.</summary>
        /// <param name="node">The node.</param>
        /// <returns>The path of a node being added to the path.</returns>
        public DataPath AddChild(Node node)
        {
            var path = string.IsNullOrEmpty(FolderPath) ?
                GetSuffix(node) :
                $"{FolderPath}/{GetSuffix(node)}";
            return new DataPath(path, FileSystemStorage.DataFile);
        }

        private static string GetSuffix(Node node)
        {
            var type = node.GetType();
            var attribute = GitPathAttribute.Get(type);
            var folder = attribute?.FolderName ?? type.Name;
            return string.IsNullOrEmpty(folder) ? node.Id.ToString() : $"{folder}/{node.Id}";
        }

        /// <inheritdoc/>
        public override string ToString() => FilePath;

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            Equals(obj as DataPath);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            FolderPath.GetHashCode(StringComparison.Ordinal);

        /// <inheritdoc/>
        public bool Equals(DataPath other) =>
            string.Equals(FilePath, other?.FilePath, StringComparison.Ordinal);

        internal DataPath GetResourceParentNode()
        {
            int position = FolderPath.IndexOf($"/{FileSystemStorage.ResourceFolder}/");
            if (position == -1)
            {
                throw new InvalidOperationException($"Path doesn't refer to a resource.");
            }
            return new DataPath(FolderPath.Substring(0, position - 1), FileSystemStorage.DataFile);
        }
    }
}
