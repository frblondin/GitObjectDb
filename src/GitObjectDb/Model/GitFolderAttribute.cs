using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace GitObjectDb.Model
{
    /// <summary>Instructs the engine in which folder name to store nodes.</summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GitFolderAttribute : Attribute
    {
        private static readonly ConcurrentDictionary<Type, GitFolderAttribute> _cache = new ConcurrentDictionary<Type, GitFolderAttribute>();

        /// <summary>Initializes a new instance of the <see cref="GitFolderAttribute"/> class.</summary>
        /// <param name="folderName">Name of the folder.</param>
        /// <exception cref="ArgumentException">Folder name cannot be empty and cannot containt '/' character.</exception>
        public GitFolderAttribute(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || folderName.IndexOf('/') != -1)
            {
                throw new ArgumentException("Folder name cannot be empty and cannot containt '/' character.", nameof(folderName));
            }
            if (FileSystemStorage.ReservedNames.Contains(folderName))
            {
                throw new ArgumentException($"'{FolderName}' is a reserved name and cannot be used as a folder name.", nameof(folderName));
            }

            FolderName = folderName;
        }

        /// <summary>Gets the name of the folder.</summary>
        public string FolderName { get; }

        internal static GitFolderAttribute Get(Type type) =>
            _cache.GetOrAdd(type, type => type.GetCustomAttribute<GitFolderAttribute>(true));
    }
}
