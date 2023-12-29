using Azure.Identity;
using Saas.Permissions.Service.Data;
using Saas.Permissions.Service.Interfaces;
using Saas.Shared.Options;
using Saas.Permissions.Service.Services;
using Saas.Permissions.Service.Middleware;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Reflection;
using Saas.Identity.Extensions;
using Saas.Shared.Interface;
using Polly;
using Saas.Permissions.Service.Data.Context;
using Saas.Shared.Settings;

/*  IMPORTANT
    In the configuration pattern used here, we're seeking to minimize the use of appsettings.json, 
    as well as eliminate the need for storing local secrets. 

    Instead we're utilizing the Azure App Configuration service for storing settings and the Azure Key Vault to store secrets.
    Azure App Configuration still hold references to the secret, but not the secret themselves.

    This approach is more secure and allows us to have a single source of truth 
    for all settings and secrets. 

    The settings and secrets were provisioned to Azure App Configuration and Azure Key Vault 
    during the deployment of the Identity Framework.

    For local development, please see the ASDK Permission Service readme.md for more 
    on how to set up and run this service in a local development environment - i.e., a local dev machine. 
*/

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

// IMPORTANT: The current version, must correspond exactly to the version string of our deployment as specificed in the deployment config.json to match the label in Azure App Configuration.
// The version string is set manually for Development environment and is pulled from Azure App Configuration for Production environment.
string version = builder.Environment.EnvironmentName switch
{
    "Development" => "ver0.8.0",
    "Production" => builder.Configuration.GetRequiredSection("Version")?.Value
        ?? throw new NullReferenceException("The Version value cannot be found. Has the 'Version' environment variable been set correctly?"),
    _ => throw new NullReferenceException("The Version value must be set ot either 'Development' or 'Production'. Has the 'Version' environment variable been set correctly?")
};

string appName = Assembly.GetCallingAssembly().GetName().Name
    ?? throw new NullReferenceException("Project name cannot be null.");

// Setting up Logging for console
ILogger consoleLogger = SaasConfigurator.CreateConsoleLogger(appName);

consoleLogger.LogInformation("001");

SaasConfigurator.Initialize(builder.Configuration, builder.Environment, consoleLogger, version);


// Add configuration settings data using Options Pattern.
// For more see: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-7.0
builder.Services.Configure<PermissionsApiOptions>(
        builder.Configuration.GetRequiredSection(PermissionsApiOptions.SectionName));

builder.Services.Configure<AzureB2CPermissionsApiOptions>(
        builder.Configuration.GetRequiredSection(AzureB2CPermissionsApiOptions.SectionName));

builder.Services.Configure<SqlOptions>(
            builder.Configuration.GetRequiredSection(SqlOptions.SectionName));

builder.Services.Configure<MSGraphOptions>(
            builder.Configuration.GetRequiredSection(MSGraphOptions.SectionName));

builder.Services.AddControllers();

// Using Entity Framework for accessing permission data stored in the Permissions Db.
builder.Services.AddDbContext<SaasPermissionsContext>(options =>
{
    var sqlConnectionString = builder.Configuration.GetRequiredSection(SqlOptions.SectionName)
        .Get<SqlOptions>()?.PermissionsSQLConnectionString
            ?? throw new NullReferenceException("SQL Connection string cannot be null.");

    options.UseSqlServer(sqlConnectionString);
});

builder.Services
    .AddSaasApiCertificateClientCredentials<ISaasMicrosoftGraphApi, AzureB2CPermissionsApiOptions>()
    .AddMicrosoftGraphAuthenticationProvider()
    .AddHttpClient<IGraphApiClientFactory, GraphApiClientFactory>()
    .AddTransientHttpErrorPolicy(builder =>
        builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Adding the service used when accessing MS Graph.
builder.Services.AddScoped<IGraphAPIService, GraphAPIService>();

// Adding the permission service used by the API controller
builder.Services.AddScoped<IPermissionsService, PermissionsService>();

builder.Logging.ClearProviders();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}
else
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Configuring the db holding the permissions data.
app.ConfigureDatabase();

// Use Swagger when running in development mode.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.SwaggerEndpoint("/swagger/v1/swagger.json", appName);
    });
}

app.UseHttpsRedirection();
app.UseForwardedHeaders();

if (! app.Environment.IsDevelopment())
{
    // When now in development, add middleware to check for the presaz ence of a valid API Key
    // For debugging purposes, you can comment out 'app.UseMiddleware...'. This way you
    // don't have to add the secret to the header everytime you want to test something in swagger, for instance.
    app.UseMiddleware<ApiKeyMiddleware>();
}

app.MapControllers();

app.Run();