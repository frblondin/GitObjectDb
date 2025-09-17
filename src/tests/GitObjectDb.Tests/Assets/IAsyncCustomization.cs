using System.Threading.Tasks;

namespace AutoFixture;
public interface IAsyncCustomization
{
    Task CustomizeAsync(IFixture fixture);
}

public static class IAsyncCustomizationExtensions
{
    public static async Task<IFixture> CustomizeAsync<TCustomization>(this IFixture fixture)
        where TCustomization : IAsyncCustomization, new()
    {
        var customization = new TCustomization();
        await customization.CustomizeAsync(fixture);
        return fixture;
    }
}
