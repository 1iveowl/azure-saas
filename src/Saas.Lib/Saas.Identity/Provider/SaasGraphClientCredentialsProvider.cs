﻿
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Saas.Shared.Interface;
using Saas.Shared.Options.Entra;

namespace Saas.Identity.Provider;
public class SaasGraphClientCredentialsProvider<TOptions>(
    SaasApiAuthenticationProvider<ISaasMicrosoftGraphApi, TOptions> authProvider,
    ILogger<SaasGraphClientCredentialsProvider<TOptions>> logger) : IAuthenticationProvider
    where TOptions : EntraBase
{
    private readonly ILogger _logger = logger;
    private readonly SaasApiAuthenticationProvider<ISaasMicrosoftGraphApi, TOptions> _authProvider = authProvider;

    // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage?view=aspnetcore-8.0
    private static readonly Action<ILogger, Exception> _logError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(SaasGraphClientCredentialsProvider<TOptions>)),
            "Client Assertion Signing Provider");

    public async Task AuthenticateRequestAsync(
        RequestInformation request, 
        Dictionary<string, object>? additionalAuthenticationContext = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            request.Headers.Add("Authorization", $"bearer { await _authProvider.GetAccessTokenAsync() }");
        }
        catch (Exception ex)
        {
            _logError(_logger, ex);
            throw;
        }
    }
}
