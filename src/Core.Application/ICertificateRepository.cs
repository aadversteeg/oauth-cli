using CSharpFunctionalExtensions;
using System.Security.Cryptography.X509Certificates;

namespace Core.Application
{
    public interface ICertificateRepository
    {
        Result<X509Certificate2> GetCertificate(string name);
    }
}