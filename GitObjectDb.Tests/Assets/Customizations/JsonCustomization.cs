using AutoFixture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class JsonCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register(() => JObject.Parse($@"{{""{fixture.Create<string>()}"": ""{fixture.Create<string>()}""}}"));
            fixture.Register<JToken>(() => fixture.Create<JObject>());
        }
    }
}
