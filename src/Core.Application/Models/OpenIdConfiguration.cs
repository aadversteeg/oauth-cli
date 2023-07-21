namespace Core.Application.Models
{
    public class OpenIdConfiguration
    {
        public OpenIdConfiguration(string issuer, Uri tokenEndpoint, Uri authorizationEndpoint)
        {
            Issuer = issuer;
            TokenEndpoint = tokenEndpoint;
            AuthorizationEndpoint = authorizationEndpoint;
        }

        public string Issuer { get; set; }

        public Uri TokenEndpoint { get; }

        public Uri AuthorizationEndpoint { get;}
    }
}
