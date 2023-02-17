using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Threading.Tasks;
using Core.Infrastructure.ConsoleApp.Extensions;

namespace Core.Infrastructure.ConsoleApp
{
    public class CommandLineHandler
    {
        private readonly IClientService _clientService;
        private readonly IConsole _console;

        public CommandLineHandler(IClientService clientService, IConsole console)
        {
            _clientService = clientService;
            _console = console;
        }

        public void HandleCancellation(InvocationContext context)
        {
            context.Console.Error.WriteLine("The operation was aborted.");
            context.ExitCode = 1;
        }

        public async Task<int> Invoke(string[] args)
        {
            var rootCommand = new RootCommand("Command Line Interface for retrieving OAuth access tokens.");

            var clientNameArgument = new Argument<string>("client-name", "Name of client to get access token from.");

            var clientCommand = new Command("client", "Manage clients.");
        
            var getAccessTokenCommand = new Command("get-access-token", "Get the access token for the specified client.");
            getAccessTokenCommand.AddAlias("gat");
            getAccessTokenCommand.Add(clientNameArgument);

            getAccessTokenCommand.SetHandler(async (context, cancellationToken) =>
                {
                    var clientNameArgumentValue = context.ParseResult.GetValueForArgument(clientNameArgument);
                    var accessToken = await _clientService.GetAccessToken(clientNameArgumentValue, cancellationToken);
                    context.Console.WriteLine($"Received token: {accessToken}");
                }, 
                HandleCancellation);

            clientCommand.AddCommand(getAccessTokenCommand);

            var listCommand = new Command("list", "Get a list of all clients.");

            listCommand.SetHandler( async (context, cancellationToken) =>
                {
                        var clients = await _clientService.GetClients(cancellationToken);
                        foreach (var client in clients)
                        {
                            _console.WriteLine(client);
                        }
                }, 
                HandleCancellation);
            clientCommand.AddCommand(listCommand);
            rootCommand.AddCommand(clientCommand);

            var invokeResult = await rootCommand.InvokeAsync(args, _console);
            return invokeResult;
        }
    }
}
