using FeverCordSvc.Features.Rss.Content;

namespace FeverCord.Tests
{
    internal static class TestData
    {
        public static ItemContent Item(
            string id,
            string title = "",
            string html = "",
            string author = "")
        {
            return new ItemContent
            {
                Id = id,
                Title = title,
                Html = html,
                Author = author,
                Url = $"https://example.test/{id}",
            };
        }

        public static string LongText(int length) => new('a', length);
    }
}
