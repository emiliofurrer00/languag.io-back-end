using Languag.io.Api.Contracts.Webhooks;

namespace Languag.io.Api.Webhooks;

public interface IKindeWebhookDecoder
{
    Task<KindeWebhookDecodeResult> DecodeAsync(string rawJwt, CancellationToken ct = default);
}

public sealed record KindeWebhookDecodeResult(
    bool IsValid,
    WebhookEnvelope? Payload,
    string? Error);
