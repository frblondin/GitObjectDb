using NUnit.Framework;
using System;
using System.Linq;

namespace GitObjectDb.Tests;

public abstract class DisposeArguments
{
    [TearDown]
    public void DisposeTestArguments()
    {
        foreach (var disposable in TestContext.CurrentContext.Test?.Arguments.OfType<IDisposable>())
        {
            disposable.Dispose();
        }
    }
}
