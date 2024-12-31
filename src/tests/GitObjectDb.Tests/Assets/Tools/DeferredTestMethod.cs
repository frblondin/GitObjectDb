using AutoFixture.NUnit3;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GitObjectDb.Tests.Assets.Tools;

internal class DeferredTestMethod : TestMethod
{
    // Should be readonly, but LazyInitializer doesn't have 'in' modifier for the lock.
    private static object _staticLock = new();
    private bool _argsInitialized;
    private object[] _lockedArguments;

    public DeferredTestMethod(IMethodInfo method, Test parentSuite)
        : base(method, parentSuite)
    {
    }

    public IEnumerable<object> DeferredArguments { get; set; }

    public override object[] Arguments =>
        LazyInitializer.EnsureInitialized(ref _lockedArguments, ref _argsInitialized, ref _staticLock, () => [.. DeferredArguments]);
}
