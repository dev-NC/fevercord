# Build and publish in a single stage to keep the layer graph small.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["FeverCord.csproj", "./"]
RUN dotnet restore "FeverCord.csproj"

COPY . .
RUN dotnet publish "FeverCord.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image. Configuration comes from environment variables at startup
# (see README), e.g. FeverSecrets__RssApiKey, DiscordSecrets__DiscordWebhook.
# The app validates required config at startup and fails fast with a clear error.
FROM mcr.microsoft.com/dotnet/runtime:8.0
USER $APP_UID
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FeverCord.dll"]
