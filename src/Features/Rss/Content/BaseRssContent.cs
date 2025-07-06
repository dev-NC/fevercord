using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Rss.Content
{
    public class BaseRssContent
    {
        [JsonPropertyName("api_version")]
        public int ApiVersion { get; set; }

        [JsonPropertyName("auth")]
        public int Auth { get; set; }

        [JsonPropertyName("last_refreshed_on_time")]
        public int LastRefreshTime { get; set; }
    }
}
