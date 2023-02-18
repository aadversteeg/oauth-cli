using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Core.Infrastructure.ConsoleApp
{
    class Program
    {

        static async Task<int> Main(string[] args)
        {
            Version latestVersion = null;

            var thread = new Thread( async () => {
                var gitHubService = new Core.Infrastructure.GitHub.GitHubService();
                latestVersion = await gitHubService.GetLatestVersion(CancellationToken.None);
            });
            thread.Start();


            // get basic configuration to see if we need to load other configuration providers as well
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();

            // set up configuration to be used by the rest of the applicaton
            configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var userSecretsID = configuration.GetValue<string>("UserSecretsId");
            if (!string.IsNullOrEmpty(userSecretsID))
            {
                configurationBuilder.AddUserSecrets(userSecretsID);
            }
            configurationBuilder.AddEnvironmentVariables();
            configuration = configurationBuilder.Build();

            // Get all client configurations
            var clientConfigurations = new Dictionary<string, Configuration.ClientConfiguration>();
            configuration.GetSection("clients").Bind(clientConfigurations);

            var console = new SystemConsole();
            var commandLineHandler = new CommandLineHandler(new ClientService(console, clientConfigurations), console);
            var invokeResult = await commandLineHandler.Invoke(args);

            while( latestVersion == null)
            {
                await Task.Delay(200);
            }

            var currentVersion = new Version(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            if (currentVersion < latestVersion)
            {
                Console.WriteLine($"A newer version is available {currentVersion} -> {latestVersion}");
            }


            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press any key...");
                Console.ReadLine();
            }

            return invokeResult;
        }
    }
}





