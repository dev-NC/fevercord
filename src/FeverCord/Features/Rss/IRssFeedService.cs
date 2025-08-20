using FeverCordSvc.Features.Rss.Content;

namespace FeverCordSvc.Features.Rss
{
    public interface IRssFeedService
    {
        Task<IEnumerable<string>> FetchUnreadItemIdsAsync();

        Task<IEnumerable<ItemContent>> FetchItemsByIdsAsync(IEnumerable<string> ids);

        Task MarkItemAsReadByIdAsync(string id);
    }
}
