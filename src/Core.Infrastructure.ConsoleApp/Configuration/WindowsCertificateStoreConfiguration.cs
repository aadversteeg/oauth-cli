namespace Core.Infrastructure.ConsoleApp.Configuration
{
    public class WindowsCertificateStoreConfiguration : CertificateStoreConfiguration 
    {
        public System.Security.Cryptography.X509Certificates.StoreLocation Location { get; set; }
    }
}
