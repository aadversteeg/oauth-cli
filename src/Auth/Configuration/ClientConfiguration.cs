#pragma warning disable CS8618 

using Auth.Models;
using System.Collections.Generic;

namespace Auth.Configuration
{
    public class ClientConfiguration
    {
        public string WellknownEndpoint { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public bool UsePKCE { get; set; }

        public string? RedirectUri { get; set; }

        public GrantType GrantType { get; set; }

        public string ApplicationIdUri { get; set; }

        public string[] Scopes { get; set; }

        public IDictionary<string, string> AuthorizationParameters { get; set; }
    }
}
