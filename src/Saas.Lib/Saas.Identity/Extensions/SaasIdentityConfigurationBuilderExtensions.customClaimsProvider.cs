
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Saas.Identity.Extensions;
public static partial class SaasIdentityConfigurationBuilderExtensions
{
    // For details see: https://learn.microsoft.com/en-us/entra/identity-platform/custom-extension-overview#protect-your-rest-api
    private const string EntraIdP = "99045fe1-7639-4a75-9d4a-577b6ca3810f";

    public static void AddSaasEntraCustomClaimsProviderAuthentication(this AuthenticationBuilder authenticationBuilder,
        string configSectionName,
        ConfigurationManager configuration)
    {
        authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                configuration.Bind(configSectionName, options);

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal?.Claims is null)
                        {
                            context.Fail("Token Validation Has Failed. No claims found in token.");
                            return;
                        }

                        if (!TryGetAuthorizedPartyValue(context.Principal.Claims, out var authorizedParty))
                        {
                            context.Fail("Token Validation Has Failed. No authorized party specified in claims.");
                            return;
                        }

                        if (EntraIdP != authorizedParty)
                        {
                            context.Fail("Token Validation Has Failed. Unauthorized party specified in claims.");
                            return;
                        }

                        context.Success();

                        await Task.CompletedTask;
                    }
                };

            static bool TryGetAuthorizedPartyValue(IEnumerable<Claim> claims, out string? authorizedParty)
            {
                string? version = claims.FirstOrDefault(context => context.Type.Equals("ver", StringComparison.OrdinalIgnoreCase))?.Value;

                authorizedParty = version switch
                {
                    "1.0" => claims.FirstOrDefault(context => context.Type.Equals("appid", StringComparison.OrdinalIgnoreCase))?.Value,
                    "2.0" => claims.FirstOrDefault(context => context.Type.Equals("azp", StringComparison.OrdinalIgnoreCase))?.Value,
                    _ => null
                };

                // Authorized party must be a valid GUID.
                return Guid.TryParse(authorizedParty, out _);
            }
        });
    }
}
