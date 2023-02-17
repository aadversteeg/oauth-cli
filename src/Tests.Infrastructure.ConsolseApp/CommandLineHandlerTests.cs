using Core.Infrastructure.ConsoleApp;
using FluentAssertions;
using Moq;
using System.CommandLine;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Infrastructure.ConsoleApp
{
    public class CommandLineHandlerTests
    {

        [Fact(DisplayName = "CMLH-001: Client sub command get-access-token on the command line should get the access token for the specified client.")]
        public async Task CMLH001()
        {
            // arrange
            var clientServiceMock = new Mock<IClientService>();
            var consoleMock = new Mock<IConsole>();
            var commandLineHandler = new CommandLineHandler(clientServiceMock.Object, consoleMock.Object);

            // act
            var result = await commandLineHandler.Invoke(new[] { "client", "get-access-token", "client-name" });

            // assert
            result.Should().Be(0);
            clientServiceMock.Verify((m) => m.GetAccessToken(It.Is<string>(a => a == "client-name")), Times.Once());
        }
    }
}
