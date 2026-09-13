# CDC Monitoring (CDC-MON-2026)

PostgreSQL örnekleri arasındaki mantıksal replikasyon (publication/subscription) tabanlı CDC (Change Data Capture) ilişkilerini kayıt altına alan, keşfeden, izleyen ve sağlıksız durumlarda e-posta ile bildiren **tamamen salt-gözlem (read-only)** bir .NET uygulaması.

Gereksinimlerin tam listesi için bkz. [`CDC_Monitoring_BRD.md`](./CDC_Monitoring_BRD.md).

## Problem

Şirketin PostgreSQL örnekleri arasındaki CDC ilişkileri (Kafka/Debezium değil, doğrudan PostgreSQL'in yerleşik mantıksal replikasyonu ile) merkezi olarak kayıtlı ve görünür değildi:

- Hangi PostgreSQL örneklerinin var olduğuna dair merkezi bir envanter yoktu.
- Hangi bağlantının hangi bağlantıya CDC ile bağımlı olduğu tek bir yerden görülemiyordu.
- Bir replication slot'un sessizce inaktif kalması veya bir subscription'ın hataya düşmesi fark edilmiyordu — bu da disk dolması (WAL birikimi) veya hedefin güncel olmaması gibi sonuçlar doğurabiliyordu.

## Bu uygulama ne yapar

- **Bağlantı Defteri:** PostgreSQL bağlantılarını (host/port/db/kullanıcı/parola/ortam etiketi) kaydeder; parolalar uygulama içi Data Protection ile şifreli saklanır, hiçbir ekranda geri gösterilmez.
- **CDC İlişkileri:** Kayıtlı bağlantılar arasında `pg_publication`, `pg_subscription`, `pg_replication_slots` gibi sistem görünümlerini salt-okuma ile tarayarak CDC ilişkilerini otomatik keşfeder; kullanıcı bu önerileri onaylar/reddeder ya da elle tanımlar.
- **Topoloji:** Kayıtlı bağlantıları ve aralarındaki CDC ilişkilerini canlı güncellenen, hiyerarşik (kaynak → publication/hub → hedef) bir graf ekranında gösterir; bir publication'ın birden fazla hedefe bağlandığı durumlar (fan-out) ayrı kenarlar olarak görünür, kenar rengi sağlık durumunu (yeşil/sarı/kırmızı/gri) taşır. Bir düğüme tıklamak onu ve doğrudan komşularını öne çıkarıp geri kalanını soluklaştırır (odaklama); bir kenara tıklamak slot/lag detaylarını gösterir.
- **Alarmlar:** Slot inaktifliği, WAL kritik durumu, subscription hatası, sürekli artan lag ve ardışık health check hatası gibi durumları izler; eşik aşıldığında **e-posta** ile bildirir (anti-flap: aynı sorun için tekrar tekrar göndermez, durum düzelince otomatik kapatır). `/alerts` ekranı önem/tür/durum filtreleri ve sayfalama ile geçmişi listeler.
- **Veri Tutarlılık Kontrolü:** Onaylanmış CDC ilişkileri için (aralığı dakika/saat/gün olarak `/settings`'ten seçilebilir bir sıklıkta) kaynak-hedef satır sayısı/checksum karşılaştırması yapar, tutarsızlık bulunursa e-posta ile raporlar. Checksum yalnızca **kaynağın publication'ında gerçekten yayınlanan kolonlara** göre hesaplanır — hedef servisin kendi eklediği fazladan bir kolon (ör. bir bookkeeping alanı) checksum'ı asla etkilemez. `/reconciliation` ekranı durum filtresi (Eşleşti/Tutarsız) ve sayfalama ile sonuç geçmişini listeler; eski kayıtlar ayarlanabilir bir saklama süresinden sonra otomatik silinir.
- **Şema Karşılaştırma:** `/schema-check` ekranından, seçilen bir CDC ilişkisi için publication'ındaki her tabloda kaynak/hedef kolon listelerini **talep üzerine (on-demand)** karşılaştırır — eksik kolon, hedefte fazladan kolon ve tip uyuşmazlıklarını gösterir. Reconciliation'ın aksine periyodik çalışmaz, geçmiş tutmaz; salt tanı amaçlı canlı bir sorgu aracıdır.
- **Ayarlar:** Tüm çalışma parametreleri (tarama sıklıkları, alarm eşikleri, SMTP bilgileri, health check/reconciliation kayıt saklama süreleri) veritabanında saklanır ve `/settings` ekranından — her alanın yanındaki bilgi ikonuyla ne işe yaradığı açıklanarak, e-posta ayarları için kaydetmeden test gönderme imkânıyla, **yeniden başlatma gerekmeden** — yönetilir. Tarih/saat formatı (Türkçe/English) tercihi artık Ayarlar'da değil, sol menünün en altında (tarayıcı bazlı bir çerezle, hesap girişi olmadığından o tarayıcıya özel) seçilir.

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

Beş arka plan işi (bağlantı health check, CDC keşfi, alarm değerlendirme, veri tutarlılık kontrolü, retention temizliği) tek bir Quartz job'u (`SchedulerTickJob`) tarafından, sabit kısa aralıklarla (15 sn) "yoklanır". İlk dördünün gerçek çalışma sıklığı veritabanındaki `SystemSettings` tablosundan okunur ve `/settings`'ten değiştirilebilir; retention temizliği sabit 24 saatte bir çalışır (kullanıcıya açılmamıştır — saklama *süresi* ayarlanabilir, taramanın *sıklığı* değil). Bir işin çalışma hakkı, çoklu replika (NFR-05) güvenliği için **atomik bir koşullu UPDATE** ile "claim" edilir — aynı iş iki replikada birden çalışamaz.

## Teknoloji Yığını

- .NET 10, Blazor Server (Interactive Server render mode)
- EF Core 10 + Npgsql (kendi metadata şeması) — izlenen örneklere ise ham Npgsql ile salt-okuma bağlanılır
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
# Metadata DB + kaynak/hedef Postgres (wal_level=logical) + app + Mailpit'i ayağa kaldır.
# Tek komut, elle hiçbir adım gerekmeden uçtan uca bir test ortamı hazırlar:
#  - cdc-fixture servisi pub-db/sub-db sağlıklı olur olmaz gerçek publication/subscription'ları
#    kurar (DB ekibinin gerçek ortamda yapacağı işi simüle eder), iki senaryo halinde:
#     1) Basit: pub-db/orders'ta 5 tablo/publication (orders, customers, products, invoices,
#        payments); orders ve customers ikişer hedefe abone edilerek fan-out gösterir.
#     2) Zengin (kurgusal bir kütüphane/katalog domaini): dom-catalog-api 8 publication
#        yayınlar; dom-lending-api (ayrı bir Postgres instance'ı olan mid-db üzerinde —
#        bir düğümün aynı sunucuda hem hedef hem kaynak olması CREATE SUBSCRIPTION'ı
#        kendi kendine kilitler) bunlardan bazılarına HEM abone olur HEM kendi
#        publication'ını aşağı akışa yayınlar (hem hedef hem kaynak); 5 hedef servis
#        (notification, search-index, billing, analytics, recommendation) farklı
#        kombinasyonlarla abone olur.
#  - app, Development ortamında açılışta bu bağlantıların tamamını Connections ekranına
#    otomatik kaydeder (Program.cs, Seed:LocalCdcFixtureConnections).
docker compose -f docker-compose.local.yml up -d --build
```

Sonra `http://localhost:5299/topology` üzerinden CDC ilişkilerinin otomatik keşfedildiğini — çoğu publication'ın iki-üç ayrı hedefe dallandığı (fan-out) hiyerarşik topolojiyi — ve e-posta bildirimlerinin `http://localhost:8025` (Mailpit) üzerinden gerçek SMTP protokolüyle geldiğini gözlemleyebilirsiniz.

Her kaynak (publisher) tablosuna otomatik olarak birkaç örnek satır eklenir (yalnızca tablo boşsa) ve subscription kurulduktan sonra gerçek mantıksal replikasyonla hedefe kendiliğinden taşınır — Veri Tutarlılık Kontrolü ve Şema Karşılaştırma ekranlarını boş tablolarla değil, gerçek veriyle deneyebilirsiniz. `orders_replica.orders` tablosuna ayrıca, hedef servisin kendi eklediği bir kolonu simüle etmek için kasıtlı olarak fazladan bir `last_updated_on_utc` kolonu eklenir — Şema Karşılaştırma ekranının "hedefte fazladan kolon" senaryosunu canlı örnekle göstermek içindir.

`cdc-fixture` container'ları kalıcı volume kullanmadığından `docker compose up --build` her çağrıldığında yeniden oluşabilir; script bunu `pub-db/orders` içindeki bir işaret tabloya (`_cdc_fixture_marker`) bakarak fark eder — kurulum daha önce tamamlandıysa (veri hâlâ duruyorsa) tüm adımları tekrar denemeden saniyeler içinde çıkar.

`scripts/setup-local-cdc-test.sh`, yalnızca eski/tekil (tek tablo) senaryoyu host'tan `docker compose exec` ile elle tekrarlamak isteyenler için tutulur; normal akışta gerekmez — `cdc-fixture` servisi zaten yukarıdaki genişletilmiş kurulumu otomatik yapar.

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

## Lisans

[Apache License 2.0](./LICENSE) — bkz. `LICENSE` dosyası.
