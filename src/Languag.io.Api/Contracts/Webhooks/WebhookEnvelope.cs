using System.Text.Json.Serialization;

namespace Languag.io.Api.Contracts.Webhooks;
public sealed class WebhookEnvelope
{
    [JsonPropertyName("data")]
    public WebhookEvent Data { get; init; } = default!;
}

public sealed class WebhookEvent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!; // "user.created"

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("data")]
    public UserData Payload { get; init; } = default!;

    [JsonPropertyName("event_attributes")]
    public EventAttributes? EventAttributes { get; init; }
}

public sealed class UserData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("email_addresses")]
    public List<EmailAddress> EmailAddresses { get; init; } = new();

    [JsonPropertyName("primary_email_address_id")]
    public string? PrimaryEmailAddressId { get; init; }
}

public sealed class EmailAddress
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("email_address")]
    public string Email { get; init; } = default!;
}

public sealed class EventAttributes
{
    [JsonPropertyName("http_request")]
    public HttpRequestAttributes? HttpRequest { get; init; }
}

public sealed class HttpRequestAttributes
{
    [JsonPropertyName("client_ip")]
    public string? ClientIp { get; init; }

    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; init; }
}