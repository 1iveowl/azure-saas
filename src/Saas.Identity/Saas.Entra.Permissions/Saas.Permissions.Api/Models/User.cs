namespace Saas.Permissions.Api.Models;

public record User
{
    public string? UserId { get; init; }
    public string? DisplayName { get; init; }  
}
