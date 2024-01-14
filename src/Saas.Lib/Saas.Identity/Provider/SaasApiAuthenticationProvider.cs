using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Saas.Identity.Interface;
using Saas.Shared.Interface;
using Saas.Shared.Options;
using Saas.Shared.Options.Entra;
using System.Net.Http.Headers;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Saas.Identity.Provider;

public class SaasApiAuthenticationProvider<TProvider, TOptions> : DelegatingHandler
    where TProvider : ISaasApi
    where TOptions : EntraBase
{
    private readonly ILogger _logger;

    // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage?view=aspnetcore-7.0
    private static readonly Action<ILogger, Exception> _logError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(SaasApiAuthenticationProvider<TProvider, TOptions>)),
            "Client Assertion Signing Provider");

    private readonly Lazy<IConfidentialClientApplication> _msalClient;
    private readonly IEnumerable<string>? _scopes;

    public SaasApiAuthenticationProvider(
        IClientAssertionSigningProvider clientAssertionSigningProvider,
        IOptions<TOptions> entraOptions,
        IOptions<SaasApiScopeOptions<TProvider>> scopes,
        IKeyVaultCredentialService credentialService,
        ILogger<SaasApiAuthenticationProvider<TProvider, TOptions>> logger)
    {
        _logger = logger;
        _scopes = scopes.Value.Scopes;

        if (entraOptions.Value.KeyVaultCertificateReferences?.FirstOrDefault() is null)
        {
            logger.LogError("Certificate cannot be null.");
            throw new NullReferenceException("Certificate cannot be null.");
        }

        _msalClient = new Lazy<IConfidentialClientApplication>(() =>
        {
            return ConfidentialClientApplicationBuilder
                .Create(entraOptions.Value.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, entraOptions.Value.TenantId)
                .WithClientAssertion(
                    (options) =>
                        clientAssertionSigningProvider.GetClientAssertion(
                            entraOptions.Value.KeyVaultCertificateReferences.First(),
                            options.TokenEndpoint,
                            options.ClientID,
                            credentialService.GetCredential(),
                            TimeSpan.FromMinutes(10))).Build();
        });

    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            request.Headers.Authorization =
                    new AuthenticationHeaderValue("bearer", await GetAccessTokenAsync());

            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logError(_logger, ex);
            throw;
        }
    }

    internal async Task<string> GetAccessTokenAsync()
    {
        try
        {
            var result = await _msalClient.Value
                .AcquireTokenForClient(_scopes)
                .ExecuteAsync();

            return result.AccessToken;
        }
        catch (Exception ex)
        {
            _logError(_logger, ex);
            throw;
        }
    }
}
