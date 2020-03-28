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
#if NET6_0_OR_GREATER
    internal class DeferredArgumentTestMethodBuilder : ITestMethodBuilder
    {
        /// <summary>
        /// We're saved because `parameterValues` is inherently deferred in its execution.
        /// </summary>
        /// <param name="method">The <see cref="T:NUnit.Framework.Interfaces.IMethodInfo" /> for which tests are to be constructed.</param>
        /// <param name="suite">The suite to which the tests will be added.</param>
        /// <param name="parameterValues">The argument values generated for the test case.</param>
        /// <param name="autoDataStartIndex">Index at which the automatically generated values start.</param>
        /// <returns>The test method.</returns>
        public TestMethod Build(IMethodInfo method, Test suite, IEnumerable<object> parameterValues, int autoDataStartIndex)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (parameterValues is null)
            {
                throw new ArgumentNullException(nameof(parameterValues));
            }

            return new DeferredTestCaseBuilder().BuildTestMethod(method, suite, GetDisplayParameters(method, parameterValues, autoDataStartIndex), parameterValues);
        }

        private static object[] GetDisplayParameters(IMethodInfo method, IEnumerable<object> deferredParams, int autoDataStartIndex)
        {
            return deferredParams.Take(autoDataStartIndex) // Only take parameter values that are not managed by AutoFixture
                   .Concat(method.GetParameters().Skip(autoDataStartIndex).Select(p => new TypeNameRenderer(p.ParameterType))).ToArray();
        }

        private class TypeNameRenderer
        {
            private readonly Type _argType;

            public TypeNameRenderer(Type argType)
            {
                _argType = argType;
            }

            public override string ToString() => "auto<" + _argType.Name + ">";
        }
    }
#endif
}
