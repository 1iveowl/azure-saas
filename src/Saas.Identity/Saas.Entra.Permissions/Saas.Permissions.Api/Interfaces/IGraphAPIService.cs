using Saas.Permissions.Api.Models;

namespace Saas.Permissions.Api.Interfaces;

public interface IGraphAPIService
{
    public Task<string[]> GetAppRolesAsync(ClaimsRequest request);
    public Task<IEnumerable<User>> GetUsersByIds(ICollection<Guid> userIds);
    public Task<User> GetUserByEmail(string userEmail);

}