using Microsoft.Graph;

namespace Saas.CustomClaimsProvider.Interfaces;

public interface IGraphApiClientFactory
{
    GraphServiceClient Create();
}
