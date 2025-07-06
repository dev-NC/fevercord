using System.Text.Json.Serialization;

namespace FeverCordSvc.Features.Discord.Content
{
    public class AuthorContent
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
