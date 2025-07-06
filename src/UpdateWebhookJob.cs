using FeverCordSvc.Features.Discord;
using FeverCordSvc.Features.Discord.Content;
using FeverCordSvc.Features.Rss;
using FeverCordSvc.Features.Rss.Content;
using FeverCordSvc.Infrastructure;
using Quartz;

namespace FeverCordSvc
{
    [DisallowConcurrentExecution]
    public class UpdateWebhookJob : IJob
    {
        private const int MaxEmbedContentLength = 6000;
        private readonly DiscordSettings _discordSettings;
        private readonly IDiscordWebhookService _discordWebhookService;
        private readonly FeverSettings _feverSettings;
        private readonly ILogger<UpdateWebhookJob> _logger;
        private readonly IRssFeedService _rssFeedService;

        public UpdateWebhookJob(
            FeverSettings feverSettings,
            DiscordSettings discordSettings,
            ILogger<UpdateWebhookJob> logger,
            IRssFeedService rssFeedService,
            IDiscordWebhookService discordWebhookService)
        {
            _feverSettings = feverSettings;
            _discordSettings = discordSettings;
            _logger = logger;
            _rssFeedService = rssFeedService;
            _discordWebhookService = discordWebhookService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Checking for updates...");
            var update = await CheckForUpdatesAsync();
            if (!update.Available)
            {
                _logger.LogInformation("No new items found.  Exiting...");
                return;
            }

            _logger.LogInformation(
                $"Update available: {update.UnreadItemIds.Count()} new item(s).");

            _logger.LogInformation("Fetching items...");
            var items = await FetchItemsByIdsAsync(update.UnreadItemIds);

            _logger.LogInformation("Converting to embeds...");
            var embeds = new Queue<EmbedContent>();
            foreach (var item in items)
            {
                embeds.Enqueue(item.ToEmbed(_discordSettings.DiscordEmbedColor));
            }

            _logger.LogInformation("Sending embeds...");
            var successfulIds = await SendEmbedsAsync(embeds);
            await MarkItemsAsReadAsync(successfulIds);

            _logger.LogInformation("Update complete.  Exiting...");
            return;
        }

        private async Task<(bool Available, Queue<string> UnreadItemIds)> CheckForUpdatesAsync()
        {
            var unreadItemIds = await _rssFeedService.FetchUnreadItemIdsAsync();
            return (unreadItemIds.Any(), new Queue<string>(unreadItemIds));
        }

        private async Task<IEnumerable<ItemContent>> FetchItemsByIdsAsync(
            Queue<string> itemQueue)
        {
            var items = new List<ItemContent>();
            while (itemQueue.Count > 0)
            {
                var itemIds = itemQueue.DequeueMany(_feverSettings.RssItemFetchLimit);
                try
                {
                    items.AddRange(await _rssFeedService.FetchItemsByIdsAsync(itemIds));
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        $"Unable to fetch items: {string.Join(',', itemIds)}.");
                }
            }

            return items;
        }

        private async Task MarkItemsAsReadAsync(IEnumerable<string> itemIds)
        {
            foreach (var id in itemIds)
            {
                try
                {
                    await _rssFeedService.MarkItemAsReadByIdAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unable to mark item '{id}' as read.");
                }
            }
        }

        private async Task<IEnumerable<string>> SendEmbedsAsync(
                    Queue<EmbedContent> embedQueue)
        {
            var successfulIds = new List<string>();
            while (embedQueue.Count > 0)
            {
                var content = new WebhookContent
                {
                    Flags = (int)MessageFlags.SUPPRESS_NOTIFICATIONS,
                    Embeds = embedQueue.DequeueMany(_discordSettings.DiscordEmbedLimit)
                };

                while (content.CalculateEmbedCharacterLength() > MaxEmbedContentLength)
                {
                    embedQueue.Enqueue(content.Embeds.Dequeue());
                }

                if (!content.Embeds.Any())
                {
                    throw new InvalidOperationException(
                            $"No embeds in WebhookContent.  " +
                            $"Ids left in queue: " +
                            $"{string.Join(',', embedQueue.Select(embed => embed.Id))}");
                }

                try
                {
                    await _discordWebhookService.PostWebhookContentAsync(content);
                    successfulIds.AddRange(content.Embeds.Select(embed => embed.Id));
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        $"Unable to send embeds: " +
                        $"{string.Join(',', content.Embeds.Select(embed => embed.Id))}.");
                }
            }

            return successfulIds;
        }
    }
}