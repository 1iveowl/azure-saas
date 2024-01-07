using System.Text.Json.Serialization;

namespace Saas.CustomClaimsProvider.Models;

// https://learn.microsoft.com/en-us/entra/identity-platform/custom-claims-provider-reference
public class CustomerExtensionResponse
{
    [JsonPropertyName("data")]
    public ResponseData Data { get; set; }

    public CustomerExtensionResponse()
    {
        Data = new ResponseData();
    }
}

public class ResponseData
{
    [JsonPropertyName("@odata.type")]
    public string ODatatype { get; set; }

    [JsonPropertyName("actions")]
    public List<Action> Actions { get; set; }

    public ResponseData()
    {
        ODatatype = "microsoft.graph.onTokenIssuanceStartResponseData";
        Actions = [new Action()];
    }
}

public class Action
{
    [JsonPropertyName("@odata.type")]
    public string ODatatype { get; set; }

    [JsonPropertyName("claims")]
    public Claims Claims { get; set; }

    public Action()
    {
        ODatatype = "microsoft.graph.tokenIssuanceStart.provideClaimsForToken";
        Claims = new Claims();
    }
}

public class Claims
{
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("apiVersion")]
    public string? ApiVersion { get; set; }

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; }

    public Claims() => Permissions = [];
}
