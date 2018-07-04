using GitObjectDb.Compare;
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
    public abstract partial class AbstractInstance : AbstractModel
    {
        readonly Func<Func<IRepository>, IComputeTreeChanges> _computeTreeChangesFactory;

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
            _computeTreeChangesFactory = serviceProvider.GetService<Func<Func<IRepository>, IComputeTreeChanges>>();
            GetRepository = () => _getRepository?.Invoke() ?? throw new NotSupportedException("The module is not attached to a repository.");
        }
    }
}
