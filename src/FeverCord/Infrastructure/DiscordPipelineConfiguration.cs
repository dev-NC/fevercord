namespace FeverCordSvc.Infrastructure
{
    public class DiscordPipelineConfiguration
    {
        public required int DiscordRequestLimit { get; set; }

        public required int DiscordRequestWindow { get; set; }
    }
}
