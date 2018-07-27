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
    public class Application : AbstractModel
    {
        public Application(IServiceProvider serviceProvider, Guid id, string name, ILazyChildren<Page> pages)
            : base(serviceProvider, id, name)
        {
            Pages = (pages ?? throw new ArgumentNullException(nameof(pages))).AttachToParent(this);
        }

        public ILazyChildren<Page> Pages { get; }
    }
}
