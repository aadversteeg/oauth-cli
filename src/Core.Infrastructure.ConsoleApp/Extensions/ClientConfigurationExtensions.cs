using Core.Infrastructure.ConsoleApp.Configuration;

namespace Core.Infrastructure.ConsoleApp.Extensions
{
    public static class ClientConfigurationExtensions
    {
        public static bool UsesClientAssertion(this ClientConfiguration clientConfiguration)
        {
            return !string.IsNullOrEmpty(clientConfiguration.ClientSecret ) || !string.IsNullOrEmpty (clientConfiguration.ClientCertificateName);
        }

        public static bool IsSpa(this ClientConfiguration clientConfiguration)
        {
            return !clientConfiguration.UsesClientAssertion() && clientConfiguration.GrantType == GrantType.AuthorizationCode;
        }
    }
}
