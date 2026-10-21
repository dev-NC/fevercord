using FeverCordSvc.Features.Discord.Content;

namespace FeverCord.Tests.Features.Discord
{
    public class WebhookContentExtensionsTests
    {
        [Fact]
        public void CalculateEmbedCharacterLength_EmptyEmbed_ReturnsZero()
        {
            var content = NewWebhookContent(new EmbedContent());

            Assert.Equal(0, content.CalculateEmbedCharacterLength());
        }

        [Fact]
        public void CalculateEmbedCharacterLength_SumsStringPropertiesExactly()
        {
            // Id "abc" (3) + Title "hi" (2) + Description "12345" (5).
            var content = NewWebhookContent(new EmbedContent
            {
                Id = "abc",
                Title = "hi",
                Description = "12345",
            });

            Assert.Equal(10, content.CalculateEmbedCharacterLength());
        }

        [Fact]
        public void CalculateEmbedCharacterLength_CountsColorDigitsAndNestedAuthor()
        {
            // Description "x" (1) + Color 12345 as digits (5) + Author "Jane" (4).
            // The reflection-based calculation deliberately over-estimates: it counts
            // Id (a tracking field Discord never sees) and color digits, so the
            // 6,000-char budget check errs on the conservative side.
            var content = NewWebhookContent(new EmbedContent
            {
                Id = string.Empty,
                Description = "x",
                Color = 12345,
                Author = new AuthorContent { Name = "Jane" },
            });

            Assert.Equal(10, content.CalculateEmbedCharacterLength());
        }

        [Fact]
        public void CalculateEmbedCharacterLength_SumsAcrossAllEmbeds()
        {
            var content = NewWebhookContent(
                new EmbedContent { Id = string.Empty, Title = "aa" },
                new EmbedContent { Id = string.Empty, Title = "bb" });

            Assert.Equal(4, content.CalculateEmbedCharacterLength());
        }

        private static WebhookContent NewWebhookContent(params EmbedContent[] embeds)
        {
            return new WebhookContent
            {
                Embeds = new Queue<EmbedContent>(embeds),
            };
        }
    }
}
