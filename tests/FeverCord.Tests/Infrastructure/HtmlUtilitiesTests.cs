using FeverCordSvc.Infrastructure;

namespace FeverCord.Tests.Infrastructure
{
    public class HtmlUtilitiesTests
    {
        [Fact]
        public void Cut_TextOverLimit_TruncatesToLengthWithEllipsis()
        {
            var result = HtmlUtilities.Cut(new string('x', 100), 10);

            Assert.Equal(10, result.Length);
            Assert.EndsWith(" ...", result);
        }

        [Fact]
        public void Cut_TextUnderLimit_ReturnsUnchanged()
        {
            Assert.Equal("hello", HtmlUtilities.Cut("hello", 10));
        }

        [Fact]
        public void Cut_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, HtmlUtilities.Cut(string.Empty, 10));
        }

        [Fact]
        public void NormalizeWhitespace_CollapsesRunsButPreservesNewlines()
        {
            var result = HtmlUtilities.NormalizeWhitespace("a   b \t c\r\n\r\n d");

            Assert.Equal("a b c\r\n\r\n d", result);
        }

        [Fact]
        public void ConvertToPlainText_RemovesScriptAndStyleContent()
        {
            var html = "<p>Hello <b>world</b></p>" +
                       "<script>badCode();</script>" +
                       "<style>.red{color:red}</style>";

            var result = HtmlUtilities.ConvertToPlainText(html);

            Assert.Contains("Hello world", result);
            Assert.DoesNotContain("badCode", result);
            Assert.DoesNotContain("color:red", result);
        }

        [Fact]
        public void ConvertToPlainText_ParagraphsBecomeNewlines()
        {
            var result = HtmlUtilities.ConvertToPlainText("<p>one</p><p>two</p>");

            Assert.Equal("one\r\ntwo", result.Trim());
        }

        [Fact]
        public void ConvertToPlainText_DecodesHtmlEntities()
        {
            var result = HtmlUtilities.ConvertToPlainText("<p>Tom &amp; Jerry</p>");

            Assert.Equal("Tom & Jerry", result.Trim());
        }

        [Fact]
        public void FindImageSource_ReturnsFirstImageInDocumentOrder()
        {
            var html = "<div><img src=\"https://x.test/1.png\">" +
                       "<img src=\"https://x.test/2.png\"></div>";

            var result = HtmlUtilities.FindImageSource(html);

            Assert.Equal("https://x.test/1.png", result);
        }

        [Fact]
        public void FindImageSource_NoImages_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, HtmlUtilities.FindImageSource("<p>text only</p>"));
        }

        [Fact]
        public void FindImageSource_ImgWithoutSrc_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, HtmlUtilities.FindImageSource("<img alt=\"missing\">"));
        }
    }
}
