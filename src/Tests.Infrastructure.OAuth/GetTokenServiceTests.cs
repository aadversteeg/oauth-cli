using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System.IO;
using Core.Infrastructure.OAuth;
using System.Collections.Generic;
using Ave.Extensions.Functional.FluentAssertions;
using Core.Application.Models;

namespace Tests.Infrastructure.OAuth
{
    public class GetTokenServiceTests
    {
        public string GetResponseBody(string responseName)
        {
            var assembly = this.GetType().Assembly;


            string resourceName = $"Tests.Infrastructure.OAuth.GetTokenResponses.{responseName}.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        [Fact(DisplayName = "GTS-001 Should return GetTokenSuccess on 200 response")]
        public async Task GTS001()
        {
            // Given
            var server = WireMockServer.Start();

            var responseBody = GetResponseBody("success");

            server
                .Given(Request.Create().WithPath("/token").UsingGet())
                .RespondWith(
                  Response.Create()
                    .WithStatusCode(200)
                    .WithBody(responseBody)
                    .WithHeader("Content-Type", "application/json")
                );

            var getTokenService = new GetTokenService();

            // When
            var response = await getTokenService.GetToken(new System.Uri($"{server.Urls[0]}/token"), new Dictionary<string, string>(), new Dictionary<string, string>());

            // Then
            response.Should().Succeed();
            response.Value.Should().BeEquivalentTo(new GetTokenSuccess()
            {
                AccessToken = "7779HHKHKHKK6683TY3",
                TokenType = "bearer",
                ExpiresIn = 3600,
                RefreshToken = "703JH3YU89YH389T3878T38",
                Scope = "profile.read"
            });

            server.Stop();
        }

        [Fact(DisplayName = "GTS-002 Should return GetTokenError on 400 response")]
        public async Task GTS002()
        {
            // Given
            var server = WireMockServer.Start();

            var responseBody = GetResponseBody("failure");

            server
                .Given(Request.Create().WithPath("/token").UsingGet())
                .RespondWith(
                  Response.Create()
                    .WithStatusCode(400)
                    .WithBody(responseBody)
                    .WithHeader("Content-Type", "application/json")
                );

            var getTokenService = new GetTokenService();

            // When
            var response = await getTokenService.GetToken(new System.Uri($"{server.Urls[0]}/token"), new Dictionary<string, string>(), new Dictionary<string, string>());

            // Then
            response.Should().Fail();
            response.Error.Should().BeEquivalentTo(
                new GetTokenError()
                {
                    Error = "invalid_request"
                });

            server.Stop();
        }
    }
}