using Microsoft.Extensions.Options;
using Saas.Admin.Service.Data;
using Saas.Identity.Authorization.Handler;
using Saas.Identity.Authorization.Option;
using Saas.Identity.Authorization.Provider;
using Saas.Permissions.Client;
using Saas.Shared.Options;
using Saas.Shared.Settings;

/*  IMPORTANT
    In the configuration pattern used here, we're seeking to minimize the use of appsettings.json, 
    as well as eliminate the need for storing local secrets. 

    Instead we're utilizing the Azure App Configuration service for storing settings and the Azure Key Vault to store secrets.
    Azure App Configuration still hold references to the secret, but not the secret themselves.

    This approach is more secure and allows us to have a single source of truth 
    for all settings and secrets. 

    The settings and secrets are provisioned by the deployment script made available for deploying this service.
    Please see the readme for the project for details.

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

SaasConfigurator.Initialize(builder.Configuration, builder.Environment, consoleLogger, version);

// Configure Options for DI
builder.Services.Configure<AzureB2CAdminApiOptions>(
        builder.Configuration.GetRequiredSection(AzureB2CAdminApiOptions.SectionName));

builder.Services.Configure<AzureB2CPermissionsApiOptions>(
        builder.Configuration.GetRequiredSection(AzureB2CPermissionsApiOptions.SectionName));

builder.Services.Configure<PermissionsApiOptions>(
        builder.Configuration.GetRequiredSection(PermissionsApiOptions.SectionName));

builder.Services.Configure<SqlOptions>(
            builder.Configuration.GetRequiredSection(SqlOptions.SectionName));

builder.Services.Configure<SaasAuthorizationOptions>(
    builder.Configuration.GetRequiredSection(SaasAuthorizationOptions.SectionName));

// Configure HttpContext for DI
builder.Services.AddHttpContextAccessor();

// Add authentication for incoming requests
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, AzureB2CAdminApiOptions.SectionName);

// Register authorization handlers for authorization
builder.Services.AddSingleton<IAuthorizationHandler, SaasTenantPermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, SaasUserPermissionAuthorizationHandler>();

// Register the policy provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, SaasPermissionAuthorizationPolicyProvider>();

// Register the tenant service
builder.Services.AddScoped<ITenantService, TenantService>();

builder.Services.AddControllers();

// Register the permissions service client
builder.Services.AddHttpClient<IPermissionsServiceClient, PermissionsServiceClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        using var scope = serviceProvider.CreateScope();

        var baseUrl = scope.ServiceProvider.GetRequiredService<IOptions<AzureB2CPermissionsApiOptions>>().Value.BaseUrl
            ?? throw new NullReferenceException("Permissions Base Url cannot be null");

        var apiKey = scope.ServiceProvider.GetRequiredService<IOptions<PermissionsApiOptions>>().Value.ApiKey
            ?? throw new NullReferenceException("Permissions Base Api Key cannot be null");

        client.BaseAddress = new Uri(baseUrl);

        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
    });

// Using Entity Framework for accessing permission data stored in the Permissions Db.
builder.Services.AddDbContext<TenantsContext>(options =>
{
    var sqlConnectionString = builder.Configuration.GetRequiredSection(SqlOptions.SectionName)
        .Get<SqlOptions>()?.TenantSQLConnectionString
            ?? throw new NullReferenceException("SQL Connection string cannot be null.");

    options.UseSqlServer(sqlConnectionString);
});

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
    
    // Configuring Swagger.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    // Enabling to option for add the 'x-api-key' header to swagger UI.
    builder.Services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new() { Title = appName, Version = "v1.3" });
        // option.OperationFilter<SwagCustomHeaderFilter>();
    });
}

var app = builder.Build();

//Call this as early as possible to make sure DB is ready
//In a larger project it's better update the database during deployment process
app.ConfigureDatabase();

// Setting up Swagger for development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();