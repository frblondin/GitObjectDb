using AutoFixture.Kernel;
using AutoFixture.NUnit3;
using GitObjectDb.IO;
using GitObjectDb.Tests.Assets.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitObjectDb.Tests.IO
{
    public partial class StringBuilderStreamTests
    {
        [Test]
        [AutoDataCustomizations(typeof(StringBuilderStreamCustomization))]
        public void ReadToEnd(StringBuilder stringBuilder)
        {
            // Act
            string result;
            StringBuilderStream sut = null;
            try
            {
                sut = new StringBuilderStream(stringBuilder);
                using (var reader = new StreamReader(sut, Encoding.Default, true, 1024, true))
                {
                    result = reader.ReadToEnd();
                }
            }
            finally
            {
                sut?.Dispose();
            }

            // Assert
            Assert.That(result, Is.EqualTo(stringBuilder.ToString()));
        }
    }
}
