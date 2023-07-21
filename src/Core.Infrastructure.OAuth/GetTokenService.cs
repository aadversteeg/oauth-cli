using Ave.Extensions.Functional;
using Core.Application;
using Core.Application.Models;
using Core.Infrastructure.OAuth.Models;
using System.Text;
using System.Text.Json;

namespace Core.Infrastructure.OAuth
{
    public class GetTokenService: IGetTokenService
    {
        public async Task<Result<GetTokenResult, string>> GetToken(Uri tokenEndpoint, IReadOnlyDictionary<string,string> headers, IReadOnlyDictionary<string, string> content)
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
                return Result<GetTokenResult, string>.Success(
                    GetTokenResult.Create(
                        new GetTokenSuccess()
                        {
                            AccessToken = getTokenSuccessResponse.AccessToken,
                            TokenType = getTokenSuccessResponse.TokenType,
                            ExpiresIn = getTokenSuccessResponse.ExpiresIn,
                            RefreshToken = getTokenSuccessResponse.RefreshToken,
                            Scope = getTokenSuccessResponse.Scope
                        }));
            }

            var getTokenErrorResponse = JsonSerializer.Deserialize<GetTokenErrorResponse>(responseContent);
            return Result<GetTokenResult, string>.Success(
                GetTokenResult.Create(
                    new GetTokenError()
                    {
                        Error = getTokenErrorResponse.Error,
                        ErrorDescription = getTokenErrorResponse.ErrorDescription,
                        ErrorUri = getTokenErrorResponse.ErrorUri
                    }));
        }
    }
}