using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Rss.Content
{
    public class ItemContent : BaseRssContent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("feed_id")]
        public int FeedId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("html")]
        public string Html { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("is_saved")]
        public int IsSaved { get; set; }

        [JsonPropertyName("is_read")]
        public int IsRead { get; set; }

        [JsonPropertyName("created_on_time")]
        public int CreatedOnTime { get; set; }
    }
}
