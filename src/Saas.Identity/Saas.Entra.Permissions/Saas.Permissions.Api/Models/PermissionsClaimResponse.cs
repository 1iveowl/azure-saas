namespace Saas.Permissions.Api.Models;


public record PermissionsClaimResponse
{
    public string[]? Permissions { get; init; }
}

