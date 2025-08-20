using FeverCordSvc.Features.Rss.Content;
using FeverCordSvc.Infrastructure;
using System.Net.Http.Json;

namespace FeverCordSvc.Features.Rss
{
    public class RssFeedService : IRssFeedService
    {
        private readonly FeverSettings _settings;
        private readonly FeverSecrets _secrets;
        private readonly HttpClient _httpClient;
        private readonly FormUrlEncodedContent _authorizationContent;

        public RssFeedService(FeverSettings settings, FeverSecrets secrets, HttpClient httpClient)
        {
            _settings = settings;
            _secrets = secrets;
            _httpClient = httpClient;
            _authorizationContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("api_key", _secrets.RssApiKey)
            });
        }

        public async Task<IEnumerable<string>> FetchUnreadItemIdsAsync()
        {
            var response = await _httpClient.PostAsync(
                "api/fever.php?api&unread_item_ids",
                _authorizationContent);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<UnreadItemListContent>();
            EnsureValidRssResponseContent(response, content);

            if (string.IsNullOrEmpty(content.UnreadItemIds))
            {
                return Enumerable.Empty<string>();
            }

            return content.UnreadItemIds.Split(',');
        }

        public async Task<IEnumerable<ItemContent>> FetchItemsByIdsAsync(IEnumerable<string> ids)
        {
            if (ids.Count() > _settings.RssItemFetchLimit)
            {
                throw new ArgumentException(
                    $"The number of ids cannot exceed the limit: {_settings.RssItemFetchLimit}");
            }

            if (!ids.Any())
            {
                throw new ArgumentException("The number of ids cannot be 0.");
            }

            var strIds = string.Join(',', ids);
            var response = await _httpClient.PostAsync(
                $"api/fever.php?api&items&with_ids={strIds}",
                _authorizationContent);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<ItemListContent>();
            EnsureValidRssResponseContent(response, content);

            return content.Items;
        }

        public async Task MarkItemAsReadByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("The id cannot be null or empty.");
            }

            var response = await _httpClient.PostAsync(
                $"api/fever.php?api&mark=item&as=read&id={id}",
                _authorizationContent);

            response.EnsureSuccessStatusCode();
        }

        private void EnsureValidRssResponseContent(HttpResponseMessage response, BaseRssContent? content)
        {
            if (content == null)
            {
                throw new HttpResponseException(
                    $"Http response content is null: {response.ReasonPhrase}");
            }

            if (content.Auth == 0)
            {
                response.Content.Dispose();
                throw new HttpResponseException(
                    $"Authentication error: {response.ReasonPhrase}");
            }
        }
    }
}
