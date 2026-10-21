using FeverCordSvc.Features.Rss.Content;

namespace FeverCord.Tests.Features.Rss
{
    public class ItemContentExtensionsTests
    {
        [Fact]
        public void ToEmbed_LongTitle_TruncatedToEndWithEllipsis()
        {
            var item = TestData.Item("1", title: new string('t', 300));

            var embed = item.ToEmbed();

            Assert.Equal(256, embed.Title!.Length);
            Assert.EndsWith(" ...", embed.Title);
        }

        [Fact]
        public void ToEmbed_LongHtml_TruncatesDescriptionTo4096()
        {
            var item = TestData.Item("1", html: TestData.LongText(5000));

            var embed = item.ToEmbed();

            Assert.Equal(4096, embed.Description!.Length);
            Assert.EndsWith(" ...", embed.Description);
        }

        [Fact]
        public void ToEmbed_ExtractsFirstImageFromHtml()
        {
            var item = TestData.Item(
                "1",
                html: "<p>x</p>" +
                      "<img src=\"https://img.test/first.png\">" +
                      "<img src=\"https://img.test/second.png\">");

            var embed = item.ToEmbed();

            Assert.Equal("https://img.test/first.png", embed.Image!.Url);
        }

        [Fact]
        public void ToEmbed_EmptyHtml_YieldsEmptyDescriptionAndImageNoCrash()
        {
            var item = TestData.Item("1");

            var embed = item.ToEmbed();

            Assert.Equal(string.Empty, embed.Description);
            Assert.Equal(string.Empty, embed.Image!.Url);
        }

        [Fact]
        public void ToEmbed_CopiesIdUrlAndConfiguredColor()
        {
            var item = TestData.Item("42");

            var embed = item.ToEmbed(color: 999);

            Assert.Equal("42", embed.Id);
            Assert.Equal(item.Url, embed.Url);
            Assert.Equal(999, embed.Color);
        }

        [Fact]
        public void ToEmbed_ScriptText_NeverReachesDescription()
        {
            var item = TestData.Item("1", html: "<p>keep me</p><script>bad()</script>");

            var embed = item.ToEmbed();

            Assert.Contains("keep me", embed.Description);
            Assert.DoesNotContain("bad()", embed.Description);
        }

        [Fact]
        public void ToEmbed_AuthorName_IsNotTruncated()
        {
            // Known limitation, documented in README: only title and description
            // are cut to Discord's limits. A long author can therefore push a
            // single embed past the 6,000-char budget — if this test starts
            // failing because truncation got added, update the README bullets.
            var item = TestData.Item("1", author: new string('a', 300));

            var embed = item.ToEmbed();

            Assert.Equal(300, embed.Author!.Name!.Length);
        }
    }
}
