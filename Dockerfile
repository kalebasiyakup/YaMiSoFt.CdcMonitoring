FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CdcMonitoring.slnx ./
COPY src/CdcMonitoring.Domain/*.csproj src/CdcMonitoring.Domain/
COPY src/CdcMonitoring.Application/*.csproj src/CdcMonitoring.Application/
COPY src/CdcMonitoring.Infrastructure/*.csproj src/CdcMonitoring.Infrastructure/
COPY src/CdcMonitoring.Web/*.csproj src/CdcMonitoring.Web/
RUN dotnet restore src/CdcMonitoring.Web/CdcMonitoring.Web.csproj

COPY src/ src/
# --no-restore KULLANILMAZ: wwwroot, ilk "dotnet restore" katmanında henüz kopyalanmamış
# olduğundan (yalnızca *.csproj dosyaları vardı), Blazor'un _framework/blazor.web.js
# dosyasını sağlayan örtük Microsoft.AspNetCore.App.Internal.Assets paket referansı o
# restore sırasında hiç eklenmiyor/indirilmiyor. --no-restore ile publish edilirse bu
# eksiklik telafi edilmez ve blazor.web.js 404 döner (SignalR circuit hiç kurulamaz).
# Burada restore paket indirmesi zaten global NuGet cache'inden geldiği için ek maliyet
# küçüktür; asıl amaç wwwroot mevcutken restore/evaluate'in yeniden çalışmasıdır.
RUN dotnet publish src/CdcMonitoring.Web/CdcMonitoring.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql, GSS/Kerberos kimlik doğrulama görüşmesini deneyebilmek için
# libgssapi-krb5-2'yi native olarak yüklemeye çalışır; bu, aspnet:10.0 taban
# imajında (Debian slim) varsayılan olarak yoktur. Eksik olduğunda Npgsql her
# bağlantıda bir kerede başarısız bir yükleme denemesi loglar (fonksiyonel
# olarak SCRAM/plain auth'a geri döner, ama gürültülü hata + gecikme yaratır
# ve gerçek bir GSS/Kerberos ortamında sert hataya dönüşür).
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CdcMonitoring.Web.dll"]
