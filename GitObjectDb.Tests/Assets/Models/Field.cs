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
    [DataContract]
    public class Field : AbstractModel
    {
        public Page Page => (Page)Parent ?? throw new NullReferenceException("No parent has been set.");

        public delegate Field Factory(Guid id, string name);
        public Field(IServiceProvider serviceProvider, Guid id, string name) : base(serviceProvider, id, name)
        {
        }
    }
}
