using FeverCordSvc.Features.Discord.Content;
using System.Net.Http.Json;

namespace FeverCordSvc.Features.Discord
{
    public class DiscordWebhookService : IDiscordWebhookService
    {
        private readonly HttpClient _httpClient;

        public DiscordWebhookService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task PostWebhookContentAsync(WebhookContent content)
        {
            if (content == null)
            {
                throw new ArgumentException("Content cannot be null.");
            }

            var response = await _httpClient.PostAsJsonAsync(
                string.Empty,
                content);

            response.EnsureSuccessStatusCode();
        }
    }
}
