using GitObjectDb.Model;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitObjectDb
{
    /// <summary>Represents a data path.</summary>
    public sealed class DataPath : IEquatable<DataPath>
    {
        private static readonly Regex _isInResourceFolderRegex = new Regex($"/{FileSystemStorage.ResourceFolder}[/$]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Lazy<string> _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPath"/> class.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="fileName">The file name containing data.</param>
        /// <param name="useNodeFolders">Sets whether node should be stored in a nested folder (FolderName/NodeId/data.json) or not (FolderName/NodeId.json).</param>
        public DataPath(string folderPath, string fileName, bool useNodeFolders)
        {
            UseNodeFolders = useNodeFolders;
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

        /// <summary>Gets a value indicating whether the path represents a path to a node (e.g. not a resource).</summary>
        public bool IsNode => !_isInResourceFolderRegex.IsMatch(FolderPath);

        /// <summary>Gets a value indicating whether node should be stored in a nested folder (FolderName/NodeId/data.json) or not (FolderName/NodeId.json).</summary>
        public bool UseNodeFolders { get; }

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
        public static DataPath Root(Node node)
        {
            var useNodeFolders = GetSuffix(node, out var suffix);
            return new DataPath(suffix, $"{node.Id}.json", useNodeFolders);
        }

        internal static DataPath Root(string folderName, UniqueId id, bool useNodeFolders) =>
            new DataPath(useNodeFolders ? $"{folderName}/{id}" : folderName, $"{id}.json", useNodeFolders);

        /// <summary>Converts the specified string representation to its <see cref="DataPath" /> equivalent.</summary>
        /// <param name="path">A string containing a sha to convert.</param>
        /// <returns>The <see cref="DataPath" /> value equivalent to the path contained in <paramref name="path" />.</returns>
        /// <exception cref="ArgumentException">Wrong path provided.</exception>
        public static DataPath Parse(string path)
        {
            if (!TryParse(path, out var result))
            {
                throw new ArgumentException("Wrong path provided.", nameof(path));
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
            var folder = path.Substring(0, separator);
            var file = path.Substring(separator + 1);
            var useNodeFolders = !folder.Contains(FileSystemStorage.ResourceFolder) && folder.EndsWith(System.IO.Path.GetFileNameWithoutExtension(file));
            result = separator != -1 ?
                new DataPath(folder, file, useNodeFolders) :
                new DataPath(string.Empty, path, false);

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

        /// <summary>
        /// Adds a node to the current instance.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        /// <returns>The new <see cref="DataPath"/> representing the child node path.</returns>
        public DataPath AddChild(Node node)
        {
            var useNodeFolders = GetSuffix(node, out var suffix);
            var path = string.IsNullOrEmpty(FolderPath) ? suffix : $"{FolderPath}/{suffix}";
            return new DataPath(path, $"{node.Id}.json", useNodeFolders);
        }

        internal DataPath AddChild(string folderName, UniqueId id, bool useNodeFolders)
        {
            var folder = useNodeFolders ? $"{FolderPath}/{folderName}/{id}" : $"{FolderPath}/{folderName}";
            return new DataPath(folder, $"{id}.json", useNodeFolders);
        }

        private static bool GetSuffix(Node node, out string suffix)
        {
            var type = node.GetType();
            var attribute = GitFolderAttribute.Get(type);
            var folder = attribute?.FolderName ?? $"{type.Name}s";
            var result = attribute?.UseNodeFolders ?? GitFolderAttribute.DefaultUseNodeFoldersValue;
            suffix = result ? $"{folder}/{node.Id}" : folder;
            return result;
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
            int position = Array.FindIndex(FolderParts, p => StringComparer.Ordinal.Equals(p, FileSystemStorage.ResourceFolder));
            if (position == -1)
            {
                throw new InvalidOperationException($"Path doesn't refer to a resource.");
            }
            return new DataPath(string.Join("/", FolderParts.Take(position)), $"{FolderParts[position - 1]}.json", true);
        }

        internal DataPath CreateResourcePath(string folderPath, string file)
        {
            ThrowIfNotUsingNodeFolders();

            var folder = string.IsNullOrEmpty(folderPath) ? string.Empty : $"/{folderPath}";
            return Parse($"{FolderPath}/{FileSystemStorage.ResourceFolder}{folder}/{file}");
        }

        internal void ThrowIfNotUsingNodeFolders()
        {
            if (!UseNodeFolders)
            {
                throw new GitObjectDbException("The path contains reserved folder names;");
            }
        }
    }
}
