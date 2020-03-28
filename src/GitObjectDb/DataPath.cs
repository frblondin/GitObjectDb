using System;

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
            (FolderPath, FolderName) = CleanupFolder(folderPath, fileName);
            FileName = fileName;

            _filePath = new Lazy<string>(() =>
                string.IsNullOrEmpty(FolderPath) ? FileName : $"{FolderPath}/{FileName}");
        }

        /// <summary>Gets the folder path.</summary>
        public string FolderPath { get; }

        /// <summary>Gets the folder name.</summary>
        public string FolderName { get; }

        /// <summary>Gets the blob data path, holding the serialized representation of a node in the repository.</summary>
        public string FilePath => _filePath.Value;

        /// <summary>Gets the name of the file containing data.</summary>
        public string FileName { get; }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="DataPath" /> objects are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(DataPath left, DataPath right) =>
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

        /// <summary>
        /// Indicates whether the values of two specified <see cref="DataPath" /> objects are not equal.
        /// </summary>
        /// <param name="left">The first object to compare. </param>
        /// <param name="right">The second object to compare. </param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(DataPath left, DataPath right) => !(left == right);

        /// <summary>Gets the root path of the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns>The root path.</returns>
        public static DataPath Root(Node node) =>
            new DataPath(GetSuffix(node), FileSystemStorage.DataFile);

        internal static DataPath Root(string folderName, UniqueId id) =>
            new DataPath($"{folderName}/{id}", FileSystemStorage.DataFile);

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

        private static (string Path, string Name) CleanupFolder(string folder, string fileName)
        {
            if (folder.EndsWith(fileName, StringComparison.Ordinal))
            {
                folder = folder.Substring(0, folder.Length - fileName.Length);
            }
            var path = folder.Trim('/');
            var lastSlash = path.LastIndexOf('/');
            return (path, lastSlash != -1 ? path.Substring(lastSlash + 1) : string.Empty);
        }

        internal DataPath AddChild(Node node)
        {
            var path = string.IsNullOrEmpty(FolderPath) ?
                GetSuffix(node) :
                $"{FolderPath}/{GetSuffix(node)}";
            return new DataPath(path, FileSystemStorage.DataFile);
        }

        internal DataPath AddChild(string folderName, UniqueId id) =>
            new DataPath($"{FolderPath}/{folderName}/{id}", FileSystemStorage.DataFile);

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
        public bool Equals(DataPath? other) =>
            string.Equals(FilePath, other?.FilePath, StringComparison.Ordinal);

        internal DataPath GetResourceParentNode()
        {
            int position = FolderPath.IndexOf($"/{FileSystemStorage.ResourceFolder}/", StringComparison.Ordinal);
            if (position == -1)
            {
                throw new InvalidOperationException($"Path doesn't refer to a resource.");
            }
            return new DataPath(FolderPath.Substring(0, position), FileSystemStorage.DataFile);
        }
    }
}
