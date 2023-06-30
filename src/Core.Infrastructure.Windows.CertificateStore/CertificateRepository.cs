using Core.Application;
using Ave.Extensions.Functional;
using System.Security.Cryptography.X509Certificates;

namespace Core.Infrastructure.Windows.CertificateStore
{
    public class CertificateRepository : ICertificateRepository
    {
        public Result<X509Certificate2, string> GetCertificate(string name)
        {
            X509Certificate2? signingCert = null;

            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, name, false);
            if(certs.Count== 0)
            {
                return Result<X509Certificate2, string>.Failure($"Certificate {name} does not exist.");
            }

            return Result<X509Certificate2, string>.Success(certs[0]);
        }
    }
}