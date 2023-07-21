using Core.Application.Models;
using Ave.Extensions.Functional;
using System.Text.Json;
using Core.Infrastructure.OAuth.Models;
using Core.Application;

namespace Core.Infrastructure.OAuth
{
    public class GetOpenIdConfigurationService : IGetOpenIdConfigurationService
    {
        public async Task<Result<OpenIdConfiguration, string>> GetOpenIdConfiguration(Uri wellknownEndpoint)
        {
            using var client = new HttpClient();

            var request = new HttpRequestMessage()
            {
                RequestUri = wellknownEndpoint,
                Method = HttpMethod.Get
            };

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var successResponse = JsonSerializer.Deserialize<GetOpenIdConfigurationSuccessResponse>(responseContent);
                return Result<OpenIdConfiguration, string>.Success(
                    new OpenIdConfiguration(
                        successResponse.Issuer,
                        new Uri(successResponse.TokenEndpoint),
                        new Uri(successResponse.AuthorizationEndpoint)));
            }

            return Result<OpenIdConfiguration, string>.Failure(response.StatusCode.ToString());
        }
    }
}
