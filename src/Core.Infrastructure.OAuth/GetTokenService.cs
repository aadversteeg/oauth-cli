using Ave.Extensions.Functional;
using Core.Application;
using Core.Application.Models;
using Core.Infrastructure.ConsoleApp.Models;
using System.Text;
using System.Text.Json;

namespace Core.Infrastructure.OAuth
{
    public class GetTokenService: IGetTokenService
    {
        public async Task<Result<Application.Models.GetTokenResult, string>> GetToken(Uri tokenEndpoint, IReadOnlyDictionary<string,string> headers, IReadOnlyDictionary<string, string> content)
        {
            using var client = new HttpClient();

            var body = BodyFormatter.Format(content);

            var request = new HttpRequestMessage()
            {
                RequestUri = tokenEndpoint,
                Method = HttpMethod.Get,
                Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await client.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode == true)
            {
                var getTokenSuccessResponse = JsonSerializer.Deserialize<GetTokenSuccessResponse>(responseContent);
                return Result<Application.Models.GetTokenResult, string>.Success(
                    GetTokenResult.ToSuccess(
                        new Application.Models.GetTokenSuccess()
                        {
                            AccessToken = getTokenSuccessResponse.AccessToken,
                            TokenType = getTokenSuccessResponse.TokenType,
                            ExpiresIn = getTokenSuccessResponse.ExpiresIn,
                            RefreshToken = getTokenSuccessResponse.RefreshToken,
                            Scope = getTokenSuccessResponse.Scope
                        }));
            }

            var getTokenErrorResponse = JsonSerializer.Deserialize<GetTokenErrorResponse>(responseContent);
            return Result<Application.Models.GetTokenResult, string>.Success(
                GetTokenResult.ToError(
                    new Application.Models.GetTokenError()
                    {
                        Error = getTokenErrorResponse.Error,
                        ErrorDescription = getTokenErrorResponse.ErrorDescription,
                        ErrorUri = getTokenErrorResponse.ErrorUri
                    }));
        }
    }
}