namespace GitObjectDb.Tests.Assets.Data.Software
{
    public class SoftwareBenchmarkCustomization : SoftwareCustomization
    {
        public const int DefaultApplicationCount = 2;
        public const int DefaultTablePerApplicationCount = 200;
        public const int DefaultFieldPerTableCount = 30;
        public const int DefaultResourcePerTableCount = 5;

        public SoftwareBenchmarkCustomization()
            : base(
                  DefaultApplicationCount,
                  DefaultTablePerApplicationCount,
                  DefaultFieldPerTableCount,
                  DefaultResourcePerTableCount,
                  GitObjectDbFixture.SoftwareBenchmarkRepositoryPath)
        {
        }
    }
}
