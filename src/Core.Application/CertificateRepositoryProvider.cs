using CSharpFunctionalExtensions;

namespace Core.Application
{
    public class CertificateRepositoryProvider : ICertificateRepositoryProvider
    {
        private readonly IReadOnlyDictionary<string, ICertificateRepository> _providers;

        public CertificateRepositoryProvider(IReadOnlyDictionary<string, ICertificateRepository> providers)
        {
            _providers = providers;
        }

        public Maybe<ICertificateRepository> GetRepository(string certificateStoreName)
        {
            if (_providers.TryGetValue(certificateStoreName, out var certificateRepository))
            {
                return Maybe.From(certificateRepository);
            }
            return Maybe<ICertificateRepository>.None;
        }
    }
}
