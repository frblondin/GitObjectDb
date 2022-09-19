using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Organization;

/// <summary>Gets the status of an organization.</summary>
public enum OrganizationStatus
{
    /// <summary>The organization has not been activated yet.</summary>
    PreCommissioning,

    /// <summary>The organization is active.</summary>
    Commissioned,

    /// <summary>The organization is no longer active.</summary>
    Decommissioned,
}
