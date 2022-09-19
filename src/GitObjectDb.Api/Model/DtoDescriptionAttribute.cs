namespace GitObjectDb.Api.Model;

[AttributeUsage(AttributeTargets.Class)]
public class DtoDescriptionAttribute : Attribute
{
    public DtoDescriptionAttribute(string entitySetName)
    {
        EntitySetName = entitySetName;
    }

    public string EntitySetName { get; }
}
