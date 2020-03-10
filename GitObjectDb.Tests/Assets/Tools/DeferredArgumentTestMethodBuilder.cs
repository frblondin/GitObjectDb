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
    internal class DeferredArgumentTestMethodBuilder : ITestMethodBuilder
    {
        /// <summary>
        /// We're saved because `parameterValues` is inherently deferred in its execution.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="suite"></param>
        /// <param name="parameterValues"></param>
        /// <param name="autoDataStartIndex"></param>
        /// <returns></returns>
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
                RunState = normalTestMethod.RunState
            };

            var originalProperties = normalTestMethod.Properties;
            foreach (var propKey in originalProperties.Keys)
            {
                testMethod.Properties.Add(propKey, originalProperties[propKey]);
            }

            return testMethod;
        }
    }
    internal class DeferredTestMethod : TestMethod
    {
        // should be readonly, but LazyInitializer doesn't have 'in' modifier for the lock.
        private static object _staticLock = new object();

        public DeferredTestMethod(IMethodInfo method, Test parentSuite) : base(method, parentSuite)
        {
        }

        public IEnumerable<object> DeferredArguments { get; set; }

        private bool _argsInitialized = false;

        private object[] _lockedArguments;

        public override object[] Arguments
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _lockedArguments, ref _argsInitialized, ref _staticLock, () => DeferredArguments.ToArray());
            }
        }
    }
}
