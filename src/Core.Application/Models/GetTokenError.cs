
namespace Core.Application.Models
{
    public class GetTokenError
    {
        public required string Error { get; set; }

        public string? ErrorDescription { get; set; }

        public string? ErrorUri { get; set; }
    }
}
