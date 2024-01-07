using Microsoft.Identity.Web.UI;
using Saas.SignupAdministration.Web;
using Saas.Application.Web;
using System.Reflection;
using Saas.Shared.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Saas.Identity.Extensions;
using Saas.Identity.Helper;
using Saas.Admin.Client;
using Saas.Shared.Settings;
using Saas.Shared.Options.Entra;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

// Hint: For debugging purposes: https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/wiki/PII
// IdentityModelEventSource.ShowPII = true;

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

builder.Services.Configure<AzureB2CSignupAdminOptions>(
        builder.Configuration.GetRequiredSection(AzureB2CSignupAdminOptions.SectionName));

// Load the email settings 
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(SR.EmailOptionsProperty));

builder.Services.AddRazorPages();

builder.Services.AddMvc();

// Add the workflow object
builder.Services.AddScoped<OnboardingWorkflowService, OnboardingWorkflowService>();

// Add this to allow for context to be shared outside of requests
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add the email object
builder.Services.AddScoped<IEmail, Email>();

// Required for the JsonPersistenceProvider
// Should be replaced based on the persistence scheme
builder.Services.AddMemoryCache();

// TODO: Replace with your implementation of persistence provider
// Session persistence is the default
builder.Services.AddScoped<IPersistenceProvider, JsonSessionPersistenceProvider>();

// Add the user details that come back from B2C
builder.Services.AddScoped<IApplicationUser, ApplicationUser>();

var applicationUri = builder.Configuration.GetRequiredSection(AdminApiOptions.SectionName)
    .Get<AdminApiOptions>()?.ApplicationIdUri
        ?? throw new NullReferenceException($"ApplicationIdUri cannot be null");

var scopes = builder.Configuration.GetRequiredSection(AdminApiOptions.SectionName)
    .Get<AdminApiOptions>()?.Scopes
        ?? throw new NullReferenceException("Scopes cannot be null");

// This is required to be instantiated before the OpenIdConnectOptions starts getting configured.
// By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
// For instance, 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' instead of 'roles' claim.
// This flag ensures that the ClaimsIdentity claims collection will be built from the claims in the token
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// Azure AD B2C requires scope config with a fully qualified url along with an identifier. To make configuring it more manageable and less
// error prone, we store the names of the scopes separately from the application id uri and combine them when neded.
// var fullyQualifiedScopes = scopes.Select(scope => $"{applicationUri}/{scope}".Trim('/')).ToArray();

// Sign-in users with the Microsoft identity platform
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration, EntraSignupAdminOptions.SectionName)
    //.EnableTokenAcquisitionToCallDownstreamApi(fullyQualifiedScopes)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Adding SaaS Authentication and setting web app up for calling the Admin API
//builder.Services.AddSaasWebAppAuthentication(
//    fullyQualifiedScopes,
//    options =>
//    {
//        builder.Configuration.Bind(EntraSignupAdminOptions.SectionName, options);
//    })
//    .SaaSAppCallDownstreamApi()
//    .AddInMemoryTokenCaches();

// Managing the situation where the access token is not in cache.
// For more details please see: https://github.com/AzureAD/microsoft-identity-web/issues/13
//builder.Services.Configure<CookieAuthenticationOptions>(
//    CookieAuthenticationDefaults.AuthenticationScheme,
//    options => options.Events = new RejectSessionCookieWhenAccountNotInCacheEvents(fullyQualifiedScopes));

builder.Services.AddHttpClient<IAdminServiceClient, AdminServiceClient>(httpClient =>
{
    string adminApiBaseUrl = builder.Environment.IsDevelopment()
        ? builder.Configuration.GetRequiredSection("adminApi:baseUrl").Value
            ?? throw new NullReferenceException("Environment is running in development mode. Please specify the value for 'adminApi:baseUrl' in appsettings.json.")
        : builder.Configuration.GetRequiredSection(AzureB2CAdminApiOptions.SectionName)?.Get<AzureB2CAdminApiOptions>()?.BaseUrl
            ?? throw new NullReferenceException($"{nameof(AzureB2CAdminApiOptions)} Url cannot be null");

    httpClient.BaseAddress = new Uri(adminApiBaseUrl);
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// This is required for auth to work correctly when running in a docker container because of SSL Termination
// Remove this and the subsequent app.UseForwardedHeaders() line below if you choose to run the app without using containers
//builder.Services.Configure<ForwardedHeadersOptions>(options =>
//{
//    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
//    options.ForwardedProtoHeaderName = "X-Forwarded-Proto";
//});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
else
{
    app.UseExceptionHandler(SR.ErrorRoute);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
// app.UseForwardedHeaders();
app.UseCookiePolicy(new CookiePolicyOptions
{
    Secure = CookieSecurePolicy.Always
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(name: SR.DefaultName, pattern: SR.MapControllerRoutePattern);

app.MapRazorPages();

AppHttpContext.Services = ((IApplicationBuilder)app).ApplicationServices;

app.Run();