namespace GitObjectDb.Tests.Assets.Data.Software;

public class SoftwareBenchmarkCustomization : SoftwareCustomization
{
    public const int DefaultApplicationCount = 2;
    public const int DefaultTablePerApplicationCount = 200;
    public const int DefaultFieldPerTableCount = 30;
    public const int DefaultConstantPerTableCount = 2;
    public const int DefaultResourcePerTableCount = 5;

    public SoftwareBenchmarkCustomization()
        : base(GitObjectDbFixture.SoftwareBenchmarkRepositoryPath)
    {
    }

    public override int ApplicationCount => DefaultApplicationCount;

    public override int TablePerApplicationCount => DefaultTablePerApplicationCount;

    public override int FieldPerTableCount => DefaultFieldPerTableCount;

    public override int ConstantPerTableCount => DefaultConstantPerTableCount;

    public override int ResourcePerTableCount => DefaultResourcePerTableCount;
}
