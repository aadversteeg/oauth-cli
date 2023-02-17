using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Infrastructure.ConsoleApp
{
    public interface IClientService
    {
        Task<string> GetAccessToken(string clientName);
        Task<IReadOnlyCollection<string>> GetClients();
    }
}
