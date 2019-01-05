using GitObjectDb.Models;
using System;
using System.Collections.Generic;

namespace GitObjectDb.Reflection
{
    internal partial class ModelDataAccessor
    {
        private class TransformationLookupComparer : IEqualityComparer<(UniqueId InstanceId, string Name)>
        {
            internal static TransformationLookupComparer Instance { get; } = new TransformationLookupComparer();

            private TransformationLookupComparer()
            {
            }

            public bool Equals((UniqueId InstanceId, string Name) x, (UniqueId InstanceId, string Name) y) =>
                x.InstanceId == y.InstanceId &&
                StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);

            public int GetHashCode((UniqueId InstanceId, string Name) obj)
            {
                var h1 = obj.InstanceId.GetHashCode();
                var h2 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
                return ((h1 << 5) + h1) ^ h2;
            }
        }
    }
}
