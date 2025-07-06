namespace FeverCordSvc.Features.Discord.Content
{
    [Flags]
    public enum MessageFlags
    {
        SUPPRESS_EMBEDS = 4,
        SUPPRESS_NOTIFICATIONS = 4096,
        IS_COMPONENTS_V2 = 32768
    }
}
