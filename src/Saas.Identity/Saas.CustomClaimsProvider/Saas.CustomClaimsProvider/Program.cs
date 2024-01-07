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

using Microsoft.EntityFrameworkCore;
using Saas.CustomClaimsProvider;
using Saas.CustomClaimsProvider.Data;
using Saas.CustomClaimsProvider.Data.Context;
using Saas.CustomClaimsProvider.Interfaces;
using Saas.CustomClaimsProvider.Services;
using Saas.Shared.Options;
using Saas.Shared.Settings;
using Saas.Identity.Extensions;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

SaasConfigurator.Initialize(builder.Configuration, builder.Environment, consoleLogger, version);

// Add configuration settings data using Options Pattern.
// For more see: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-8.0
builder.Services.Configure<PermissionsApiOptions>(
        builder.Configuration.GetRequiredSection(PermissionsApiOptions.SectionName));

builder.Services.Configure<SqlOptions>(
            builder.Configuration.GetRequiredSection(SqlOptions.SectionName));

builder.Services.AddControllers();

// Using Entity Framework for accessing permission data stored in the Permissions Db.
builder.Services.AddDbContext<SaasPermissionsContext>(options =>
{
    var sqlConnectionString = builder.Configuration.GetRequiredSection(SqlOptions.SectionName)
        .Get<SqlOptions>()?.PermissionsSQLConnectionString
            ?? throw new NullReferenceException("SQL Connection string cannot be null.");

    options.UseSqlServer(sqlConnectionString);
});

// Adding the permission service used by the API controller
builder.Services.AddScoped<IPermissionsService, PermissionsService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddSaasEntraCustomClaimsProviderAuthentication(
        Constants.EnvironmentVariableEntraConfigurationSectionName, 
        builder.Configuration);

// builder.Logging.ClearProviders();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();

    // Configuring Swagger.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type=ReferenceType.SecurityScheme,
                        Id="Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}
else
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Configuring the db holding the permissions data.
app.ConfigureDatabase();

// Use Swagger when running in development mode.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.SwaggerEndpoint("/swagger/v1/swagger.json", appName);
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
