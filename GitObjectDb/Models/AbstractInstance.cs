using LibGit2Sharp;
using GitObjectDb.Attributes;
using GitObjectDb.Compare;
using GitObjectDb.Models;
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
using GitObjectDb.Reflection;

namespace GitObjectDb.Models
{
    [DebuggerDisplay(DebuggerDisplay + ", IsRepositoryAttached = {_getRepository != null}")]
    [DataContract]
    public abstract partial class AbstractInstance : AbstractModel
    {
        readonly ComputeTreeChanges.Factory _computeTreeChangesFactory;

        [JsonConstructor]
        public AbstractInstance(IServiceProvider serviceProvider, Guid id, string name) : base(serviceProvider, id, name)
        {
            _computeTreeChangesFactory = serviceProvider.GetService<ComputeTreeChanges.Factory>();
        }
    }
}
