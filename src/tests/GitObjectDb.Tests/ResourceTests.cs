using AutoFixture;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public class ResourceTests
{
    [Test]
    public async Task StreamResourceValue()
    {
        // Arrange
        var fixture = new Fixture();
        var value = fixture.Create<string>();
        var sut = new Resource.Data(value);

        // Act
        using var stream = await sut.GetContentStreamAsync();
        using var reader = new StreamReader(stream, leaveOpen: true);

        // Assert
        Assert.That(reader.ReadToEnd(), Is.EqualTo(value));
    }

    [Test]
    public async Task StreamResourceValueSupportsRepositioning()
    {
        // Arrange
        var fixture = new Fixture();
        var value = fixture.Create<string>();
        var sut = new Resource.Data(value);

        // Act
        using var stream = await sut.GetContentStreamAsync();
        using var reader = new StreamReader(stream, leaveOpen: true);
        reader.ReadToEnd();
        stream.Position = 0L;

        // Assert
        Assert.That(reader.ReadToEnd(), Is.EqualTo(value));
    }

    [Test]
    public async Task StreamResourceValueSupportsAbsoluteSeek()
    {
        // Arrange
        var fixture = new Fixture();
        var value = fixture.Create<string>();
        var sut = new Resource.Data(value);

        // Act
        using var stream = await sut.GetContentStreamAsync();
        using var reader = new StreamReader(stream, leaveOpen: true);
        reader.ReadToEnd();
        stream.Seek(0L, SeekOrigin.Begin);

        // Assert
        Assert.That(reader.ReadToEnd(), Is.EqualTo(value));
    }

    [Test]
    public async Task StreamResourceValueThrowsExceptionForNonZeroSeek()
    {
        // Arrange
        var fixture = new Fixture();
        var value = fixture.Create<string>();
        var sut = new Resource.Data(value);

        // Act
        using var stream = await sut.GetContentStreamAsync();
        stream.Position = 0L;

        // Assert
        Assert.Throws<NotSupportedException>(() => stream.Position = 1L);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0L, SeekOrigin.Current));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0L, SeekOrigin.End));
    }
}
