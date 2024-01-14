using Saas.CustomClaimsProvider.Models;

namespace Saas.CustomClaimsProvider.Interfaces;

public interface IGraphAPIService
{
    public Task<string[]> GetAppRolesAsync(ClaimsRequest request);
    public Task<IEnumerable<User>> GetUsersByIds(ICollection<Guid> userIds);
    public Task<User> GetUserByEmail(string userEmail);

}