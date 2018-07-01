using GitObjectDb.Compare;
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
        readonly ComputeTreeChanges.Factory _computeTreeChangesFactory;

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
            _computeTreeChangesFactory = serviceProvider.GetService<ComputeTreeChanges.Factory>();
        }
    }
}
