using Saas.Interface;

namespace Saas.Shared.Options.Entra;

public record EntraBase
{
    public string? ClientId { get; init; }
    public string? Audience { get; init; }
    public string? Authority { get; init; }
    public string? Instance { get; init; }
    public string? TenantId { get; init; }
    public string? CallbackPath { get; init; }
    public string? SignedOutCallbackPath { get; init; }

    public CredentialItem[]? ClientCredentials { get; init; }

    public KeyVaultCertificate[]? KeyVaultCertificateReferences { get; init; }
}

public record CredentialItem
{
    public string? SourceType { get; init; }
    public string? ClientSecret { get; init; }
    public string? Certificate { get; init; }
}

public record KeyVaultCertificate : IKeyVaultInfo
{
    public string? SourceType { get; init; }
    public string? KeyVaultUrl { get; init; }
    public string? KeyVaultCertificateName { get; init; }
}