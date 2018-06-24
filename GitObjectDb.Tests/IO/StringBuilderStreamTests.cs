using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.NUnit3;
using GitObjectDb.IO;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Utils;
using NUnit.Framework;

namespace GitObjectDb.Tests.IO
{
    public class StringBuilderStreamTests
    {
        [Test, AutoDataCustomizations(typeof(StringBuilderStreamCustomization))]
        public void ReadToEnd(StringBuilder stringBuilder)
        {
            // Act
            string result;
            using (var sut = new StringBuilderStream(stringBuilder))
            {
                using (var reader = new StreamReader(sut, Encoding.Default, true, 1024, true))
                {
                    result = reader.ReadToEnd();
                }
            }

            // Assert
            Assert.That(result, Is.EqualTo(stringBuilder.ToString()));
        }

        public class StringBuilderStreamCustomization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                var stringBuilder = new StringBuilder();
                var @string = fixture.Create<string>();
                foreach (var i in Enumerable.Range(0, 1000))
                    stringBuilder.Append(@string);

                fixture.Inject(stringBuilder);
            }
        }
    }
}
