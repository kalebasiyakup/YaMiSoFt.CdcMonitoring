# CDC Monitoring (CDC-MON-2026)

PostgreSQL örnekleri arasındaki mantıksal replikasyon (publication/subscription) tabanlı CDC (Change Data Capture) ilişkilerini kayıt altına alan, keşfeden, izleyen ve sağlıksız durumlarda e-posta ile bildiren **tamamen salt-gözlem (read-only)** bir .NET uygulaması.

Gereksinimlerin tam listesi için bkz. [`CDC_Monitoring_BRD_v0.3.md`](./CDC_Monitoring_BRD_v0.3.md).

## Problem

Şirketin PostgreSQL örnekleri arasındaki CDC ilişkileri (Kafka/Debezium değil, doğrudan PostgreSQL'in yerleşik mantıksal replikasyonu ile) merkezi olarak kayıtlı ve görünür değildi:

- Hangi PostgreSQL örneklerinin var olduğuna dair merkezi bir envanter yoktu.
- Hangi bağlantının hangi bağlantıya CDC ile bağımlı olduğu tek bir yerden görülemiyordu.
- Bir replication slot'un sessizce inaktif kalması veya bir subscription'ın hataya düşmesi fark edilmiyordu — bu da disk dolması (WAL birikimi) veya hedefin güncel olmaması gibi sonuçlar doğurabiliyordu.

## Bu uygulama ne yapar

- **Bağlantı Defteri:** PostgreSQL bağlantılarını (host/port/db/kullanıcı/parola/ortam etiketi) kaydeder; parolalar uygulama içi Data Protection ile şifreli saklanır, hiçbir ekranda geri gösterilmez.
- **CDC İlişkileri:** Kayıtlı bağlantılar arasında `pg_publication`, `pg_subscription`, `pg_replication_slots` gibi sistem görünümlerini salt-okuma ile tarayarak CDC ilişkilerini otomatik keşfeder; kullanıcı bu önerileri onaylar/reddeder ya da elle tanımlar.
- **Topoloji:** Kayıtlı bağlantıları ve aralarındaki CDC ilişkilerini canlı güncellenen bir graf ekranında gösterir (kaynak → hedef, sağlık durumu, lag).
- **Alarmlar:** Slot inaktifliği, WAL kritik durumu, subscription hatası, sürekli artan lag ve ardışık health check hatası gibi durumları izler; eşik aşıldığında **e-posta** ile bildirir (anti-flap: aynı sorun için tekrar tekrar göndermez, durum düzelince otomatik kapatır).
- **Veri Tutarlılık Kontrolü:** Onaylanmış CDC ilişkileri için periyodik olarak kaynak-hedef satır sayısı/checksum karşılaştırması yapar, tutarsızlık bulunursa e-posta ile raporlar.
- **Ayarlar:** Tüm çalışma parametreleri (tarama sıklıkları, alarm eşikleri, SMTP bilgileri) veritabanında saklanır ve `/settings` ekranından, **yeniden başlatma gerekmeden** yönetilir.

### Salt-gözlem garantisi

Uygulama izlediği hiçbir PostgreSQL örneğinde publication, subscription veya replication slot **oluşturmaz, silmez veya değiştirmez**. Tüm sorgular sabit, parametreli `SELECT` ifadeleridir (bkz. `CdcMonitoring.UnitTests/Postgres/ReadOnlyGuardTests.cs`). Kritik bir durum tespit edildiğinde uygulamanın tek çıktısı e-posta bildirimidir; düzeltici aksiyon her zaman ilgili ekip tarafından manuel yapılır.

## Mimari

Katmanlı (Domain / Application / Infrastructure / Web) bir .NET çözümü:

```
src/
  CdcMonitoring.Domain/          Entity'ler, enum'lar (framework bağımsız)
  CdcMonitoring.Application/     İş servisleri, arayüzler (Connections, CdcDiscovery,
                                  Alerting, Reconciliation, Settings, HealthChecks)
  CdcMonitoring.Infrastructure/  EF Core + PostgreSQL, Npgsql tabanlı inspector,
                                  Quartz.NET job'ları, Data Protection, e-posta (MailKit),
                                  Prometheus metrikleri
  CdcMonitoring.Web/             Blazor Server arayüzü (SignalR ile canlı güncelleme)
tests/
  CdcMonitoring.UnitTests/       Servis/iş kuralı testleri + salt-okuma guard testleri
  CdcMonitoring.IntegrationTests/
deploy/helm/cdc-monitoring/      Kubernetes/Helm chart
```

### Zamanlama modeli

Dört arka plan işi (bağlantı health check, CDC keşfi, alarm değerlendirme, veri tutarlılık kontrolü) tek bir Quartz job'u (`SchedulerTickJob`) tarafından, sabit kısa aralıklarla (15 sn) "yoklanır". Her işin gerçek çalışma sıklığı veritabanındaki `SystemSettings` tablosundan okunur; bir işin çalışma hakkı, çoklu replika (NFR-05) güvenliği için **atomik bir koşullu UPDATE** ile "claim" edilir — aynı iş iki replikada birden çalışamaz.

## Teknoloji Yığını

- .NET 8, Blazor Server (Interactive Server render mode)
- EF Core 8 + Npgsql (kendi metadata şeması) — izlenen örneklere ise ham Npgsql ile salt-okuma bağlanılır
- Quartz.NET (opsiyonel clustered PostgreSQL job store, NFR-05)
- ASP.NET Core Data Protection (parola/SMTP şifreleme)
- MailKit (SMTP e-posta gönderimi)
- prometheus-net (`/metrics` endpoint'i)
- Serilog (JSON, ELK uyumlu loglama)
- vis-network (self-hosted, CDN bağımlılığı yok) — topoloji grafiği
- xUnit — birim testleri

## Local Geliştirme ve Test

Docker Compose ile gerçek PostgreSQL logical replication üzerinden uçtan uca test edilebilir bir ortam:

```bash
# Metadata DB + kaynak/hedef Postgres (wal_level=logical) + app + Mailpit'i ayağa kaldır
docker compose -f docker-compose.local.yml up -d --build

# Test amaçlı publication/subscription kur (DB ekibinin gerçek ortamda yapacağı işi simüle eder)
./scripts/setup-local-cdc-test.sh
```

Sonra `http://localhost:5299` üzerinden bağlantıları kaydedip (Host: `pub-db` / `sub-db`) CDC ilişkisinin otomatik keşfedildiğini, `/topology` ekranında göründüğünü ve e-posta bildirimlerinin `http://localhost:8025` (Mailpit) üzerinden gerçek SMTP protokolüyle geldiğini gözlemleyebilirsiniz.

```bash
docker compose -f docker-compose.local.yml down -v   # temizlik
```

### Testler

```bash
dotnet test tests/CdcMonitoring.UnitTests/CdcMonitoring.UnitTests.csproj
```

## Dağıtım

Uygulama mevcut Kubernetes/Helm standardına uygun şekilde `deploy/helm/cdc-monitoring/` altında paketlenmiştir. Gerekli Secret'lar ve önkoşullar için chart'ın `templates/NOTES.txt` dosyasına bakın. Çalışma parametreleri (alarm eşikleri, SMTP vb.) chart/Secret üzerinden değil, ilk kurulumdan sonra uygulamanın `/settings` ekranından yönetilir.

## Kapsam Dışı

- Kafka, Strimzi veya Debezium — CDC tamamen PostgreSQL'in yerleşik mantıksal replikasyonu ile yürütülür.
- PostgreSQL'de publication/subscription/replication slot kurulumu — bunların önceden yapılandırılmış olduğu varsayılır, uygulama tarafından oluşturulmaz.
- Slack veya benzeri bir sohbet entegrasyonu — bildirim kanalı yalnızca e-postadır.
