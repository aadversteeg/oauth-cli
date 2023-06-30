using Ave.Extensions.Functional;
using System.Security.Cryptography.X509Certificates;

namespace Core.Application
{
    public interface ICertificateRepository
    {
        Result<X509Certificate2, string> GetCertificate(string name);
    }
}