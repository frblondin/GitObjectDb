using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides a description of a repository dependency.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("Id = {Id}, Version = {Version}, Name = {Name}")]
    public class RepositoryDependency
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryDependency"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="version">The version.</param>
        /// <exception cref="ArgumentNullException">
        /// name
        /// or
        /// version
        /// </exception>
        [JsonConstructor]
        public RepositoryDependency(UniqueId id, string name, Version version)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryDependency"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="version">The version.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public RepositoryDependency(IObjectRepository repository, Version version = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            Id = repository.Id;
            Name = repository.Name;
            Version = version ?? repository.Version;
        }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        [DataMember]
        public UniqueId Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        [DataMember]
        public string Name { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        [DataMember]
        public Version Version { get; }
    }
}