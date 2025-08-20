using FeverCordSvc.Features.Discord.Content;
using FeverCordSvc.Infrastructure;

namespace FeverCordSvc.Features.Rss.Content
{
    public static class ItemContentExtensions
    {
        public static EmbedContent ToEmbed(this ItemContent item, int? color = null)
        {
            const int TitleCharLimit = 256;
            const int DescriptionCharLimit = 4096;

            var description = HtmlUtilities.ConvertToPlainText(item.Html);
            description = HtmlUtilities.NormalizeWhitespace(description);
            description = HtmlUtilities.Cut(description, DescriptionCharLimit);

            var image = HtmlUtilities.FindImageSource(item.Html);

            return new EmbedContent
            {
                Id = item.Id,
                Url = item.Url,
                Title = HtmlUtilities.Cut(item.Title, TitleCharLimit),
                Description = description,
                Color = color,
                Author = new AuthorContent { Name = item.Author },
                Image = new ImageContent { Url = image }
            };
        }

    }
}
