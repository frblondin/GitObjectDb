using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;

namespace GitObjectDb.Tests
{
    public class FileSystemStorageTests
    {
        [Test]
        [AutoDataCustomizations]
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
        [AutoDataCustomizations]
        public void DoesNotThrowIfNoReservedName()
        {
            Assert.DoesNotThrow(() => FileSystemStorage.ThrowIfAnyReservedName($"A/B/C.json"));
        }
    }
}
