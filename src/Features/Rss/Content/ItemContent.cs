using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Rss.Content
{
    public class ItemContent : BaseRssContent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("feed_id")]
        public int FeedId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("html")]
        public string Html { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("is_saved")]
        public int IsSaved { get; set; }

        [JsonPropertyName("is_read")]
        public int IsRead { get; set; }

        [JsonPropertyName("created_on_time")]
        public int CreatedOnTime { get; set; }
    }
}
