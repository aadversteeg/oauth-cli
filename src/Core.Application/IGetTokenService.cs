using Ave.Extensions.Functional;
using Core.Application.Models;

namespace Core.Application
{
    public interface IGetTokenService
    {
        public Task<Result<GetTokenResult, string>> GetToken(Uri tokenEndpoint, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> content);
    }
}
