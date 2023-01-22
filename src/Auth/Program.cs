using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ConsoleApp2
{
    class Program
    {
        static Task<Auth.Models.OpenIdConfiguration?> GetOpenIdConfiguration(string url)
        {
            var client = new HttpClient();
            return client.GetFromJsonAsync<Auth.Models.OpenIdConfiguration>(url);
        }

        static string GenerateRandomCodeVerifier()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz123456789";
            var random = new Random();
            var nonce = new char[128];
            for (int i = 0; i < nonce.Length; i++)
            {
                nonce[i] = chars[random.Next(chars.Length)];
            }

            return new string(nonce);
        }

        static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var b64Hash = Convert.ToBase64String(hash);
            var code = Regex.Replace(b64Hash, "\\+", "-");
            code = Regex.Replace(code, "\\/", "_");
            code = Regex.Replace(code, "=+$", "");
            return code;
        }



        static async Task Main(string[] args)
        {
            
            string? returnedCode = null;
            bool cancelled = false;
            bool usePKCE = true;


            // get basic configuration to see if we need to load other configuration providers as well
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();

            // set up configuration to be used by the rest of the applicaton
            configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var userSecretsID = configuration.GetValue<string>("UserSecretsId");
            if(!string.IsNullOrEmpty(userSecretsID))
            {
                configurationBuilder.AddUserSecrets(userSecretsID);
            }
            configurationBuilder.AddEnvironmentVariables();
            configuration = configurationBuilder.Build();


            // Get all client configurations
            var clientConfigurations = new Dictionary<string, Auth.Configuration.ClientConfiguration>();
            configuration.GetSection("clients").Bind(clientConfigurations);

            Console.WriteLine($"Authorizing for client {args[0]}");

            Auth.Configuration.ClientConfiguration clientConfiguration = null;

            if (!clientConfigurations.TryGetValue(args[0], out clientConfiguration))
                {
                Console.WriteLine($"No configuration for client {args[0]}!");
                return;
            }

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                cancelled = true;

            };

            var openIdConfiguration = await GetOpenIdConfiguration(clientConfiguration.WellknownEndpoint);

            var codeVerifier = GenerateRandomCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            var encodedRedirectUri = HttpUtility.UrlEncode(clientConfiguration.RedirectUri);

            string[] scopes = clientConfiguration.Scopes;

            if (clientConfiguration.ApplicationIdUri != null)
            {
                scopes = scopes.Select(s => s.Replace("{ApplicationIdUri}", clientConfiguration.ApplicationIdUri)).ToArray();
            }

            scopes = scopes.Select(s => HttpUtility.UrlEncode(s)).ToArray();

            var encodedScopes = string.Join(" ", scopes);


            // Get the code

            if (clientConfiguration.GrantType == Auth.Configuration.GrantType.AuthorizationCode)
            {
                var url = $"{openIdConfiguration.AuthorizeEndpoint}?" +
                    $"client_id={clientConfiguration.ClientId}" +
                    $"&response_type=code" +
                    $"&redirect_uri={encodedRedirectUri}" +
                    $"&response_mode=query&scope={encodedScopes}";

                if (clientConfiguration.AuthorizationParameters != null)
                {
                    foreach (var parameter in clientConfiguration.AuthorizationParameters)
                    {
                        url = url + $"&{parameter.Key}={parameter.Value}";
                    }
                }

                if (usePKCE)
                {
                    url = url + $"&code_challenge={codeChallenge}&code_challenge_method=S256";
                }

                var ps = new ProcessStartInfo(url)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);

                // Setup WebServer to handle the redirect

                var redirectUri = new Uri(clientConfiguration.RedirectUri);
                var serverUrl = $"{redirectUri.Scheme}://{redirectUri.Host}:{redirectUri.Port}/";

                var builder = WebApplication.CreateBuilder();
                builder.Services.AddCors();
                builder.WebHost.UseUrls(serverUrl);

                var app = builder.Build();

                // Configure the HTTP request pipeline.

                app.UseHttpsRedirection();
                app.UseCors(builder =>
                {
                    builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });

                app.MapGet(redirectUri.AbsolutePath, (string? code) =>
                {
                    returnedCode = code;
                    return "Received Code";
                });

                var thread = new Thread(() => app.Run());
                thread.Start();

                // Wait for code from redirect

                while (returnedCode == null && !cancelled)
                {
                    await Task.Delay(1000);
                }
                await app.StopAsync();
                Console.WriteLine($"Received code: {returnedCode}");
            }

            // Now get the token


            var formFields = new Dictionary<string, string>();
            formFields.Add("client_id", clientConfiguration.ClientId);

            if (clientConfiguration.ClientSecret != null)
            {
                formFields.Add("client_secret", clientConfiguration.ClientSecret);
            }

            if(clientConfiguration.ClientCertificateName != null)
            {
                X509Store store = new X509Store(StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                var certs = store.Certificates.Find(X509FindType.FindBySubjectName, clientConfiguration.ClientCertificateName, false);
                var signingCert = certs[0];
               
                var jwtToken = JwtCreator.CreateTokenWithX509SigningCredentials(signingCert, clientConfiguration.ClientId, clientConfiguration.TenantId);
                formFields.Add("client_assertion_type", UrlEncoder.Default.Encode("urn:ietf:params:oauth:client-assertion-type:jwt-bearer"));
                formFields.Add("client_assertion", jwtToken);

            }
            formFields.Add("scope", encodedScopes);


            if (clientConfiguration.GrantType == Auth.Configuration.GrantType.AuthorizationCode)
            {
                formFields.Add("grant_type", "authorizaton_code");

                formFields.Add("code", returnedCode);
                formFields.Add("redirect_uri", encodedRedirectUri);
                
                if (usePKCE)
                {
                    formFields.Add("code_verifier", codeVerifier);
                }
            }

            if (clientConfiguration.GrantType == Auth.Configuration.GrantType.ClientCredentials)
            {
                formFields.Add("grant_type", "client_credentials");            
            }

            if (clientConfiguration.GrantType == Auth.Configuration.GrantType.Password)
            {
                Console.Write("Username:");
                var userName = Console.ReadLine();
                Console.WriteLine($"-{userName}-");
                Console.Write("Password:");


                var password = string.Empty;
                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey(true);
                    // Skip if Backspace or Enter is Pressed
                    if (keyInfo.Key != ConsoleKey.Backspace && keyInfo.Key != ConsoleKey.Enter)
                    {
                        password += keyInfo.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
                        {
                            // Remove last charcter if Backspace is Pressed
                            password = password.Substring(0, (password.Length - 1));
                            Console.Write("\b \b");
                        }
                    }
                }
                // Stops Getting Password Once Enter is Pressed
                while (keyInfo.Key != ConsoleKey.Enter);

                Console.WriteLine();

                formFields.Add("grant_type", "password");
                formFields.Add("username", userName);
                formFields.Add("password", password);
            }

            var body = BodyFormatter.Format(formFields);

            var client = new HttpClient();
            var response = await client.PostAsync(
                openIdConfiguration.TokenEndpoint,
                new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"));

            var tokenResult = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<object>(tokenResult);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            tokenResult = JsonSerializer.Serialize(json, jsonSerializerOptions);


            Console.WriteLine($"Received token: {tokenResult}");
        }
    }
}





