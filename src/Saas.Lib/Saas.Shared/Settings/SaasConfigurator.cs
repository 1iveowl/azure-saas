using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Saas.Shared.Settings;

/// <summary>
/// Helper class for configuring Azure App Configuration and Azure Key Vault for local development and production environments.
/// </summary>
public static class SaasConfigurator
{
    /// <summary>
    /// Helper method for creating the console logger.
    /// </summary>
    /// <param name="projectName"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public static ILogger CreateConsoleLogger(string? projectName)
    {
        if (projectName is null) throw new NullReferenceException("Project name cannot be null.");

        var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger(projectName);

        logger.LogInformation("001");

        return logger;
    }

    /// <summary>
    /// Helper method for configuring Azure App Configuration and Azure Key Vault for local development and production environments.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="logger"></param>
    /// <param name="version"></param>
    public static void Initialize(
        ConfigurationManager configuration,
        IWebHostEnvironment environment,
        ILogger logger, 
        string version)
    {        
        logger.LogInformation("Version: {version}", version);

        if (environment.IsDevelopment())
        {
            InitializeDevEnvironment(configuration, logger, version);
        }
        else
        {
            InitializeProdEnvironment(configuration, logger, version);
        }        
    }

    private static void InitializeDevEnvironment(
        ConfigurationManager configuration,
        ILogger logger,
        string version)
    {
        logger.LogInformation($"Is Development.");

        // For local development, use the Secret Manager feature of .NET to store a connection string
        // and likewise for storing a secret for the permission-api app. 
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        var appConfigurationconnectionString = configuration.GetConnectionString("AppConfig")
            ?? throw new NullReferenceException("App config missing.");

        // Use the connection string to access Azure App Configuration to get access to app settings stored there.
        // To gain access to Azure Key Vault use 'Azure Cli: az login' to log into Azure.
        // This login on will also now provide valid access tokens to the local development environment.
        // For more details and the option to chain and combine multiple credential options with `ChainedTokenCredential`
        // please see: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet#define-a-custom-authentication-flow-with-chainedtokencredential

        AzureCliCredential credential = new();

        configuration.AddAzureAppConfiguration(options =>
                options.Connect(appConfigurationconnectionString)
                    .ConfigureKeyVault(kv => kv.SetCredential(new ChainedTokenCredential(credential)))
                .Select(KeyFilter.Any, version)); // <-- Important: since we're using labels in our Azure App Configuration store
    }

    private static void InitializeProdEnvironment(
        ConfigurationManager configuration,
        ILogger logger,
        string version)
    {
        logger.LogInformation("Version: {version}", version);
        logger.LogInformation($"Is Production.");

        // Get the Azure App Configuration Endpoint from the environment variable set in the Azure App Service.
        var appConfigurationEndpoint = configuration.GetRequiredSection(Constant.EnvironmentVariableAppConfigurationEndpoint)?.Value
            ?? throw new NullReferenceException("The Azure App Configuration Endpoint cannot be found. Has the endpoint environment variable been set correctly for the App Service?");

        // Get the ClientId of the UserAssignedIdentity
        // If we don't set this ClientID in the ManagedIdentityCredential constructor, it doesn't know it should use the user assigned managed id.
        var managedIdentityClientId = configuration.GetRequiredSection(Constant.EnvironmentVariableUserAssignedManagedIdentityClientId)?.Value
            ?? throw new NullReferenceException($"The Environment Variable '{Constant.EnvironmentVariableUserAssignedManagedIdentityClientId}' cannot be null. Check the App Service Configuration.");

        ManagedIdentityCredential userAssignedManagedCredentials = new(managedIdentityClientId);

        configuration.AddAzureAppConfiguration(options =>
            options.Connect(new Uri(appConfigurationEndpoint), userAssignedManagedCredentials)
            .ConfigureKeyVault(kv => 
            {
                kv.SetCredential(userAssignedManagedCredentials);
            })
            .Select(KeyFilter.Any, version)); ;; // <-- Important since we're using labels in our Azure App Configuration store
    }
}
