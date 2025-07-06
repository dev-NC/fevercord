using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Rss.Content
{
    public class UnreadItemListContent : BaseRssContent
    {
        [JsonPropertyName("unread_item_ids")]
        public string UnreadItemIds { get; set; }
    }
}
