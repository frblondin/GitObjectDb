using GitObjectDb.Attributes;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace GitObjectDb.Tests.Assets.Models
{
    [DataContract]
    public class Page : AbstractModel
    {
        public Application Application => (Application)Parent ?? throw new NullReferenceException("No parent has been set.");

        public ILazyChildren<Field> Fields { get; }

        public delegate Page Factory(Guid id, string name, LazyChildren<Field> fields);
        public Page(IServiceProvider serviceProvider, Guid id, string name, ILazyChildren<Field> fields) : base(serviceProvider, id, name)
        {
            Fields = (fields ?? throw new ArgumentNullException(nameof(fields))).AttachToParent(this);
        }
    }
}
