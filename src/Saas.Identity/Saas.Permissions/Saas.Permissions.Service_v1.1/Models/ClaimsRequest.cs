﻿using System.Text.Json.Serialization;

namespace Saas.Permissions.Api.Models;

public record ClaimsRequest
{
    [JsonPropertyName("signInNames.emailAddress")]
    public string? EmailAddress { get; init; }
    
    public Guid ObjectId { get; init; }
    
    public string? ClientId { get; init; }
}
