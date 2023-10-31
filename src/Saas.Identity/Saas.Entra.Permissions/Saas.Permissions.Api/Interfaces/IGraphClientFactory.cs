using Microsoft.Graph;

namespace Saas.Permissions.Api.Interfaces;

public interface IGraphApiClientFactory
{
    GraphServiceClient Create();
}
