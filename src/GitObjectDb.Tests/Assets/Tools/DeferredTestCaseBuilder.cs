using AutoFixture.NUnit3;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GitObjectDb.Tests.Assets.Tools
{
    internal class DeferredTestCaseBuilder
    {
        public DeferredTestMethod BuildTestMethod(IMethodInfo method, Test parentSuite, object[] displayParameters, IEnumerable<object> parameters)
        {
            var normalTestMethod = new NUnitTestCaseBuilder().BuildTestMethod(method, parentSuite, new TestCaseParameters(displayParameters));

            var testMethod = new DeferredTestMethod(normalTestMethod.Method, parentSuite)
            {
                Seed = normalTestMethod.Seed,
                Name = normalTestMethod.Name,
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
}
