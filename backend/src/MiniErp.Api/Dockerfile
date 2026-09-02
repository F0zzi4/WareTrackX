FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace

COPY MiniErp.slnx ./
COPY NuGet.config ./
COPY src/MiniErp.Api/MiniErp.Api.csproj src/MiniErp.Api/
RUN dotnet restore src/MiniErp.Api/MiniErp.Api.csproj

COPY src/MiniErp.Api src/MiniErp.Api
RUN dotnet publish src/MiniErp.Api/MiniErp.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MiniErp.Api.dll"]
