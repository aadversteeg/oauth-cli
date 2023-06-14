using Core.Application;
using CSharpFunctionalExtensions;
using System.Security.Cryptography.X509Certificates;

namespace Core.Infrastructure.Windows.CertificateStore
{
    public class CertificateRepository : ICertificateRepository
    {
        public Result<X509Certificate2> GetCertificate(string name)
        {
            X509Certificate2? signingCert = null;

            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, name, false);
            if(certs.Count== 0)
            {
                return Result.Failure<X509Certificate2>($"Certificate {name} does not exist.");
            }

            return Result.Success<X509Certificate2>(certs[0]);
        }
    }
}