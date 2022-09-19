using AutoFixture.NUnit3;
using NUnit.Framework;
using System;
using System.IO;

namespace GitObjectDb.Tests
{
    public class ResourceTests
    {
        [Test]
        [AutoData]
        public void StreamResourceValue(string value)
        {
            // Arrange
            var sut = new Resource.Data(value);

            // Act
            using var stream = sut.GetContentStream();
            using var reader = new StreamReader(stream, leaveOpen: true);

            // Assert
            Assert.That(reader.ReadToEnd(), Is.EqualTo(value));
        }

        [Test]
        [AutoData]
        public void StreamResourceValueSupportsRepositioning(string value)
        {
            // Arrange
            var sut = new Resource.Data(value);

            // Act
            using var stream = sut.GetContentStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            reader.ReadToEnd();
            stream.Position = 0L;

            // Assert
            Assert.That(reader.ReadToEnd(), Is.EqualTo(value));
        }

        [Test]
        [AutoData]
        public void StreamResourceValueSupportsAbsoluteSeek(string value)
        {
            // Arrange
            var sut = new Resource.Data(value);

            // Act
            using var stream = sut.GetContentStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            reader.ReadToEnd();
            stream.Seek(0L, SeekOrigin.Begin);

            // Assert
            Assert.That(reader.ReadToEnd(), Is.EqualTo(value));
        }

        [Test]
        [AutoData]
        public void StreamResourceValueThrowsExceptionForNonZeroSeek(string value)
        {
            // Arrange
            var sut = new Resource.Data(value);

            // Act
            using var stream = sut.GetContentStream();
            stream.Position = 0L;

            // Assert
            Assert.Throws<NotImplementedException>(() => stream.Position = 1L);
            Assert.Throws<NotImplementedException>(() => stream.Seek(0L, SeekOrigin.Current));
            Assert.Throws<NotImplementedException>(() => stream.Seek(0L, SeekOrigin.End));
        }
    }
}
