using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Discord.Content
{
    public class EmbedContent
    {
        [JsonIgnore]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        [StringLength(256)]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        [StringLength(4096)]
        public string? Description { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("color")]
        public int? Color { get; set; }

        [JsonPropertyName("author")]
        public AuthorContent? Author { get; set; }

        [JsonPropertyName("image")]
        public ImageContent? Image { get; set; }
    }
}
