# AGENTS.md

FeverCord: .NET 8 worker service that polls a Fever (`fever.php`) RSS reader API for unread
items, converts them to Discord embeds, and posts them to a Discord webhook on a Quartz cron
schedule.

## Commands

- Build: `dotnet build FeverCord.sln`
- Test: `dotnet test` (xUnit in `tests/FeverCord.Tests`; 56 tests covering job semantics,
  pure helpers, HTTP contracts, and config validation)
- Run: `dotnet run --project src/FeverCord`
- Verification order: build, then test. CI: GitHub Actions (`.github/workflows/ci.yml`)
  runs restore, a Release build with warnings-as-errors, and tests on ubuntu for pushes
  to master/develop and all PRs.

## Layout & naming

- `src/FeverCord` — the app. Entrypoint `Program.cs` wires everything through
  `ServiceExtensions.AddFeverCord` (DI, HTTP clients) and `AddJobScheduler` (Quartz jobs).
- `tests/FeverCord.Tests` — xUnit project referencing the app.
- Naming trap: project/assembly/solution are `FeverCord`, but all namespaces and the Windows
  service name are `FeverCordSvc`. Do not "fix" this; follow the existing `FeverCordSvc.*`
  namespaces.

## Config & secrets

- Both of these sections are required at startup; `AddFeverCord` validates section
  presence, non-empty secret values, and absolute URLs, and throws
  `InvalidOperationException` naming the offending key:
  - `FeverSecrets`: `RssApiKey`, `RssBaseAddress` (the Fever server base URL)
  - `DiscordSecrets`: `DiscordWebhook` (full webhook URL, used as HttpClient BaseAddress)
- Dev secret storage: user-secrets (ID in `src/FeverCord/FeverCord.csproj`) or env vars like
  `FeverSecrets__RssApiKey`.
- Tunables live in `appsettings.json`: `Fever:RssItemFetchLimit`, `Discord:*`
  (rate limit/window, embed limit/color), and `Quartz:*` cron schedules.

## Quartz jobs

- Jobs are registered via `ServiceExtensions.AddJobAndTrigger<T>`, which reads the cron from
  config key `Quartz:<JobClassName>` and throws at startup if the key is absent. Adding a job
  requires a matching `appsettings.json` entry.
- `UpdateWebhookJob` (the only job) runs every 5 min and is `[DisallowConcurrentExecution]`.
  Flow: fetch unread ids -> fetch items in batches of `RssItemFetchLimit` -> build embeds ->
  post in batches of `DiscordEmbedLimit` (max 6000 chars per message) -> mark items read only
  after a successful post. Delivery is at-least-once: retries/failures can duplicate posts.

## Gotchas

- Docker: `src/FeverCord/Dockerfile` (context = `src/FeverCord`), non-root runtime user,
  config via env vars (`FeverSecrets__*`, `DiscordSecrets__*`). CI builds the image on
  every push; there is no docker-compose file.
- RSS API responses are authenticated via `api_key` (form POST); a response with `auth == 0`
  raises `HttpResponseException` (`BaseRssContent.Auth`).
- Embeds are derived from item HTML through `HtmlUtilities` (HtmlAgilityPack): HTML -> plain
  text, first image extracted, title/description cut to 256/4096 chars.
