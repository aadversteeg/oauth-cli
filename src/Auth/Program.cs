using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            // set up configuration
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets("1cf919bb-3567-4c56-9401-931d767f1c4e")
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();

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



            var client = new HttpClient();

            string body = string.Empty;

          //  Console.WriteLine($"Using credentials {clientConfiguration.ClientId}:{clientConfiguration.ClientSecret}");


            if (clientConfiguration.GrantType == Auth.Configuration.GrantType.AuthorizationCode)
            {
                body =
                    $"client_id={clientConfiguration.ClientId}" +
                    $"&scope={encodedScopes}" +
                    $"&code={returnedCode}" +
                    $"&redirect_uri={encodedRedirectUri}" +
                    $"&grant_type=authorization_code";

                if (clientConfiguration.ClientSecret != null)
                {
                    body = body + $"&client_secret={clientConfiguration.ClientSecret}";
                }


                if (usePKCE)
                {
                    body = body + $"&code_verifier={codeVerifier}";
                }

            }

            if (clientConfiguration.GrantType == Auth.Configuration.GrantType.ClientCredentials)
            {
                body =
                    $"client_id={clientConfiguration.ClientId}" +
                    $"&scope={encodedScopes}" +
                    $"&grant_type=client_credentials";

                if (clientConfiguration.ClientSecret != null)
                {
                    body = body + $"&client_secret={clientConfiguration.ClientSecret}";
                }
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

                body =
                    $"client_id={clientConfiguration.ClientId}" +
                    $"&scope={encodedScopes}" +
                    $"&grant_type=password" +
                    $"&username={userName}" +
                    $"&password={password}";

                if (clientConfiguration.ClientSecret != null)
                {
                    body = body + $"&client_secret={clientConfiguration.ClientSecret}";
                }
            }

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





