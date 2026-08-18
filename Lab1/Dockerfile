FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY RetroGameExchange.csproj .
RUN dotnet restore RetroGameExchange.csproj

COPY . .
RUN dotnet publish RetroGameExchange.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStringDefault="Data Source=/data/retrogameexchange.db;Cache=Shared"
ENV JwtKey=RetroGameExchangeDevSecretKey_ChangeInProd_32+
ENV JwtIssuer=RetroGameExchange
ENV JwtAudience=RetroGameExchange
ENV JwtExpiresMinutes=120

EXPOSE 8080
ENTRYPOINT ["dotnet", "RetroGameExchange.dll"]
