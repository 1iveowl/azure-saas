namespace Saas.CustomClaimsProvider.Models;

public record UnauthorizedResponse
{
    public UnauthorizedResponse(string _error)
    {
        Error = _error;
    }
    public string Error { get; init; }
}
