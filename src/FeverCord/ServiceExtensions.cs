using FeverCordSvc.Features.Discord;
using FeverCordSvc.Features.Rss;
using FeverCordSvc.Infrastructure;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.RateLimiting;
using Polly.Timeout;
using Quartz;
using System.Net;
using System.Threading.RateLimiting;

namespace FeverCordSvc
{
    public static class ServiceExtensions
    {
        public static ResiliencePipelineBuilder<HttpResponseMessage> AddDiscordPipeline(
            this ResiliencePipelineBuilder<HttpResponseMessage> builder,
            DiscordPipelineConfiguration configuration)
        {
            builder.AddTimeout(new HttpTimeoutStrategyOptions
            {
                Name = "discord-totalrequesttimeout",
                Timeout = TimeSpan.FromSeconds(30)
            });

            builder.AddRetry(new HttpRetryStrategyOptions
            {
                Name = "discord-retry",
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 3,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2),
                ShouldRetryAfterHeader = true,
                ShouldHandle = args => args.Outcome switch
                {
                    { Exception: RateLimiterRejectedException } => PredicateResult.True(),
                    { Exception: TimeoutRejectedException } => PredicateResult.True(),
                    { Result: HttpResponseMessage response } when
                        response.StatusCode == HttpStatusCode.TooManyRequests => PredicateResult.True(),
                    _ => PredicateResult.False()
                }
            });

            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                Name = "discord-circuitbreaker",
                FailureRatio = 0.2,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(5),
                ShouldHandle = args => args.Outcome switch
                {
                    { Exception: HttpRequestException } => PredicateResult.True(),
                    { Result: HttpResponseMessage response } when
                        (int)response.StatusCode >= 500 => PredicateResult.True(),
                    { Result: HttpResponseMessage response } when
                        response.StatusCode == HttpStatusCode.RequestTimeout => PredicateResult.True(),
                    _ => PredicateResult.False()
                }
            });

            builder.AddTimeout(new HttpTimeoutStrategyOptions
            {
                Name = "discord-attempttimeout",
                Timeout = TimeSpan.FromSeconds(10)
            });

            var rateLimiter = new FixedWindowRateLimiter(
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.DiscordRequestLimit,
                    Window = TimeSpan.FromSeconds(configuration.DiscordRequestWindow)
                });

            builder.AddRateLimiter(new HttpRateLimiterStrategyOptions
            {
                Name = "discord-ratelimiter",
                RateLimiter = args => rateLimiter.
                    AcquireAsync(cancellationToken: args.Context.CancellationToken)
            });

            return builder;
        }

        public static IServiceCollection AddFeverCord(
                    this IServiceCollection services,
            IConfigurationManager configuration)
        {
            var feverSettings = configuration
                .GetSection("Fever")
                .Get<FeverSettings>()
                ?? throw new InvalidOperationException(
                    "Missing required configuration section 'Fever'. See README for setup.");
            services.AddSingleton(feverSettings);

            var feverSecrets = configuration
                .GetSection("FeverSecrets")
                .Get<FeverSecrets>()
                ?? throw new InvalidOperationException(
                    "Missing required configuration section 'FeverSecrets'. See README for setup.");
            ValidateFeverSecrets(feverSecrets);
            services.AddSingleton(feverSecrets);

            var discordSettings = configuration
                .GetSection("Discord")
                .Get<DiscordSettings>()
                ?? throw new InvalidOperationException(
                    "Missing required configuration section 'Discord'. See README for setup.");
            services.AddSingleton(discordSettings);

            var discordSecrets = configuration
                .GetSection("DiscordSecrets")
                .Get<DiscordSecrets>()
                ?? throw new InvalidOperationException(
                    "Missing required configuration section 'DiscordSecrets'. See README for setup.");
            ValidateDiscordSecrets(discordSecrets);
            services.AddSingleton(discordSecrets);

            services.AddFeverCordHttpClients(new FeverCordHttpClientConfiguration
            {
                RssBaseAddress = feverSecrets.RssBaseAddress,
                DiscordWebhook = discordSecrets.DiscordWebhook,
                DiscordRequestLimit = discordSettings.DiscordRequestLimit,
                DiscordRequestWindow = discordSettings.DiscordRequestWindow
            });

            services.AddWindowsService(options =>
            {
                options.ServiceName = "FeverCordSvc";
            });

            return services;
        }

        private static void ValidateFeverSecrets(FeverSecrets secrets)
        {
            if (string.IsNullOrWhiteSpace(secrets.RssApiKey))
            {
                throw new InvalidOperationException(
                    "'FeverSecrets:RssApiKey' is required. See README for setup.");
            }

            if (string.IsNullOrWhiteSpace(secrets.RssBaseAddress))
            {
                throw new InvalidOperationException(
                    "'FeverSecrets:RssBaseAddress' is required. See README for setup.");
            }

            if (!Uri.TryCreate(secrets.RssBaseAddress, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    "'FeverSecrets:RssBaseAddress' must be an absolute URL " +
                    "(e.g. https://freshrss.example.net/).");
            }
        }

        private static void ValidateDiscordSecrets(DiscordSecrets secrets)
        {
            if (string.IsNullOrWhiteSpace(secrets.DiscordWebhook))
            {
                throw new InvalidOperationException(
                    "'DiscordSecrets:DiscordWebhook' is required. See README for setup.");
            }

            if (!Uri.TryCreate(secrets.DiscordWebhook, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    "'DiscordSecrets:DiscordWebhook' must be an absolute URL " +
                    "(e.g. https://discord.com/api/webhooks/...).");
            }
        }

        public static IServiceCollection AddFeverCordHttpClients(
            this IServiceCollection services,
            FeverCordHttpClientConfiguration configuration)
        {
            services.AddHttpClient<IRssFeedService, RssFeedService>(client =>
            {
                client.BaseAddress = new Uri(configuration.RssBaseAddress);
            })
                .AddStandardResilienceHandler();

            services.AddHttpClient<IDiscordWebhookService, DiscordWebhookService>(client =>
            {
                client.BaseAddress = new Uri(configuration.DiscordWebhook);
            })
                .AddResilienceHandler("discord", builder =>
                {
                    builder.AddDiscordPipeline(new DiscordPipelineConfiguration
                    {
                        DiscordRequestLimit = configuration.DiscordRequestLimit,
                        DiscordRequestWindow = configuration.DiscordRequestWindow
                    });
                });

            return services;
        }

        public static void AddJobAndTrigger<T>(
            this IServiceCollectionQuartzConfigurator quartz,
            IConfiguration configuration)
            where T : IJob
        {
            // Use the name of the IJob as the appsettings.json key
            string jobName = typeof(T).Name;

            // Try and load the schedule from configuration
            var configKey = $"Quartz:{jobName}";
            var cronSchedule = configuration[configKey];

            // Some minor validation
            if (string.IsNullOrEmpty(cronSchedule))
            {
                throw new InvalidOperationException(
                    $"No Quartz.NET Cron schedule found for job in configuration at {configKey}");
            }

            // register the job as before
            var jobKey = new JobKey(jobName);
            quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));

            quartz.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity(jobName + "-trigger")
                .WithCronSchedule(cronSchedule)); // use the schedule from configuration
        }

        public static IServiceCollection AddJobScheduler(
                    this IServiceCollection services,
            IConfigurationManager configuration)
        {
            services.AddQuartz(quartz =>
            {
                quartz.AddJobAndTrigger<UpdateWebhookJob>(configuration);
            });

            services.AddQuartzHostedService(
                quartz => quartz.WaitForJobsToComplete = true);

            return services;
        }

        //  Code taken from:
        //  https://andrewlock.net/using-quartz-net-with-asp-net-core-and-worker-services/
        //  License:
        //  The MIT License (MIT)

        //  Copyright (c) 2016 andrewlock
        //
        //  Permission is hereby granted, free of charge, to any person obtaining a copy
        //  of this software and associated documentation files (the "Software"), to deal
        //  in the Software without restriction, including without limitation the rights
        //  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        //  copies of the Software, and to permit persons to whom the Software is
        //  furnished to do so, subject to the following conditions:
        //
        //  The above copyright notice and this permission notice shall be included in all
        //  copies or substantial portions of the Software.
        //
        //  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        //  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        //  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        //  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        //  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        //  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        //  SOFTWARE.
    }
}