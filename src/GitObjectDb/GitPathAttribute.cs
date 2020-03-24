using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GitObjectDb
{
    /// <summary>Instructs the engine in which folder name to store nodes.</summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GitPathAttribute : Attribute
    {
        private static readonly ConcurrentDictionary<Type, GitPathAttribute> _cache = new ConcurrentDictionary<Type, GitPathAttribute>();

        /// <summary>Initializes a new instance of the <see cref="GitPathAttribute"/> class.</summary>
        /// <param name="folderName">Name of the folder.</param>
        /// <exception cref="ArgumentException">Folder name cannot be empty and cannot containt '/' character.</exception>
        public GitPathAttribute(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Contains('/', StringComparison.Ordinal))
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

        internal static GitPathAttribute Get(Type type) =>
            _cache.GetOrAdd(type, type => type.GetCustomAttribute<GitPathAttribute>());
    }
}
