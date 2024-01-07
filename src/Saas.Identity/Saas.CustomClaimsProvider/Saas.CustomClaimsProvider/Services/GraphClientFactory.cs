//using Microsoft.Extensions.Options;
//using Microsoft.Graph;
//using Saas.Shared.Options;
//using Microsoft.Kiota.Abstractions.Authentication;
//using Saas.CustomClaimsProvider.Interfaces;

//namespace Saas.CustomClaimsProvider.Services;

//public class GraphApiClientFactory(
//    IOptions<MSGraphOptions> msGraphOptions,
//    IAuthenticationProvider authenticationProvider,
//    HttpClient httpClient) : IGraphApiClientFactory
//{
//    private readonly IAuthenticationProvider _authenticationProvider = authenticationProvider;
//    private readonly MSGraphOptions _msGraphOptions = msGraphOptions.Value;
//    private readonly HttpClient _httpClient = httpClient;

//    public GraphServiceClient Create() =>
//                new(_httpClient, _authenticationProvider, _msGraphOptions.BaseUrl);
//}
