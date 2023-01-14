using System.Text.Json.Serialization;

namespace Auth.Models
{
    public class OpenIdConfiguration
    {
        [JsonPropertyName("token_endpoint")]
        public string? TokenEndpoint { get; set; }

        [JsonPropertyName("authorization_endpoint")]
        public string? AuthorizeEndpoint { get; set; }

        [JsonPropertyName("userinfo_endpoint")]
        public string? UserInfoEndpoint { get; set; }

        [JsonPropertyName("end_session_endpoint")]
        public string? EndSessionEndpoint { get; set; }
    }
}
