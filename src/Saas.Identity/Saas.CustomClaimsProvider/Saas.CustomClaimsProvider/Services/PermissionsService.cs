using Microsoft.EntityFrameworkCore;
using Saas.CustomClaimsProvider.Data.Context;
using Saas.Identity.Authorization.Model.Data;
using Saas.CustomClaimsProvider.Interfaces;

namespace Saas.CustomClaimsProvider.Services;

public class PermissionsService(
    SaasPermissionsContext permissionsContext,
    ILogger<PermissionsService> logger) : IPermissionsService
{
    private readonly SaasPermissionsContext _permissionsContext = permissionsContext;
    private readonly ILogger _logger = logger;

    public async Task<ICollection<SaasPermission>> GetPermissionsAsync(Guid userId)
    {
        _logger.LogDebug("User {userId} tried to get permissions", userId);

        return await _permissionsContext.SaasPermissions
            .Include(x => x.UserPermissions)
            .Include(x => x.TenantPermissions)
            .Where(x => x.UserId == userId)
            .Select(x => x.IncludeAllPermissionSets())
            .ToListAsync();
    }
}
