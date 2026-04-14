using System.Text.Json;
using System.Text.Json.Serialization;

namespace Languag.io.Api.Contracts.Webhooks;

public sealed class WebhookEnvelope
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("event_id")]
    public string? EventId { get; init; }

    [JsonPropertyName("event_timestamp")]
    public DateTimeOffset? EventTimestamp { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("data")]
    public WebhookData? Data { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed class WebhookData
{
    [JsonPropertyName("user")]
    public WebhookUser? User { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed class WebhookUser
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("is_password_reset_requested")]
    public bool? IsPasswordResetRequested { get; init; }

    [JsonPropertyName("is_suspended")]
    public bool? IsSuspended { get; init; }

    [JsonPropertyName("organizations")]
    public List<WebhookOrganization> Organizations { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed class WebhookOrganization
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("permissions")]
    public JsonElement? Permissions { get; init; }

    [JsonPropertyName("roles")]
    public JsonElement? Roles { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
