FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /SunTgBot

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /SunTgBot
COPY --from=build-env /SunTgBot/SunTgBot/bin/Release/net8.0/publish .

ENTRYPOINT ["dotnet", "SunTgBot.dll"]
