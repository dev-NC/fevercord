using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Discord.Content
{
    public class ImageContent
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
