using GitObjectDb.Compare;
using GitObjectDb.Git;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Abstract root model containing nested <see cref="IMetadataObject"/> children.
    /// </summary>
    /// <seealso cref="AbstractModel" />
    /// <seealso cref="IInstance" />
    [DebuggerDisplay(DebuggerDisplay + ", IsRepositoryAttached = {_getRepository != null}")]
    [DataContract]
    public abstract partial class AbstractInstance : AbstractModel, IInstance
    {
        readonly Func<RepositoryDescription, IComputeTreeChanges> _computeTreeChangesFactory;
        readonly Func<RepositoryDescription, ObjectId, string, IMetadataTreeMerge> _metadataTreeMergeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractInstance"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        [JsonConstructor]
        protected AbstractInstance(IServiceProvider serviceProvider, Guid id, string name)
            : base(serviceProvider, id, name)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _computeTreeChangesFactory = serviceProvider.GetRequiredService<Func<RepositoryDescription, IComputeTreeChanges>>();
            _metadataTreeMergeFactory = serviceProvider.GetRequiredService<Func<RepositoryDescription, ObjectId, string, IMetadataTreeMerge>>();
            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _instanceLoader = serviceProvider.GetRequiredService<IInstanceLoader>();
        }
    }
}
