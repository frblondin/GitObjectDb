using NUnit.Framework;

namespace GitObjectDb.Tests
{
    public class FileSystemStorageTests
    {
        [Test]
        public void ThrowIfAnyReservedName()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<GitObjectDbException>(() => FileSystemStorage.ThrowIfAnyReservedName($"{FileSystemStorage.ResourceFolder}"));
                Assert.Throws<GitObjectDbException>(() => FileSystemStorage.ThrowIfAnyReservedName($"/{FileSystemStorage.ResourceFolder}"));
                Assert.Throws<GitObjectDbException>(() => FileSystemStorage.ThrowIfAnyReservedName($"{FileSystemStorage.ResourceFolder}/file.json"));
                Assert.Throws<GitObjectDbException>(() => FileSystemStorage.ThrowIfAnyReservedName($"/{FileSystemStorage.ResourceFolder}/file.json"));
            });
        }

        [Test]
        public void DoesNotThrowIfNoReservedName()
        {
            Assert.DoesNotThrow(() => FileSystemStorage.ThrowIfAnyReservedName($"A/B/C.json"));
        }
    }
}
