using System;
using System.CommandLine;
using System.Threading.Tasks;

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

        public async Task<int> Invoke(string[] args)
        {
            var rootCommand = new RootCommand("Command Line Interface for retrieving OAuth access tokens.");

            var clientNameArgument = new Argument<string>("client-name", "Name of client to get access token from.");

            var clientCommand = new Command("client", "Manage clients.");
        
            var getAccessTokenCommand = new Command("get-access-token", "Get the access token for the specified client.");
            getAccessTokenCommand.AddAlias("gat");
            getAccessTokenCommand.Add(clientNameArgument);

            getAccessTokenCommand.SetHandler(async context =>
            {
                var clientNameArgumentValue = context.ParseResult.GetValueForArgument(clientNameArgument);
                var accessToken = await _clientService.GetAccessToken(clientNameArgumentValue);

                Console.WriteLine($"Received token: {accessToken}");

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine("Press any key...");
                    Console.ReadLine();
                }
            });

            clientCommand.AddCommand(getAccessTokenCommand);

            var listCommand = new Command("list", "Get a list of all clients.");

            listCommand.SetHandler(async context =>
            {
                var clients = await _clientService.GetClients();
                foreach ( var client in clients) {
                    _console.WriteLine(client);
                }
            });
            clientCommand.AddCommand(listCommand);

            rootCommand.Add(clientCommand);

            var invokeResult = await  rootCommand.InvokeAsync(args, _console);
            return invokeResult;
        }
    }
}
