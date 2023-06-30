using Core.Infrastructure.ConsoleApp.Configuration;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography;
using Core.Application;

namespace Core.Infrastructure.ConsoleApp
{
    public class ClientService : IClientService
    {
        private readonly IConsole _console;
        private readonly IDictionary<string, ClientConfiguration> _clients;
        private readonly ICertificateRepositoryProvider _certificateProviderFactory;
        private readonly IPasswordProvider _passwordProvider;

        public ClientService(
            IConsole console, 
            ICertificateRepositoryProvider certificateProviderFactory, 
            IPasswordProvider passwordProvider,
            IDictionary<string, ClientConfiguration> clients) {
            _console = console;
            _certificateProviderFactory = certificateProviderFactory;
            _passwordProvider = passwordProvider;
            _clients = clients;
        }

        public async Task<string> GetAccessToken(string clientName, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Authorizing for client {clientName}");

            string? returnedCode = null;
            bool usePKCE = true;

            Configuration.ClientConfiguration clientConfiguration = null;

            if (!_clients.TryGetValue(clientName, out clientConfiguration))
            {
                Console.WriteLine($"No configuration for client {clientName}!");
                return String.Empty;
            }


            var openIdConfiguration = await GetOpenIdConfiguration(clientConfiguration.WellknownEndpoint, cancellationToken);


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

            if (clientConfiguration.GrantType == Configuration.GrantType.AuthorizationCode)
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

                //app.UseHttpsRedirection();
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

                while (returnedCode == null)
                {
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        await app.StopAsync();
                        throw;
                    }
                    await Task.Delay(1000, cancellationToken);
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

            X509Certificate2 signingCert = null;

            if (clientConfiguration.ClientCertificateName != null)
            {
                var maybeClientRepository = clientConfiguration.ClientCertificateStore != null ?
                    _certificateProviderFactory.GetRepository(clientConfiguration.ClientCertificateStore) :
                    _certificateProviderFactory.GetRepository("windows-certificate-store");

                if (maybeClientRepository.HasValue)
                {
                    var certificateResult = maybeClientRepository.Value.GetCertificate(clientConfiguration.ClientCertificateName);
                    if (certificateResult.IsFailure)
                    {
                        throw new Exception(certificateResult.Error);
                    }

                    signingCert = certificateResult.Value;
                }
            }

            if (signingCert != null)
            {
                var jwtToken = JwtCreator.CreateTokenWithX509SigningCredentials(signingCert, clientConfiguration.ClientId, clientConfiguration.TenantId);
                formFields.Add("client_assertion_type", UrlEncoder.Default.Encode("urn:ietf:params:oauth:client-assertion-type:jwt-bearer"));
                formFields.Add("client_assertion", jwtToken);
            }

            formFields.Add("scope", encodedScopes);

            if (clientConfiguration.GrantType == Configuration.GrantType.AuthorizationCode)
            {
                formFields.Add("grant_type", "authorization_code");

                formFields.Add("code", returnedCode);
                formFields.Add("redirect_uri", encodedRedirectUri);

                if (usePKCE)
                {
                    formFields.Add("code_verifier", codeVerifier);
                }
            }

            if (clientConfiguration.GrantType == Configuration.GrantType.ClientCredentials)
            {
                formFields.Add("grant_type", "client_credentials");
            }

            if (clientConfiguration.GrantType == Configuration.GrantType.Password)
            {
                Console.Write("Username:");
                var userName = Console.ReadLine();
                Console.WriteLine($"-{userName}-");

                var password = _passwordProvider.GetPassword("Password");
                Console.WriteLine();

                formFields.Add("grant_type", "password");
                formFields.Add("username", userName);
                formFields.Add("password", password);
            }

            Console.WriteLine("Form fields:");
            foreach (var formField in formFields)
                Console.WriteLine($"  {formField.Key}={formField.Value}");

            var body = BodyFormatter.Format(formFields);

            Console.WriteLine(body);

            var client = new HttpClient();
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(openIdConfiguration.TokenEndpoint),
                Method = HttpMethod.Get,
                Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            request.Headers.Add("Origin", "https://localhost");

            var response = await client.SendAsync(request);
            
            var tokenResult = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<object>(tokenResult);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            tokenResult = JsonSerializer.Serialize(json, jsonSerializerOptions);

            return tokenResult;
        }

        public Task<IReadOnlyCollection<string>> GetClients(CancellationToken cancellationToken) 
        {
            var clientNames = (IReadOnlyCollection<string>)_clients.Keys;
            return Task.FromResult(clientNames);
        }

        private static Task<Models.OpenIdConfiguration?> GetOpenIdConfiguration(string url, CancellationToken cancellationToken)
        {
            var client = new HttpClient();
            return client.GetFromJsonAsync<Models.OpenIdConfiguration>(url, cancellationToken);
        }

        private static string GenerateRandomCodeVerifier()
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

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var b64Hash = Convert.ToBase64String(hash);
            var code = Regex.Replace(b64Hash, "\\+", "-");
            code = Regex.Replace(code, "\\/", "_");
            code = Regex.Replace(code, "=+$", "");
            return code;
        }
    }
}
