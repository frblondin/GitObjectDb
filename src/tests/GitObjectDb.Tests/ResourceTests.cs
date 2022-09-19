using AutoFixture.NUnit3;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System;
using System.IO;

namespace GitObjectDb.Tests;

public class ResourceTests
{
    [Test]
    [AutoDataCustomizations]
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
    [AutoDataCustomizations]
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
    [AutoDataCustomizations]
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
    [AutoDataCustomizations]
    public void StreamResourceValueThrowsExceptionForNonZeroSeek(string value)
    {
        // Arrange
        var sut = new Resource.Data(value);

        // Act
        using var stream = sut.GetContentStream();
        stream.Position = 0L;

        // Assert
        Assert.Throws<NotSupportedException>(() => stream.Position = 1L);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0L, SeekOrigin.Current));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0L, SeekOrigin.End));
    }
}
