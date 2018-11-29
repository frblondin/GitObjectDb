using GitObjectDb.Attributes;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace GitObjectDb.Tests.Assets.Models
{
    [Model]
    public partial class Field
    {
        public Page Page => (Page)Parent ?? throw new NotSupportedException("No parent has been set.");

        [DataMember]
        [Modifiable]
        public FieldContent Content { get; }
    }
}
