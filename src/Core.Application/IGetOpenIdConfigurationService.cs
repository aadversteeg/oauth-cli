using Ave.Extensions.Functional;
using Core.Application.Models;

namespace Core.Application
{
    public interface IGetOpenIdConfigurationService
    {
        public Task<Result<OpenIdConfiguration, string>> GetOpenIdConfiguration(Uri wellknownEndpoint);
    }
}
