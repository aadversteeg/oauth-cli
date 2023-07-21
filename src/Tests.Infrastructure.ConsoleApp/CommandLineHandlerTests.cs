using Ave.Extensions.Console.StateManagement;
using Ave.Extensions.Functional;
using Core.Application.Models;
using Core.Infrastructure.ConsoleApp;
using FluentAssertions;
using Moq;
using System.CommandLine.IO;
using System.Threading;
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
            clientServiceMock.Setup(m => m.GetAccessToken(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetTokenSuccess, GetTokenError>.Success(new GetTokenSuccess() { AccessToken = "", TokenType = ""}));
            var stateManagerMock = new Mock<IStateManager>();
            var console = new TestConsole();
            var commandLineHandler = new CommandLineHandler(clientServiceMock.Object, console, stateManagerMock.Object);

            // act
            var result = await commandLineHandler.Invoke(new[] { "client", "get-access-token", "client-name" });

            // assert
            result.Should().Be(0);
            clientServiceMock.Verify((m) => m.GetAccessToken(It.Is<string>(a => a == "client-name"), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
