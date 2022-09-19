using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq;

internal static class IEnumerableExtensions
{
    internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
}
