using GitObjectDb.Model;
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
        public DataPath(string folderPath, string fileName)
        {
            (FolderParts, FolderPath, FolderName) = CleanupFolder(folderPath, fileName);
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

        /// <summary>Gets the parts of the path.</summary>
        public string[] FolderParts { get; }

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
            if (!TryParse(path, out var result))
            {
                throw new ArgumentException("message", nameof(path));
            }
            return result!;
        }

        /// <summary>Converts the specified string representation to its <see cref="DataPath" /> equivalent and returns a value that indicates whether the conversion succeeded.</summary>
        /// <param name="path">A string containing a sha to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="DataPath" /> value equivalent to the path contained in <paramref name="path" />, if the conversion succeeded, or default if the conversion failed. The conversion fails if the <paramref name="path" /> parameter is <see langword="null" />, is an empty string (""), or does not contain a valid string representation of a sha. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the <paramref name="path" /> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
        public static bool TryParse(string path, out DataPath? result)
        {
            if (string.IsNullOrWhiteSpace(path) || !path[0].Equals('/'))
            {
                result = default;
            }

            var separator = path.LastIndexOf('/');
            result = separator != -1 ?
                new DataPath(path.Substring(0, separator), path.Substring(separator + 1)) :
                new DataPath(string.Empty, path);

            return true;
        }

        private static (string[] FolderParts, string Path, string Name) CleanupFolder(string folder, string fileName)
        {
            var folderParts = folder.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (folder.EndsWith(fileName, StringComparison.Ordinal))
            {
                folder = folder.Substring(0, folder.Length - fileName.Length);
            }
            var path = folder.Trim('/');
            var lastSlash = path.LastIndexOf('/');
            return (folderParts, path, lastSlash != -1 ? path.Substring(lastSlash + 1) : string.Empty);
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
            var folder = GetFolderName(type);
            return string.IsNullOrEmpty(folder) ? node.Id.ToString() : $"{folder}/{node.Id}";
        }

        internal static string GetFolderName(Type type)
        {
            var attribute = GitPathAttribute.Get(type);
            return attribute?.FolderName ?? $"{type.Name}s";
        }

        /// <inheritdoc/>
        public override string ToString() => FilePath;

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            Equals(obj as DataPath);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(FilePath);

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

        internal DataPath CreateResourcePath(DataPath resourcePath)
        {
            return FromGitBlobPath($"{FolderPath}/{FileSystemStorage.ResourceFolder}/{resourcePath.FilePath}");
        }
    }
}
