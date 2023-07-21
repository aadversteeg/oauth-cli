namespace Core.Application.Models
{
    public class GetTokenSuccess
    {
        public required string AccessToken { get; set; }

        public required string TokenType { get; set; }

        public int? ExpiresIn { get; set; }

        public string? RefreshToken { get; set; }

        public string? Scope { get; set; }
    }
}
