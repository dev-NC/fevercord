using FeverCordSvc.Infrastructure;

namespace FeverCordSvc.Features.Discord.Content
{
    public static class WebhookContentExtensions
    {
        public static int CalculateEmbedCharacterLength(
                this WebhookContent content)
        {
            return content.Embeds.Select(
                    embed => Utilities.CalculateCharacterLength(embed)).Sum();
        }
    }
}
