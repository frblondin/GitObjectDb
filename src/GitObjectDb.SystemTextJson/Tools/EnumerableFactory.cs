using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitObjectDb.SystemTextJson.Tools;
internal abstract class EnumerableFactory
{
    private static readonly ConcurrentDictionary<Type, EnumerableFactory> _factories = new();

    internal static EnumerableFactory Get(Type elementType)
    {
        return _factories.GetOrAdd(elementType,
            static t => (EnumerableFactory)Activator.CreateInstance(
                typeof(EnumerableFactory<>).MakeGenericType(t))!);
    }

    internal abstract object Create(Type type, IEnumerable values);
}

#pragma warning disable SA1402 // File may only contain a single type
internal class EnumerableFactory<T> : EnumerableFactory
{
    internal override object Create(Type type, IEnumerable values)
    {
        if (type == typeof(IList<T>) ||
            type == typeof(List<T>))
        {
            return values.Cast<T>().ToList();
        }
        if (type == typeof(T[]))
        {
            return values.Cast<T>().ToArray();
        }
        if (type == typeof(IEnumerable<T>) ||
            type == typeof(IImmutableList<T>) ||
            type == typeof(ImmutableList<T>))
        {
            return values.Cast<T>().ToImmutableList();
        }
        if (type == typeof(ImmutableArray<T>))
        {
            return values.Cast<T>().ToImmutableArray();
        }
        throw new NotSupportedException($"Cannot create collections of type {type}.");
    }
}