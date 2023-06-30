using CSharpFunctionalExtensions;

namespace Core.Application
{
    public interface ICertificateRepositoryProvider
    {
        Maybe<ICertificateRepository> GetRepository(string certificateProviderName);
    }
}
