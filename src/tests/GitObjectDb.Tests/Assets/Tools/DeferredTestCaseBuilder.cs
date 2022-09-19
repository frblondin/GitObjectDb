using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitObjectDb.Tests.Assets.Tools;

#if NET6_0_OR_GREATER
internal static class DeferredTestCaseBuilder
{
    private static readonly Regex _insertSpacesBetweenWords = new(@"([a-z])([A-Z])([\w])", RegexOptions.Compiled);

    public static DeferredTestMethod BuildTestMethod(IMethodInfo method, Test parentSuite, object[] displayParameters, IEnumerable<object> parameters)
    {
        var normalTestMethod = new NUnitTestCaseBuilder().BuildTestMethod(method, parentSuite, new TestCaseParameters(displayParameters));

        var testMethod = new DeferredTestMethod(normalTestMethod.Method, parentSuite)
        {
            Seed = normalTestMethod.Seed,
            Name = _insertSpacesBetweenWords.Replace(normalTestMethod.Name, m => $"{m.Groups[1].Value} {m.Groups[2].Value.ToLowerInvariant()}{m.Groups[3].Value}"),
            FullName = normalTestMethod.FullName,
            DeferredArguments = parameters,
            RunState = normalTestMethod.RunState,
        };

        var originalProperties = normalTestMethod.Properties;
        foreach (var propKey in originalProperties.Keys)
        {
            testMethod.Properties.Add(propKey, originalProperties[propKey]);
        }

        return testMethod;
    }
}
#endif
