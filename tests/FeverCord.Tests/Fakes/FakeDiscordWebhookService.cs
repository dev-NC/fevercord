using FeverCordSvc.Features.Discord;
using FeverCordSvc.Features.Discord.Content;

namespace FeverCord.Tests.Fakes
{
    internal sealed class FakeDiscordWebhookService : IDiscordWebhookService
    {
        public List<WebhookContent> Attempts { get; } = [];

        public List<WebhookContent> PostedContent { get; } = [];

        public Func<WebhookContent, Exception?>? PostFault { get; set; }

        public Task PostWebhookContentAsync(WebhookContent content)
        {
            Attempts.Add(content);

            var fault = PostFault?.Invoke(content);
            if (fault != null)
            {
                throw fault;
            }

            PostedContent.Add(content);
            return Task.CompletedTask;
        }
    }
}
