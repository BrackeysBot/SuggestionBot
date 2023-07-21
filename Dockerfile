FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SuggestionBot/SuggestionBot.csproj", "SuggestionBot/"]
RUN dotnet restore "SuggestionBot/SuggestionBot.csproj"
COPY . .
WORKDIR "/src/SuggestionBot"
RUN dotnet build "SuggestionBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SuggestionBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SuggestionBot.dll"]
