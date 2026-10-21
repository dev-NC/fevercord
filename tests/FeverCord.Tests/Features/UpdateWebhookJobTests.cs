using FeverCord.Tests.Fakes;
using FeverCordSvc;
using FeverCordSvc.Features.Discord.Content;
using FeverCordSvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace FeverCord.Tests.Features
{
    public class UpdateWebhookJobTests
    {
        private const int EmbedColor = 123456;

        [Fact]
        public async Task Execute_NoUnreadItems_DoesNotPostOrMarkRead()
        {
            var rss = new FakeRssFeedService();
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord);

            await job.Execute(null!);

            Assert.Empty(discord.Attempts);
            Assert.Empty(rss.MarkedReadIds);
            Assert.Empty(rss.FetchBatchCalls);
        }

        [Fact]
        public async Task Execute_UnreadItems_PostsInBatchesOfEmbedLimit()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = Ids(1, 25),
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord, discordEmbedLimit: 10);

            await job.Execute(null!);

            Assert.Equal(
                new[] { 10, 10, 5 },
                discord.PostedContent.Select(c => c.Embeds.Count));
        }

        [Fact]
        public async Task Execute_MoreIdsThanFetchLimit_FetchesInBatchesOfRssItemFetchLimit()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = Ids(1, 55),
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord, rssFetchLimit: 50);

            await job.Execute(null!);

            Assert.Equal(
                new[] { 50, 5 },
                rss.FetchBatchCalls.Select(call => call.Count));
            Assert.Equal(55, rss.MarkedReadIds.Count);
        }

        [Fact]
        public async Task Execute_WhenPostSucceeds_MarksPostedItemsAsRead()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = Ids(1, 3),
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord);

            await job.Execute(null!);

            Assert.Equal(Ids(1, 3), rss.MarkedReadIds);
        }

        [Fact]
        public async Task Execute_WhenPostFails_DoesNotMarkFailedItemsAsRead()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = Ids(1, 3),
            };
            var discord = new FakeDiscordWebhookService
            {
                PostFault = _ => new HttpRequestException("webhook is down"),
            };
            var job = CreateJob(rss, discord);

            await job.Execute(null!);

            Assert.Single(discord.Attempts);
            Assert.Empty(discord.PostedContent);
            Assert.Empty(rss.MarkedReadIds);
        }

        [Fact]
        public async Task Execute_WhenPostFails_ContinuesWithRemainingBatches()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = Ids(1, 12),
            };
            var discord = new FakeDiscordWebhookService();
            var attempts = 0;
            discord.PostFault = _ =>
                ++attempts == 1
                    ? new HttpRequestException("webhook is down")
                    : null;
            var job = CreateJob(rss, discord, discordEmbedLimit: 10);

            await job.Execute(null!);

            // 12 items in two batches (10 + 2): first batch fails, second succeeds.
            Assert.Equal(2, discord.Attempts.Count);
            Assert.Single(discord.PostedContent);
            Assert.Equal(
                new[] { "11", "12" }.OrderBy(id => id),
                rss.MarkedReadIds.OrderBy(id => id));
        }

        [Fact]
        public async Task Execute_WhenFetchFailsForOneBatch_ContinuesWithOthers()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = new List<string> { "1", "2", "3" },
                FetchFault = ids => ids.Contains("2")
                    ? new HttpRequestException("fever is down")
                    : null,
            };
            var discord = new FakeDiscordWebhookService();
            // One id per fetch call so the fault hits exactly one batch.
            var job = CreateJob(rss, discord, rssFetchLimit: 1);

            await job.Execute(null!);

            Assert.Equal(3, rss.FetchBatchCalls.Count);
            Assert.Equal(
                new[] { "1", "3" },
                rss.MarkedReadIds);
        }

        [Fact]
        public async Task Execute_WhenMarkReadFails_ContinuesWithRemainingItems()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = new List<string> { "1", "2", "3" },
                MarkReadFault = id => id == "2"
                    ? new HttpRequestException("fever is down")
                    : null,
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord);

            await job.Execute(null!);

            // "2" was posted successfully; only the mark-read call failed.
            Assert.Equal(new[] { "1", "3" }, rss.MarkedReadIds);
        }

        [Fact]
        public async Task Execute_BatchesExceedingCharacterBudget_SplitIntoSeparatePosts()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = new List<string> { "1", "2" },
                FetchResponder = ids => ids
                    .Select(id => TestData.Item(id, html: TestData.LongText(4500))),
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord, discordEmbedLimit: 10);

            await job.Execute(null!);

            Assert.Equal(2, discord.PostedContent.Count);
            Assert.All(discord.PostedContent, content =>
                Assert.True(
                    content.CalculateEmbedCharacterLength() <= 6000,
                    $"post of {content.CalculateEmbedCharacterLength()} chars exceeds the 6,000-char budget"));

            // Both items eventually delivered and marked read. Note: the split loop
            // re-queues to the back, so the two posts arrive in swapped order —
            // at-least-once delivery does not guarantee ordering across a split.
            Assert.Equal(
                new[] { "1", "2" }.OrderBy(id => id),
                rss.MarkedReadIds.OrderBy(id => id));
        }

        [Fact]
        public async Task Execute_WhenSingleEmbedExceedsBudget_ThrowsAndMarksNothingRead()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = new List<string> { "1", "2" },
                FetchResponder = ids => ids
                    .Select(id => TestData.Item(
                        id,
                        // The author name is not truncated by ToEmbed, so it is the
                        // field that can push a single embed past 6,000 chars.
                        author: id == "1" ? TestData.LongText(6000) : "")),
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord, discordEmbedLimit: 10);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => job.Execute(null!));

            // Item 2 fits on its own and was posted — but the oversized item 1
            // aborts the run before anything is marked read. Item 2 will be
            // delivered again next tick: at-least-once means duplicates here,
            // never losses. This is the sharp edge documented in the README.
            Assert.Single(discord.PostedContent);
            Assert.Empty(rss.MarkedReadIds);
        }

        [Fact]
        public async Task Execute_PostsCarryConfiguredEmbedColor()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = Ids(1, 1),
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord);

            await job.Execute(null!);

            var embed = Assert.Single(discord.PostedContent).Embeds.Single();
            Assert.Equal(EmbedColor, embed.Color);
        }

        [Fact]
        public async Task Execute_PostsSuppressNotifications()
        {
            var rss = new FakeRssFeedService
            {
                UnreadIdsToReturn = Ids(1, 1),
            };
            var discord = new FakeDiscordWebhookService();
            var job = CreateJob(rss, discord);

            await job.Execute(null!);

            var content = Assert.Single(discord.PostedContent);
            Assert.Equal(
                (int)MessageFlags.SUPPRESS_NOTIFICATIONS,
                content.Flags);
        }

        private static UpdateWebhookJob CreateJob(
            FakeRssFeedService rss,
            FakeDiscordWebhookService discord,
            int rssFetchLimit = 50,
            int discordEmbedLimit = 10)
        {
            return new UpdateWebhookJob(
                new FeverSettings { RssItemFetchLimit = rssFetchLimit },
                new DiscordSettings
                {
                    DiscordEmbedLimit = discordEmbedLimit,
                    DiscordEmbedColor = EmbedColor,
                },
                NullLogger<UpdateWebhookJob>.Instance,
                rss,
                discord);
        }

        private static List<string> Ids(int from, int count)
        {
            return Enumerable.Range(from, count)
                .Select(id => id.ToString())
                .ToList();
        }
    }
}
