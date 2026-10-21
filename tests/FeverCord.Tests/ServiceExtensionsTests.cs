using FeverCordSvc;
using FeverCordSvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace FeverCord.Tests
{
    public class ServiceExtensionsTests
    {
        [Fact]
        public void AddFeverCord_MissingSecretsSection_ThrowsWithHelpfulMessage()
        {
            var config = NewConfig(new Dictionary<string, string?>
            {
                ["Fever:RssItemFetchLimit"] = "50",
                ["Discord:DiscordEmbedLimit"] = "10",
                ["Discord:DiscordRequestLimit"] = "30",
                ["Discord:DiscordRequestWindow"] = "60",
                ["Discord:DiscordEmbedColor"] = "12058620",
            });
            var services = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(
                () => services.AddFeverCord(config));

            // The message must name the missing section so the failure is actionable.
            Assert.Contains("FeverSecrets", ex.Message);
        }

        [Fact]
        public void AddFeverCord_EmptyRssApiKey_Throws()
        {
            var values = ValidConfig();
            values["FeverSecrets:RssApiKey"] = "";
            var services = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(
                () => services.AddFeverCord(NewConfig(values)));

            Assert.Contains("RssApiKey", ex.Message);
        }

        [Fact]
        public void AddFeverCord_RelativeRssBaseAddress_Throws()
        {
            var values = ValidConfig();
            values["FeverSecrets:RssBaseAddress"] = "freshrss.test";
            var services = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(
                () => services.AddFeverCord(NewConfig(values)));

            Assert.Contains("absolute URL", ex.Message);
        }

        [Fact]
        public void AddFeverCord_InvalidWebhookUrl_Throws()
        {
            var values = ValidConfig();
            values["DiscordSecrets:DiscordWebhook"] = "not-a-url";
            var services = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(
                () => services.AddFeverCord(NewConfig(values)));

            Assert.Contains("DiscordWebhook", ex.Message);
        }

        [Fact]
        public void AddFeverCord_ValidConfig_RegistersSettingsSingletons()
        {
            var services = new ServiceCollection();

            services.AddFeverCord(NewConfig(ValidConfig()));

            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<FeverSettings>());
            Assert.NotNull(provider.GetService<FeverSecrets>());
            Assert.NotNull(provider.GetService<DiscordSettings>());
            Assert.NotNull(provider.GetService<DiscordSecrets>());
        }

        [Fact]
        public void AddJobAndTrigger_MissingCronKey_Throws()
        {
            var services = new ServiceCollection();
            var config = NewConfig(new Dictionary<string, string?>());

            // Quartz invokes the configuration callback eagerly, so the missing
            // schedule surfaces here rather than at first job execution.
            Assert.Throws<InvalidOperationException>(
                () => services.AddQuartz(
                    quartz => quartz.AddJobAndTrigger<UpdateWebhookJob>(config)));
        }

        private static ConfigurationManager NewConfig(
            Dictionary<string, string?> values)
        {
            var config = new ConfigurationManager();
            config.AddInMemoryCollection(values);
            return config;
        }

        private static Dictionary<string, string?> ValidConfig()
        {
            return new Dictionary<string, string?>
            {
                ["Fever:RssItemFetchLimit"] = "50",
                ["Discord:DiscordEmbedLimit"] = "10",
                ["Discord:DiscordRequestLimit"] = "30",
                ["Discord:DiscordRequestWindow"] = "60",
                ["Discord:DiscordEmbedColor"] = "12058620",
                ["FeverSecrets:RssApiKey"] = "key",
                ["FeverSecrets:RssBaseAddress"] = "https://freshrss.test/",
                ["DiscordSecrets:DiscordWebhook"] = "https://discord.test/api/webhooks/1/abc",
            };
        }
    }
}
