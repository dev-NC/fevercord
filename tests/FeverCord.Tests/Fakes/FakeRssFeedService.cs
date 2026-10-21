using FeverCordSvc.Features.Rss;
using FeverCordSvc.Features.Rss.Content;

namespace FeverCord.Tests.Fakes
{
    internal sealed class FakeRssFeedService : IRssFeedService
    {
        public List<string> UnreadIdsToReturn { get; set; } = [];

        public List<List<string>> FetchBatchCalls { get; } = [];

        public List<string> MarkedReadIds { get; } = [];

        public Func<IEnumerable<string>, IEnumerable<ItemContent>>? FetchResponder { get; set; }

        public Func<IEnumerable<string>, Exception?>? FetchFault { get; set; }

        public Func<string, Exception?>? MarkReadFault { get; set; }

        public Task<IEnumerable<string>> FetchUnreadItemIdsAsync()
        {
            return Task.FromResult<IEnumerable<string>>(UnreadIdsToReturn);
        }

        public Task<IEnumerable<ItemContent>> FetchItemsByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            FetchBatchCalls.Add(idList);

            var fault = FetchFault?.Invoke(idList);
            if (fault != null)
            {
                throw fault;
            }

            var items = FetchResponder != null
                ? FetchResponder(idList)
                : idList.Select(id => TestData.Item(id));

            return Task.FromResult(items);
        }

        public Task MarkItemAsReadByIdAsync(string id)
        {
            var fault = MarkReadFault?.Invoke(id);
            if (fault != null)
            {
                throw fault;
            }

            MarkedReadIds.Add(id);
            return Task.CompletedTask;
        }
    }
}
