using System.Text.Json.Serialization;

namespace Core.Infrastructure.OAuth.Models
{
    internal class GetOpenIdConfigurationSuccessResponse
    {
        [JsonPropertyName("issuer")]
        public required string Issuer { get; set; }

        [JsonPropertyName("token_endpoint")]
        public required string TokenEndpoint { get; set; }

        [JsonPropertyName("authorization_endpoint")]
        public required string AuthorizationEndpoint { get; set; }
    }
}
