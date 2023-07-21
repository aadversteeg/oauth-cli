using System.Net.Http.Json;
using Core.Infrastructure.GitHub.Models;

namespace Core.Infrastructure.GitHub
{
    public class GitHubService
    {
        public async Task<Version> GetLatestVersion(CancellationToken cancellationToken)
        {
            try
            {
                var url = "https://api.github.com/repos/aadversteeg/oauth-cli/releases";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "oauth-cli");

                var response = await client.GetFromJsonAsync<Release[]>(url, cancellationToken);
                var latestRelease = response.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
                return new Version(latestRelease.TagName.Substring(1));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to determine latest version. Error {e.Message}");

            }
            return new Version();
        }
    }
}