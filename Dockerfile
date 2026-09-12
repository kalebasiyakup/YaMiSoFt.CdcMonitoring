FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CdcMonitoring.slnx ./
COPY src/CdcMonitoring.Domain/*.csproj src/CdcMonitoring.Domain/
COPY src/CdcMonitoring.Application/*.csproj src/CdcMonitoring.Application/
COPY src/CdcMonitoring.Infrastructure/*.csproj src/CdcMonitoring.Infrastructure/
COPY src/CdcMonitoring.Web/*.csproj src/CdcMonitoring.Web/
RUN dotnet restore src/CdcMonitoring.Web/CdcMonitoring.Web.csproj

COPY src/ src/
RUN dotnet publish src/CdcMonitoring.Web/CdcMonitoring.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CdcMonitoring.Web.dll"]
