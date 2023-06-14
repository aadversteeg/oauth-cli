using Core.Application;
using CSharpFunctionalExtensions;
using System.Security.Cryptography.X509Certificates;

namespace Core.Infrastructure.FileSystem
{
    public class CertificateRepository : ICertificateRepository
    {
        private IPasswordProvider _passwordProvider;
        private string _certificateFolder;

        public CertificateRepository(string certificateFolder, IPasswordProvider passwordProvider)
        { 
            _certificateFolder = certificateFolder;
            _passwordProvider = passwordProvider;
        }

        public Result<X509Certificate2> GetCertificate(string name)
        {
            var filePath = Path.Combine(_certificateFolder, name);
            if(!File.Exists(filePath))
            {
                return Result.Failure<X509Certificate2>($"Certificate {name} does not exist.");
            }

            X509Certificate2? signingCert = null;

            var attemptsLeft = 3;
            while (signingCert == null && attemptsLeft > 0)
            {
                try
                {
                    signingCert = new X509Certificate2(filePath);
                }
                catch
                {
                }

                if (signingCert != null)
                {
                    break;
                }

                var password = _passwordProvider.GetPassword("Certificate Password");
                try
                {
                    signingCert = new X509Certificate2(filePath, password);
                }
                catch
                {
                    attemptsLeft--;
                }
            }

            if(attemptsLeft == 0)
            {
                return Result.Failure<X509Certificate2>("Unable to load certificate {name}.");
            }

            return Result.Success<X509Certificate2>(signingCert);
        }
    }
}