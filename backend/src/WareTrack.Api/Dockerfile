FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace

COPY WareTrack.slnx ./
COPY NuGet.config ./
COPY src/WareTrack.Api/WareTrack.Api.csproj src/WareTrack.Api/
RUN dotnet restore src/WareTrack.Api/WareTrack.Api.csproj

COPY src/WareTrack.Api src/WareTrack.Api
RUN dotnet publish src/WareTrack.Api/WareTrack.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WareTrack.Api.dll"]
