using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Discord.Content
{
    public class WebhookContent
    {
        [JsonPropertyName("flags")]
        public int? Flags { get; set; }

        [JsonPropertyName("embeds")]
        public Queue<EmbedContent> Embeds { get; set; }
    }
}
