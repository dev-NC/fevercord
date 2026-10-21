using FeverCord.Tests.Fakes;
using FeverCordSvc.Features.Rss;
using FeverCordSvc.Infrastructure;
using System.Net;

namespace FeverCord.Tests.Features.Rss
{
    public class RssFeedServiceTests
    {
        private const string ApiKey = "test-key";

        [Fact]
        public async Task FetchUnreadItemIdsAsync_SplitsCommaSeparatedIds()
        {
            var (service, _) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse(
                    "{\"auth\":1,\"unread_item_ids\":\"101,102,103\"}"));

            var ids = await service.FetchUnreadItemIdsAsync();

            Assert.Equal(new[] { "101", "102", "103" }, ids);
        }

        [Fact]
        public async Task FetchUnreadItemIdsAsync_EmptyIdsString_ReturnsEmpty()
        {
            var (service, _) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse(
                    "{\"auth\":1,\"unread_item_ids\":\"\"}"));

            var ids = await service.FetchUnreadItemIdsAsync();

            Assert.Empty(ids);
        }

        [Fact]
        public async Task FetchUnreadItemIdsAsync_MissingIdsField_ReturnsEmpty()
        {
            // The DTO defaults unread_item_ids to string.Empty, so an absent
            // field must degrade to "no unread items", not a null-reference crash.
            var (service, _) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse("{\"auth\":1}"));

            var ids = await service.FetchUnreadItemIdsAsync();

            Assert.Empty(ids);
        }

        [Fact]
        public async Task FetchUnreadItemIdsAsync_AuthZero_ThrowsHttpResponseException()
        {
            var (service, _) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse("{\"auth\":0}"));

            var ex = await Assert.ThrowsAsync<HttpResponseException>(
                () => service.FetchUnreadItemIdsAsync());

            Assert.Contains("Authentication error", ex.Message);
        }

        [Fact]
        public async Task FetchUnreadItemIdsAsync_NullBody_ThrowsHttpResponseException()
        {
            var (service, _) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse("null"));

            var ex = await Assert.ThrowsAsync<HttpResponseException>(
                () => service.FetchUnreadItemIdsAsync());

            Assert.Contains("content is null", ex.Message);
        }

        [Fact]
        public async Task FetchUnreadItemIdsAsync_PostsApiKeyInFormContent()
        {
            var (service, handler) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse(
                    "{\"auth\":1,\"unread_item_ids\":\"\"}"));

            await service.FetchUnreadItemIdsAsync();

            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains($"api_key={ApiKey}", body);
        }

        [Fact]
        public async Task FetchItemsByIdsAsync_MoreIdsThanLimit_ThrowsArgumentException()
        {
            var (service, handler) = CreateService(fetchLimit: 2);
            var ids = new[] { "1", "2", "3" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => service.FetchItemsByIdsAsync(ids));

            Assert.Contains("cannot exceed", ex.Message);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task FetchItemsByIdsAsync_EmptyIds_ThrowsArgumentException()
        {
            var (service, _) = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.FetchItemsByIdsAsync(Array.Empty<string>()));
        }

        [Fact]
        public async Task FetchItemsByIdsAsync_ReturnsItemsAndRequestsWithIdsParam()
        {
            var (service, handler) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse(
                    "{\"auth\":1,\"total_items\":1,\"items\":[" +
                    "{\"id\":\"1\",\"feed_id\":2,\"title\":\"T\",\"author\":\"A\"," +
                    "\"html\":\"<p>x</p>\",\"url\":\"https://e.test/1\"," +
                    "\"is_saved\":0,\"is_read\":0,\"created_on_time\":123}]}"));

            var items = await service.FetchItemsByIdsAsync(new[] { "1", "2" });

            Assert.Equal("1", Assert.Single(items).Id);
            var uri = Assert.Single(handler.Requests).RequestUri!.ToString();
            Assert.Contains("with_ids=1,2", uri);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task MarkItemAsReadByIdAsync_NullOrEmpty_ThrowsArgumentException(string? id)
        {
            var (service, _) = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.MarkItemAsReadByIdAsync(id!));
        }

        [Fact]
        public async Task MarkItemAsReadByIdAsync_PostsToMarkEndpoint()
        {
            var (service, handler) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse("{\"auth\":1}"));

            await service.MarkItemAsReadByIdAsync("42");

            var uri = Assert.Single(handler.Requests).RequestUri!.ToString();
            Assert.Contains("mark=item&as=read&id=42", uri);
        }

        [Fact]
        public async Task AnyRequest_NonSuccessStatusCode_ThrowsHttpRequestException()
        {
            var (service, _) = CreateService(
                _ => StubHttpMessageHandler.JsonResponse("oops", HttpStatusCode.InternalServerError));

            await Assert.ThrowsAsync<HttpRequestException>(
                () => service.FetchUnreadItemIdsAsync());
        }

        private static (RssFeedService Service, StubHttpMessageHandler Handler) CreateService(
            Func<HttpRequestMessage, HttpResponseMessage>? responder = null,
            int fetchLimit = 50)
        {
            var handler = new StubHttpMessageHandler(
                responder ?? (_ => StubHttpMessageHandler.JsonResponse("{}")));
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://fake.test/"),
            };
            var service = new RssFeedService(
                new FeverSettings { RssItemFetchLimit = fetchLimit },
                new FeverSecrets
                {
                    RssApiKey = ApiKey,
                    RssBaseAddress = "http://fake.test/",
                },
                client);

            return (service, handler);
        }
    }
}
