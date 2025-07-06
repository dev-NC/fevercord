using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Rss.Content
{
    public class ItemListContent : BaseRssContent
    {
        [JsonPropertyName("total_items")]
        public int TotalItems { get; set; }

        [JsonPropertyName("items")]
        public IEnumerable<ItemContent> Items { get; set; }
    }
}
