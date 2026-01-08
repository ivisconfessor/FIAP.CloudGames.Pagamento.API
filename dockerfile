FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FIAP.CloudGames.Pagamento.API.csproj", "src/"]
RUN dotnet restore "src/FIAP.CloudGames.Pagamento.API.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "FIAP.CloudGames.Pagamento.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FIAP.CloudGames.Pagamento.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FIAP.CloudGames.Pagamento.API.dll"]
