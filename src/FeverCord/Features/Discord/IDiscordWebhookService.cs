using FeverCordSvc.Features.Discord.Content;

namespace FeverCordSvc.Features.Discord
{
    public interface IDiscordWebhookService
    {
        Task PostWebhookContentAsync(WebhookContent content);
    }
}
