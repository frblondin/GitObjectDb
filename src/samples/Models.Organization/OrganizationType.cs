using GitObjectDb;
using GitObjectDb.Model;

namespace Models.Organization;

/// <summary>Gets the type of an organization.</summary>
[GitFolder(FolderName = "Types", UseNodeFolders = false)]
public record OrganizationType : Node
{
    /// <summary>Gets the displayed label of the organization type.</summary>
    public string? Label { get; init; }
}
