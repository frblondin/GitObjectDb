using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GitObjectDb;

/// <summary>Provides methods to know user level authorization on GitObjectDb items.</summary>
public interface IAuthorizationProvider
{
    /// <summary>
    /// Gets a list of permissions that a user has for a given <paramref name="node"/>.
    /// </summary>
    /// <param name="node">The item for which permissions should be checked.</param>
    /// <param name="claims">The user claims.</param>
    /// <returns>A collection of permissions.</returns>
    Task<IEnumerable<string>> GetPermissionsAsync(Node node, IEnumerable<Claim> claims);

    /// <summary>
    /// Gets whether or not a user is allowed to perform an action for a given <paramref name="node"/>.
    /// </summary>
    /// <param name="node">The item for which permissions should be checked.</param>
    /// <param name="claims">The user claims.</param>
    /// <param name="action">The action that the user wants to perform.</param>
    /// <returns><c>true</c> if the action is authorized, <c>false</c> otherwise.</returns>
    Task<bool> IsAuthorizedAsync(Node node, IEnumerable<Claim> claims, string action);
}
