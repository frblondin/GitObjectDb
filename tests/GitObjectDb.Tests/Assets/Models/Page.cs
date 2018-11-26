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
    [Model]
    public partial class Page
    {
        public Application Application =>
            (Application)Parent ?? throw new NotSupportedException("No parent has been set.");

        [DataMember]
        [Modifiable]
        public string Description { get; }

        public ILazyChildren<Field> Fields { get; }
    }
}
