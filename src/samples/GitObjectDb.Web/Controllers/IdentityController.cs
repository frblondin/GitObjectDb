using Fga.Net.AspNetCore.Authorization;
using Fga.Net.AspNetCore.Authorization.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenFga.Sdk.Api;
using OpenFga.Sdk.Model;

namespace GitObjectDb.Web.Controllers;

[Route("identity")]
[Authorize(FgaAuthorizationDefaults.PolicyKey)]
public class IdentityController : ControllerBase
{
    private readonly OpenFgaApi _authorizationClient;

    public IdentityController(OpenFgaApi authorizationClient)
    {
        _authorizationClient = authorizationClient;
    }

    [HttpGet("{id}")]
    [FgaRouteObject("write", "doc", "id")]
    public IActionResult Get()
    {
        return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
    }
}
