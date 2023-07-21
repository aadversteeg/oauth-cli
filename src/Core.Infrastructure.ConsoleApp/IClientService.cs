using Ave.Extensions.Functional;
using Core.Application.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Infrastructure.ConsoleApp
{
    public interface IClientService
    {
        Task<Result<GetTokenResult, string>> GetAccessToken(string clientName, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<string>> GetClients(CancellationToken cancellationToken);
    }
}
