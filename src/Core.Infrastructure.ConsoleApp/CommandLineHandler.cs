using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Ave.Extensions.Console.StateManagement;
using Core.Infrastructure.ConsoleApp.Extensions;

namespace Core.Infrastructure.ConsoleApp
{
    public class CommandLineHandler
    {
        private readonly IClientService _clientService;
        private readonly IConsole _console;
        private readonly IStateManager _stateManager;

        public CommandLineHandler(IClientService clientService, IConsole console, IStateManager stateManager)
        {
            _clientService = clientService;
            _console = console;
            _stateManager = stateManager;
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
                    var getAccessTokenResult = await _clientService.GetAccessToken(clientNameArgumentValue, cancellationToken);

                    var jsonSerializerOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                    };

                    var getAccessTokenResultAsString = JsonSerializer.Serialize((object) (getAccessTokenResult.IsSuccess ? getAccessTokenResult.Value : getAccessTokenResult.Error), jsonSerializerOptions);
                    context.Console.WriteLine($"Received token:");
                    context.Console.WriteLine(getAccessTokenResultAsString);

                    if (getAccessTokenResult.IsSuccess)
                    {
                        var ps = new ProcessStartInfo($"https://jwt.ms/#id_token={getAccessTokenResult.Value.AccessToken}")
                        {
                            UseShellExecute = true,
                            Verb = "open"
                        };
                        Process.Start(ps);
                    }

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


            
            var stateCommand = new Command("state", "Manage application state.");


            var stateNameArgument = new Argument<string>("name", "Name of state variable.");

            var stateSetCommand = new Command("set", "Set state value");
            var stateValueArgument = new Argument<string>("Value", "Value of state variable.");
            stateSetCommand.Add(stateNameArgument);
            stateSetCommand.Add(stateValueArgument);

            stateSetCommand.SetHandler( (context) =>
            {
                var stateNameArgumentValue = context.ParseResult.GetValueForArgument(stateNameArgument);
                var stateValueArgumentValue = context.ParseResult.GetValueForArgument(stateValueArgument);
                _stateManager.SetValue(StateScope.Session, stateNameArgumentValue, stateValueArgumentValue);
               ((StateManager) _stateManager).Save();

                _console.WriteLine($"{stateNameArgumentValue}={stateValueArgumentValue}");
            });
            stateCommand.Add(stateSetCommand);


            var stateGetCommand = new Command("get", "Get state value");
            stateGetCommand.Add(stateNameArgument);

            stateGetCommand.SetHandler((context) =>
            {
                var stateNameArgumentValue = context.ParseResult.GetValueForArgument(stateNameArgument);

                var value = _stateManager.GetValue<string>(StateScope.Session, stateNameArgumentValue);
                _console.WriteLine($"{stateNameArgumentValue}={value}");
            });
            stateCommand.Add(stateGetCommand);



            rootCommand.AddCommand(stateCommand);


            var invokeResult = await rootCommand.InvokeAsync(args, _console);
            return invokeResult;
        }
    }
}
