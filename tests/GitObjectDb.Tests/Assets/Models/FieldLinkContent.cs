using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models
{
    [Record]
    public partial class FieldLinkContent : IEquatable<FieldLinkContent>
    {
        public ILazyLink<Page> Target { get; }

        public bool Equals(FieldLinkContent other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return false;
            }
            return EqualityComparer<ILazyLink<Page>>.Default.Equals(Target, other.Target);
        }

        public override bool Equals(object obj) => Equals(obj as FieldLinkContent);

        public override int GetHashCode() => Target?.GetHashCode() ?? 0;
    }
}
