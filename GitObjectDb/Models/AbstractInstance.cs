using Autofac;
using LibGit2Sharp;
using GitObjectDb.Attributes;
using GitObjectDb.Compare;
using GitObjectDb.Models;
using GitObjectDb.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.ComponentModel;

namespace GitObjectDb.Models
{
    [DebuggerDisplay(DebuggerDisplay + ", IsRepositoryAttached = {_getRepository != null}")]
    [DataContract]
    public abstract partial class AbstractInstance : AbstractModel
    {
        readonly IComponentContext _context;
        readonly IModelDataAccessorProvider _dataAccessorProvider;
        readonly ComputeTreeChanges.Factory _computeTreeChangesFactory;

        [JsonConstructor]
        public AbstractInstance(IComponentContext context, IModelDataAccessorProvider dataAccessorProvider, ComputeTreeChanges.Factory computeTreeChangesFactory, Guid id, string name) : base(id, name)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dataAccessorProvider = dataAccessorProvider ?? throw new ArgumentNullException(nameof(dataAccessorProvider));
            _computeTreeChangesFactory = computeTreeChangesFactory;
        }

        protected override void CreateNewParent(IMetadataObject @new) { }
    }
}
