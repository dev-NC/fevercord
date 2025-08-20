namespace FeverCordSvc.Infrastructure
{
    public class FeverCordHttpClientConfiguration
    {
        public required string RssBaseAddress { get; set; }
        
        public required string DiscordWebhook { get; set; }
        
        public required int DiscordRequestLimit { get; set; }

        public required int DiscordRequestWindow { get; set; }
    }
}
