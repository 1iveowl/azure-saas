using Saas.Identity.Authorization.Model.Data;

namespace Saas.CustomClaimsProvider.Interfaces;

public interface IPermissionsService
{
    Task<ICollection<SaasPermission>> GetPermissionsAsync(Guid userId);
}
