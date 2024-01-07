namespace Saas.CustomClaimsProvider.Models;

public record PermissionsClaimResponse
{
    public string[]? Permissions { get; init; }
}

