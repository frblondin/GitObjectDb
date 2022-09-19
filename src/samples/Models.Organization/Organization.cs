using GitObjectDb;
using System;

namespace Models.Organization;

/// <summary>Represents an application.</summary>
public record Organization : Node
{
    /// <summary>Gets the label of the organization.</summary>
    public string? Label { get; init; }

    /// <summary>Gets the organization type.</summary>
    public OrganizationType? Type { get; init; }

    /// <summary>Gets the GOS identifier.</summary>
    public string? GraphicalOrganizationStructureId { get; init; }

    /// <summary>Gets the timezone of the organization.</summary>
    public TimeZoneInfo? TimeZone { get; init; }

    /// <summary>Gets the status of the organization.</summary>
    public OrganizationStatus Status { get; init; }
}
