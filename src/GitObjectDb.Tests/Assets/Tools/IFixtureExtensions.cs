using System;

namespace AutoFixture
{
    internal static class IFixtureExtensions
    {
        internal static void LazyRegister<T>(this IFixture fixture, Func<T> func)
        {
            Lazy<T> lazy = new(func);
            fixture.Register(() => lazy.Value);
        }
    }
}
