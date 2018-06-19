using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Order<T>(this IEnumerable<T> source) =>
            source.OrderBy(v => v);
    }
}
