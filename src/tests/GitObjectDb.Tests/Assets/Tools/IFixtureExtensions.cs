using System;

namespace AutoFixture;

public static class IFixtureExtensions
{
    public static void LazyRegister<T>(this IFixture fixture, Func<T> func)
    {
        Lazy<T> lazy = new(func);
        fixture.Register(() => lazy.Value);
    }
}
