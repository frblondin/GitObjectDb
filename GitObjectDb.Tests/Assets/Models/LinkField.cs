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
    [DataContract]
    public class LinkField : Field
    {
        public LinkField(IServiceProvider serviceProvider, Guid id, string name, ILazyLink<Page> pageLink)
            : base(serviceProvider, id, name)
        {
            PageLink = (pageLink ?? throw new ArgumentNullException(nameof(pageLink))).AttachToParent(this);
        }

        [DataMember]
        [Modifiable]
        public ILazyLink<Page> PageLink { get; }
    }
}
