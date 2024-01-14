using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Saas.CustomClaimsProvider.Interfaces;
using Saas.CustomClaimsProvider.Models;
using System.Diagnostics;

namespace Saas.CustomClaimsProvider.Controllers;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CustomClaimsController(
    IPermissionsService permissionsService,
    ILogger<CustomClaimsController> logger) : ControllerBase
{
    private readonly IPermissionsService _permissionsService = permissionsService;
    private readonly ILogger _logger = logger;

    // This is the endpoint that is called by Azure AD B2C to get alle the custom claims defined for a specific user.
    [HttpPost("permissions")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PermissionsClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Permissions(CustomerExtensionRequest request)
    {
        _logger.LogDebug("Custom claims where requested for user id: {objectId}", request.Data?.AuthenticationContext?.User?.Id);

        // Get all the permissions defined for the specific user with requested objectId from the database.
        var permissions = await _permissionsService.GetPermissionsAsync(Guid.Parse(request.Data?.AuthenticationContext?.User?.Id));

        IEnumerable<string> permissionClaims = new List<string>();

        foreach (var permission in permissions)
        {
            // adding user permission to permissionsClaims list
            if (permission.UserPermissions?.Count > 0)
            {
                permissionClaims = permissionClaims
                    .Concat(permissions.SelectMany(permission => permission.UserPermissions)
                        .Select(user => user.ToClaim()));
            }

            // adding tenant permissions to permissionsClaims list
            if (permission.UserPermissions?.Count > 0)
            {
                permissionClaims = permissionClaims
                    .Concat(permissions.SelectMany(permission => permission.TenantPermissions)
                        .Select(tenant => tenant.ToClaim()));
            }
        }

        PermissionsClaimResponse permissionResponse = new()
        {
            Permissions = permissionClaims.ToArray()
        };


        CustomerExtensionResponse response = new()
        {
            Data = new()
            {
                Actions = [
                    new()
                    {
                        Claims = new()
                        {
                            // Read the correlation ID from the Azure AD request    
                            CorrelationId = request?.Data?.AuthenticationContext?.CorrelationId,
                            ApiVersion = "1.0.0",
                            Permissions = ["TestPermission1", "TestPermission2"] // permissionClaims.ToList()
                        }
                    }
                ]
            }
        };


        OkObjectResult result = new(response);

        Debug.WriteLine($"CorrelationId :{response.Data.Actions[0].Claims.CorrelationId}, " +
            $"Api Version: {response.Data.Actions[0].Claims.ApiVersion}, " +
            $"Permissios: {string.Join(" - ", response.Data.Actions[0].Claims.Permissions)}");

        return result;
    }

    //[HttpPost("roles")]
    //[Produces("application/json")]
    //[ProducesResponseType(typeof(RolesClaimResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> Roles(ClaimsRequest request)
    //{
    //    // This request is currently retuning an empty list only.
    //    // The MS Graph call is expensive and we don't need it for now.
    //    // Also having a MS Graph call in the login flow is not ideal, as high volume of logins may hit MS Graph throttloing limits.
    //    // var roles = await _graphAPIService.GetAppRolesAsync(request);

    //    RolesClaimResponse response = new()
    //    {
    //        Roles = []
    //    };

    //    await Task.CompletedTask;

    //    return Ok(response);
    //}
}