using System.Text.Json.Serialization;

namespace Core.Infrastructure.GitHub.Models
{
    public  class Release
    {
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
    }
}
