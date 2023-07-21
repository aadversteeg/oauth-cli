using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System.IO;
using Core.Infrastructure.OAuth;
using Ave.Extensions.Functional.FluentAssertions;
using Core.Application.Models;
using WireMock.Server;
using System;

namespace Tests.Infrastructure.OAuth
{
    public class GetOpenIdConfigurationServiceTests : IClassFixture<WireMockTestsFixture>
    {
        private readonly WireMockTestsFixture _wireMockTestsFixture;

        public GetOpenIdConfigurationServiceTests(WireMockTestsFixture wireMockTestsFixture) {
            _wireMockTestsFixture = wireMockTestsFixture;
        }

        public WireMockServer Server => _wireMockTestsFixture.Server;

        public string GetResponseBody(string responseName)
        {
            var assembly = this.GetType().Assembly;


            string resourceName = $"Tests.Infrastructure.OAuth.GetOpenIdConfigurationResponses.{responseName}.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        [Fact(DisplayName = "GOCS-001 Should return GetTokenSuccess on 200 response")]
        public async Task GOCS001()
        {
            // Given
            var responseBody = GetResponseBody("success");

            Server
                .Given(Request.Create().WithPath("/.well-known/openid-configuration").UsingGet())
                .RespondWith(
                  Response.Create()
                    .WithStatusCode(200)
                    .WithBody(responseBody)
                    .WithHeader("Content-Type", "application/json")
                );

            var service = new GetOpenIdConfigurationService();

            // When
            var response = await service.GetOpenIdConfiguration(new System.Uri($"{Server.Urls[0]}/.well-known/openid-configuration"));

            // Then
            response.Should().Succeed();
            response.Value.Should().BeEquivalentTo(new OpenIdConfiguration(
                "http://tokenprovider", 
                new Uri("http://tokenprovider/token"), 
                new Uri("http://tokenprovider/authorize")));
        }
    }
}