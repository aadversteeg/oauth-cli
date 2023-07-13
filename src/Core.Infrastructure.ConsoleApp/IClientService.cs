using Ave.Extensions.Functional;
using Core.Infrastructure.ConsoleApp.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Infrastructure.ConsoleApp
{
    public interface IClientService
    {
        Task<Result<GetTokenResponse, GetTokenError>> GetAccessToken(string clientName, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<string>> GetClients(CancellationToken cancellationToken);
    }
}
