using AutoFixture;
using AutoFixture.NUnit3;
using System;

namespace GitObjectDb.Tests.Assets.Tools
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class InlineAutoDataCustomizationsAttribute : InlineAutoDataAttribute
    {
        public InlineAutoDataCustomizationsAttribute(Type[] customizationTypes, params object[] arguments)
            : base(() => AutoDataCustomizationsAttribute.CreateFixture(customizationTypes), arguments)
        {
#if NET6_0_OR_GREATER
            TestMethodBuilder = new DeferredArgumentTestMethodBuilder();
#endif
        }
    }
}
