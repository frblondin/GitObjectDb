using AutoFixture;
using AutoFixture.NUnit3;
using System;

namespace GitObjectDb.Tests.Assets.Utils
{
    public class AutoDataCustomizationsAttribute : AutoDataAttribute
    {
        static IFixture CreateFixture(params Type[] customizationTypes)
        {
            var result = new Fixture();
            foreach (var type in customizationTypes)
            {
                var customization = (ICustomization)Activator.CreateInstance(type);
                result.Customize(customization);
            }
            return result;
        }

        public AutoDataCustomizationsAttribute(params Type[] customizationTypes) :
            base(() => CreateFixture(customizationTypes))
        {
        }
    }
}
