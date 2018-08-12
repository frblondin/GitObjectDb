using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq
{
    public static class IReadOnlyListExtensions
    {
        static readonly Random _random = new Random();

        public static T PickRandom<T>(this IReadOnlyList<T> source) =>
            source[_random.Next(source.Count)];
    }
}
