using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ave.Extensions.Console.StateManagement;
using Core.Application;
using Core.Infrastructure.ConsoleApp.Configuration;
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


            // determine root folder for saving session data
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ave", "oauth-cli");

            var sessionStateSerializer = new SessionStateProtector(new BinarySessionStateSerializer());

            var sessionStorage = new FileSessionStorage(new SystemDirectory(), new SystemFile(), sessionStateSerializer, path);

            // create Session for generating correct session key
            var sessionMananager = new SessionManager(sessionStorage, new SystemProcessIdProvider());

            // create state manager
            var stateManager = new StateManager(sessionMananager);

            var console = new SystemConsole();
            var passwordProvider = new PasswordProvider();

            // Get certificate stores from app settings

            var certificateStoreConfigurations = new Dictionary<string, CertificateStoreConfiguration>();
            configuration.GetSection("certificateStores").Bind(certificateStoreConfigurations);

            var certificateRepositories = new Dictionary<string, ICertificateRepository>();


            foreach (var certificateStoreConfiguration in certificateStoreConfigurations)
            {
                if (certificateStoreConfiguration.Value.Type == CertificateStoreType.Windows)
                {
                    WindowsCertificateStoreConfiguration windowsCertificateStoreConfiguration = new WindowsCertificateStoreConfiguration();
                    configuration.GetSection($"certificateStores:{certificateStoreConfiguration.Key}").Bind(windowsCertificateStoreConfiguration);

                    certificateRepositories.Add(certificateStoreConfiguration.Key, new Windows.CertificateStore.CertificateRepository(windowsCertificateStoreConfiguration.Location));
                }
                else if (certificateStoreConfiguration.Value.Type == CertificateStoreType.LocalFile)
                {
                    LocalFileCertificateStoreConfiguration localFileCertificateStoreConfiguration = new LocalFileCertificateStoreConfiguration();
                    configuration.GetSection($"certificateStores:{certificateStoreConfiguration.Key}").Bind(localFileCertificateStoreConfiguration);

                    certificateRepositories.Add(certificateStoreConfiguration.Key, new FileSystem.CertificateRepository(localFileCertificateStoreConfiguration.Folder, passwordProvider));
                }
            }

            var certificateProviderFactory = new CertificateRepositoryProvider(certificateRepositories);

            var commandLineHandler = new CommandLineHandler(
                new ClientService(console, 
                    certificateProviderFactory, 
                    passwordProvider, 
                    clientConfigurations,
                    new OAuth.GetTokenService()
                ), 
                console, 
                stateManager);

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





