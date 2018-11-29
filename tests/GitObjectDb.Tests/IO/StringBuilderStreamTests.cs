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

        [Test]
        [AutoDataCustomizations(typeof(StringBuilderStreamCustomization))]
        public void UnsupportedMembers(StringBuilder stringBuilder)
        {
            using (var sut = new StringBuilderStream(stringBuilder))
            {
                Assert.Throws<NotSupportedException>(() => sut.Seek(default, default));
                Assert.Throws<NotSupportedException>(() => sut.ReadByte());
                Assert.Throws<NotSupportedException>(() => sut.BeginRead(default, default, default, default, default));
                Assert.ThrowsAsync<NotSupportedException>(async () => await sut.CopyToAsync(default, default).ConfigureAwait(false));
                Assert.Throws<NotSupportedException>(() => sut.Write(default, default, default));
                Assert.ThrowsAsync<NotSupportedException>(async () => await sut.WriteAsync(default, default, default, default).ConfigureAwait(false));
                Assert.Throws<NotSupportedException>(() => sut.WriteByte(default));
                Assert.Throws<NotSupportedException>(() => sut.WriteTo(default));
                Assert.Throws<NotSupportedException>(() => sut.BeginWrite(default, default, default, default, default));
                Assert.Throws<NotSupportedException>(() => sut.EndWrite(default));
            }
        }
    }
}
