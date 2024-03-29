﻿using System.Text.Json.Serialization;

namespace Core.Infrastructure.OAuth.Models
{
    internal class GetTokenErrorResponse
    {
        [JsonPropertyName("error")]
        public required string Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("error_uri")]
        public string? ErrorUri { get; set; }
    }
}
