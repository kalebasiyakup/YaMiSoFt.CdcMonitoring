# Proje Belleği (memory.md)

> Bu dosya, projeye yeni başlayan bir Claude Code oturumunun hızlıca bağlam kazanması
> için tutulur: kim, ne, neden, hangi kararlar neden alındı, geliştirme sırasında
> öğrenilen teknik tuzaklar. Faz/görev durumu için `PROJE_DURUMU.md`'ye, genel
> mimari/kullanım için `README.md`'ye bakın.

## Kim, Ne, Neden

- **Proje sahibi:** Yakup Kalebaşı — Yazılım Çözümleri Mimarı, YaMiSoFt.
- **Ne:** `CDC_Monitoring_BRD.md`'de tanımlı, PostgreSQL örnekleri arasındaki
  mantıksal replikasyon (publication/subscription) tabanlı CDC ilişkilerini kayıt
  altına alan, keşfeden, izleyen ve e-posta ile bildiren salt-gözlem bir .NET
  uygulaması.
- **Neden:** Şirketin CDC envanteri ve sağlık durumu merkezi olarak görünür değildi;
  inaktif bir replication slot disk dolmasına (WAL birikimi), bir subscription hatası
  ise hedefin güncel olmamasına yol açabiliyordu ve bunlar fark edilmiyordu.

## Kullanıcı Tercihleri

- **Dil:** Kullanıcı Türkçe yazıyor, uygulama arayüzü (Blazor UI) **tamamen Türkçe**
  olmalı. Kod (sınıf/değişken adları, route path'leri, DB tablo/kolon adları) İngilizce
  kalabilir — yalnızca **kullanıcının gördüğü metinler** Türkçeleştirilir (sayfa
  başlıkları, buton/etiket metinleri, validasyon hata mesajları, e-posta konu/gövdesi,
  enum değerlerinin ekranda gösterimi). PostgreSQL'e özgü teknik terimler (publication,
  subscription, slot, lag) BRD'nin kendisi de bunları çevirmeden kullandığı için
  olduğu gibi bırakılır.
- **"Reconciliation" çevirisi:** Önce "Uzlaştırma" kullanıldı, kullanıcı "daha
  anlaşılır olması için" **"Veri Tutarlılık Kontrolü"** istedi — bu terim artık
  UI, e-posta metinleri ve log mesajlarında tutarlı şekilde kullanılıyor.
- **Test disiplini:** Kullanıcı iddiaları gerçek ortamda görmek istiyor — her fazın
  sonunda `docker-compose.local.yml` ile gerçek PostgreSQL logical replication ve
  gerçek SMTP (Mailpit) üzerinden uçtan uca canlı doğrulama yapıldı, yalnızca unit
  test yeterli görülmedi. Bu pattern'i sürdürmek önemli.
- **Onay akışı:** Büyük mimari kararlar öncesi (Faz seçimi, frontend teknolojisi,
  parola şifreleme yöntemi, ayarların UI'a taşınma kapsamı) `AskUserQuestion` ile
  netleştirme yapıldı ve kullanıcı hep "Recommended" seçeneği onayladı — bu, önerilen
  varsayılanların isabetli olduğunu gösteriyor, gelecekte de gerekçeli bir varsayılan
  sunup onaya sunmak makul.
- **Dokümantasyon her zaman güncel tutulmalı:** Kullanıcı açıkça "bundan sonrasında
  README/memory v.s. dosyaları hep güncelle" dedi — kod/config'e her değişiklik
  yapıldığında README.md ve bu dosya (memory.md) da aynı iş parçası içinde, sorulmadan
  güncellenmeli (özellikle "Local Geliştirme ve Test" bölümü ve bu dosyanın ilgili
  kısımları).
- **Public repo hazırlığı yapıldı:** Repo public'e alınacak; bu yüzden lisans **Apache
  License 2.0** (`LICENSE`, copyright: Yakup Kalebaşı), gerçek işveren adı yerine
  **"YaMiSoFt"** kullanılıyor (BRD, bu dosya, Helm `values.yaml`'daki registry
  hostname'i dahil), ve local test fixture'ındaki zengin senaryo bilinçli olarak
  **kurgusal bir sektör** (kütüphane/katalog) kullanıyor — gerçek şirket domain/servis
  isimleriyle örtüşmesin diye. Yeni içerik eklerken (özellikle örnek veri, BRD,
  yorum satırları) gerçek şirket adı/iç altyapı adı sızdırmamaya dikkat edilmeli.

## Önemli Mimari Kararlar ve Gerekçeleri

- **Blazor Server** (React/SPA değil): Tamamen .NET stack, ayrı JS build hattı yok,
  SignalR ile canlı güncelleme doğal. Kullanıcı onayladı.
- **Parola şifreleme:** appsettings.json/K8s Secret değil, **ASP.NET Core Data
  Protection** ile uygulama içi şifreleme; anahtar zinciri kendi PostgreSQL
  şemasında (`DataProtectionKeys` tablosu), master anahtar isteğe bağlı bir X509
  sertifikayla korunuyor. **Production'da sertifika zorunlu** — kod incelemesi
  bunu tespit edip `DependencyInjection.cs`'e erken-fail guard'ı ekledi (aksi halde
  sertifikasız, aynı DB'de "şifreli" saklanan parolalar fiilen korumasız kalıyordu).
- **Tüm operasyonel ayarlar DB'de, appsettings.json'da değil:** Health check/CDC
  keşif/alarm/reconciliation ayarları ve SMTP bilgileri `SystemSettings` tablosunda
  (tek satırlık singleton), `/settings` ekranından yönetiliyor. appsettings.json'da
  yalnızca bootstrap ayarları kalır (`ConnectionStrings:MetadataDb`,
  `DataProtection:CertificatePath`, `Quartz:UseClusteredPostgresStore`) — bunlar
  DB'ye bağlanmadan önce gerekli oldukları için config'de kalmak zorunda.
- **Şema Raporu export'u CSV değil gerçek .xlsx (ClosedXML):** CSV'de ayraç ve kodlama
  Excel'in bölge ayarına bağlı (Türkçe Excel ";" bekler, UTF-8 BOM'suz Türkçe karakterler
  bozuluyor). Dosya, Blazor bileşeninden değil `Program.cs`'teki `/schema-catalog/report.xlsx`
  minimal API endpoint'inden üretilir; ekrandaki filtreler query string olarak taşınır ve
  sayfalama UYGULANMAZ (kullanıcı gördüğü sayfayı değil tüm sonucu indirmek ister),
  `SchemaCatalogExcelExporter.MaxRows` ile 50.000 satırda sınırlanır. Bağlantıya `target="_top"`
  konulmuştur: target taşıyan linkleri Blazor'un enhanced navigation'ı ele geçirmez, dosya
  indirmesi tarayıcıya bırakılır.
- **Şema kataloğu "güncel durum + değişiklik günlüğü" olarak saklanır** (her taramanın
  tam snapshot'ı olarak DEĞİL): `DbTables/DbColumns/DbIndexes/DbConstraints` her nesne için
  tek satır tutar (`FirstSeenAt`/`LastSeenAt`/`DroppedAt`), farklar `SchemaChangeEvents`'e
  yazılır. Kullanıcı bu modeli açıkça seçti. Gerekçe: 6 saatte bir × N bağlantı × binlerce
  tablo snapshot'ı hacmi hızla patlatır; bu modelde "şu an ne var" tek sorgu, "ne zaman ne
  değişti" ayrı ve kompakt. Kaynakta kalmayan nesne SİLİNMEZ, `DroppedAt` ile işaretlenir —
  "bu kolon ne zaman kayboldu" cevaplanabilir kalsın diye.
- **Katalog taramasında ilk tarama ve tablo düşüşü/geri gelişi olay üretmez:** ilk doldurmada
  tüm katalog "yeni" görüneceğinden binlerce `TableAdded` kaydı günlüğü kullanılamaz hale
  getirirdi; bir tablo düşerken/geri gelirken de tek bir tablo seviyesi olay yazılır, kolon/
  indeks/kısıt başına ayrıca olay üretilmez (`suppressChildEvents`). Boyut/tahmini satır
  sayısı/publication üyeliği değişimleri de şema değişikliği sayılmaz.
- **Başarısız katalog taraması katalog verisine dokunmaz:** erişilemeyen bir sunucunun tüm
  tabloları "silinmiş" işaretlenirse hem katalog bozulur hem sunucu dönünce binlerce sahte
  "eklendi" kaydı üretilir. Aynı prensip `CdcDiscoveryService`'teki `succeededConnections`
  mantığıyla paralel.
- **Tick-tabanlı zamanlama (Quartz job'ları değil, tek `SchedulerTickJob`):** Dört
  ayrı Quartz job/trigger yerine tek bir job her 15 sn'de bir "yoklama" yapıyor,
  DB'deki güncel interval ayarına göre iş çalıştırılıp çalıştırılmayacağına karar
  veriyor. Bu, ayarların **yeniden başlatma gerekmeden** canlı uygulanmasını sağlıyor.
  Trade-off: bir işin interval'i tick süresinden (15 sn) kısa ayarlanırsa, iş fiilen
  tick hızında çalışır (15 sn'nin altına inemez) — bu UI'da kullanıcıya açıkça
  belirtiliyor.
- **Çoklu replika güvenliği (NFR-05) Quartz clustering ile değil, atomik DB
  claim'i ile sağlanıyor:** `JobScheduleRepository.TryClaimAsync`, EF Core'un
  `ExecuteUpdateAsync`'i ile TEK bir koşullu `UPDATE ... WHERE LastRunAt IS NULL OR
  LastRunAt <= @cutoff` çalıştırır; PostgreSQL'in satır kilitleme garantisi, aynı
  job'un iki replikada aynı anda çalışmasını yapısal olarak engeller. Bu, canlı
  olarak ikinci bir app konteyneri (replika) başlatılarak ve üretilen SQL'in gerçekten
  tek bir atomik UPDATE olduğu loglardan teyit edilerek doğrulandı.
- **CDC ilişki keşfi eşleştirmesi:** `pg_subscription.subconninfo`'daki host/port/db
  bilgisi, kayıtlı bağlantılarla **birebir eşleştirilerek** kaynak bulunur (libpq
  conninfo formatı — Npgsql/ADO.NET formatından farklı, özel bir `ConnInfoParser`
  yazıldı). Eşleşme bulunamazsa (kayıtsız kaynak veya redakte edilmiş conninfo —
  PG16+'da superuser olmayanlar için subconninfo gizlenebilir) ilişki oluşturulmaz,
  yalnızca log uyarısı verilir; kullanıcı FR-06 ile manuel ilişki tanımlayabilir.
- **Reconciliation checksum'ı:** Sıra bağımsız (order-independent) bir teknik —
  her satırın `md5(t::text)`'inin ilk 64 bit'i toplanır (`sum(('x'||...)::bit(64)::bigint)`).
  Standart bir PostgreSQL "checksum without ORDER BY" idiyomu. İki gerçek prod hatasından
  sonra iki kez düzeltildi (bkz. Teknik Tuzaklar): (1) toplamın son adımda `::bigint`'e
  zorlanması büyük tablolarda "bigint out of range" hatası veriyordu, artık `::text`'e
  çevriliyor; (2) `t::text` tüm satırı (tüm kolonları) hash'lediğinden hedefteki fazladan
  bir kolon (hedef servisin kendi eklediği bookkeeping alanı gibi) checksum'ı hep uyuşmaz
  hale getiriyordu — artık kolon listesi kaynaktan alınıp `SELECT <kolonlar> FROM tablo`
  alt sorgusuyla hem kaynak hem hedefe aynen geçiriliyor, yalnızca gerçekten replike
  edilen kolonlar karşılaştırılıyor.
- **Şema Karşılaştırma ekranı (`/schema-check`, `SchemaComparisonService`):** Yukarıdaki
  "hedefte fazladan kolon" sorununu keşfetmek için eklendi — reconciliation'ın aksine
  periyodik çalışmaz/geçmiş tutmaz, kullanıcı bir ilişki seçip "Karşılaştır"a bastığında
  canlı sorgular (eksik/fazladan kolon, tip uyuşmazlığı). Kasıtlı olarak on-demand
  tasarlandı: bu bir alarm değil, bir tanı aracı.
- **Veri Tutarsızlığı alarmı (`AlertType.ReconciliationMismatch`):** `ReconciliationService.RunOnceAsync`,
  tutarsızlık/hata bulunan her onaylı ilişki için (`UpdateMismatchAlertAsync`) `AlertEvaluationService.
  EvaluateRuleAsync` ile aynı tetikle/kapat desenini uygular — ama e-postayı **tekrar göndermez**,
  çünkü aynı çalıştırma sonunda zaten tek bir toplu rapor e-postası gönderiliyor (mevcut yol korundu,
  yeni `AlertEvent` yalnızca kayda/`/alerts` ekranına görünürlük katıyor). `AlertType.ReconciliationMismatch`
  enum'ı ve `/alerts` filtre dropdown'ı uzun süre vardı ama bunu üreten kod hiç yazılmamıştı — kullanıcı
  "e-posta geliyor ama Alarmlar sayfasında görünmüyor" diye fark edene kadar sessiz bir eksiklikti.
- **`IEmailNotifier.SendReportAsync`'in `body` parametresi artık HTML olarak gönderiliyor** (`MailKitEmailNotifier`
  `TextPart("html")` kullanıyor), `SendAlertAsync` (tekil alarm e-postaları) ise hâlâ düz metin — bu bilinçli
  bir asimetri, interface tek satırlık bir yorumla belirtiyor. Şu an `SendReportAsync`'in tek çağıranı
  `ReconciliationService.BuildReportHtml` (inline CSS'li tablo tabanlı bir e-posta şablonu üretir — dinamik
  alanlar `WebUtility.HtmlEncode` ile kaçışlanır, ör. bir Postgres hata mesajı HTML'i bozmasın diye). E-posta
  konusundaki/gövdesindeki eski sabit **"Haftalık"** ifadesi kaldırıldı — kullanıcı sıklığı artık `/settings`'ten
  dakika/saat/gün olarak seçebildiği için yanlış bilgiydi; gövdedeki "Kontrol sıklığı" metni
  `SystemSettings.ReconciliationIntervalSeconds`'tan `FormatInterval` ile dinamik üretiliyor. Yeni bir
  `SendReportAsync` çağıranı eklenirse gövdeyi HTML olarak hazırlamayı unutmayın (düz metin geçirilirse
  e-posta istemcisinde etiketler ham metin olarak görünür).
- **Topoloji grafiği üç katmanlı (kaynak → publication/hub → hedef):** `TopologyGraphBuilder.
  BuildHierarchy` her `(SourceConnectionId, PublicationName)` çifti için ayrı bir hub düğümü
  üretir; bir publication'ın birden fazla subscription'a bağlanması (fan-out) hub'dan çıkan
  ayrı kenarlar olarak kalır. Bir bağlantı hem source hem target rolündeyse (bkz. yukarıdaki
  mid-db notu) basitlik için "source" sayılır. `topology.js`'te vis-network hiyerarşik
  (UD) layout kullanır; bir düğüme tıklamak onu ve komşularını öne çıkarıp gerisini
  soluklaştırır (`applyFocus`/`clearFocus`, opacity tabanlı).

- **Tablo/kolon açıklamaları entity'lere değil `Configure()`'a (Fluent API) eklendi:**
  9 `IEntityTypeConfiguration<T>` dosyasına (`Infrastructure/Persistence/Configurations/*.cs`)
  `builder.ToTable(t => t.HasComment(...))` + her `Property(...)`'ye `.HasComment(...)` eklenip
  `AddTableAndColumnComments` migration'ı oluşturuldu — DB'de `COMMENT ON TABLE`/`COMMENT ON
  COLUMN` üretir. Entity üzerine (ör. EF Core'un `[Comment]` data annotation'ı ile) değil
  `Configure()`'a eklenmesinin sebebi: `CdcMonitoring.Domain.csproj`'un hiçbir NuGet paketi
  referansı yok (saf POCO, framework bağımsız) — entity'ye attribute eklemek Domain'i
  `Microsoft.EntityFrameworkCore.Abstractions`'a bağımlı kılıp katman sınırını bozardı, ayrıca
  projenin zaten kurulu "tüm şema/persistence ayarları (`HasMaxLength`, `HasConversion`,
  `HasIndex`, `HasData`) `Configure()`'da toplanır" konvansiyonundan kopardı. Kullanıcı projeyi
  çalıştırdığında `db.Database.Migrate()` migration'ı otomatik uyguladı; `pg_class.obj_description`
  ve `\d+ "PgConnections"` ile hem 9 tablonun hem kolonların Türkçe yorumları canlı DB'de teyit
  edildi.

- **Kafka Topic Erişim Raporu (`/kafka-audit`), README'nin önceki "Kapsam Dışı: Kafka/Debezium"
  ifadesine rağmen bilinçli olarak eklendi:** Kullanıcı açıkça bir Strimzi/Kafka cluster'ı için
  ayrı bir ACL+consumer-group denetim raporu istedi. Bu, projenin **CDC replikasyon mekanizmasını**
  Kafka/Debezium'a taşımaz (o hâlâ tamamen PostgreSQL mantıksal replikasyonu) — tamamen bağımsız,
  paralel bir özellik. README'deki "Kapsam Dışı" maddesi bu ayrımı netleştirecek şekilde
  güncellendi (replikasyon mekanizması hâlâ Kafka kullanmıyor; `/kafka-audit` CDC akışının parçası
  değil). Yeni bir Kafka isteği gelirse önce bu ayrımı (CDC mekanizması vs. bağımsız denetim
  ekranı) kullanıcıyla netleştirmek gerekebilir.
- **`Confluent.Kafka` AdminClient API'si (v2.6.1) doğrulanmış metotlar:** `IAdminClient.GetMetadata`
  (senkron) topic/partition/replika sayısını verir; `DescribeAclsAsync(AclBindingFilter, ...)`
  `Confluent.Kafka.Admin` namespace'inde (`ResourcePatternFilter`/`AccessControlEntryFilter`/
  `AclOperation`/`AclPermissionType`/`ResourceType` enum'ları da orada); grup listesi için
  `IAdminClient.ListGroups(TimeSpan)` (senkron, `Confluent.Kafka.GroupInfo` döner — **Admin
  namespace'inde DEĞİL**), ayrıntılı assignment için `DescribeConsumerGroupsAsync(IEnumerable<string>,
  DescribeConsumerGroupsOptions)` (`ConsumerGroupDescription.Members[].Assignment.TopicPartitions`
  üzerinden topic adları doğrudan çözülmüş gelir — eski `GroupMemberInfo.MemberAssignment` byte[]
  gibi elle protokol parse etmek GEREKMEZ). `AclOperation.All`, hem yazma hem okumayı zımnen
  kapsadığından tek bir `KafkaAclBinding` değil iki ayrı binding'e (Write+Read) açılmalı — aksi
  halde ALL yetkili bir principal raporda yalnızca tek sütunda görünür. Bu API yüzeyi build
  makinesinde gerçek NuGet paketi indirilip reflection ile doğrulandı (bkz. Application/KafkaAudit,
  Infrastructure/Kafka); Confluent.Kafka'nın public sürüm notlarına bakmadan tahmin etmek riskli —
  bir üst/alt sürüme geçerken bu imzaları tekrar reflection'la teyit etmek gerekir.
- **Kafka AdminClient için özel CA sertifikası kullanılmıyor (kullanıcı isteğiyle kaldırıldı):**
  `KafkaConnectionOptions.SslCaLocation`, `KafkaAdminClientFactory`'deki `SslCaLocation` set'i,
  appsettings'teki `Kafka:SslCaLocation`, docker-compose'daki `Kafka__SslCaLocation` override'ı +
  `kafka-ca.pem` volume mount'ları, `.env.example`'daki `KAFKA_CA_CERT_PATH` ve `secrets/`
  klasörünün tamamı (placeholder `kafka-ca.pem` + README) silindi. `SecurityProtocol.SaslSsl` hâlâ
  kullanılıyor (TLS açık). İlk aşamada "OS'un varsayılan CA deposuna güven" yeterli sanıldı ama iki
  ayrı gerçek engelle karşılaşıldı: (1) `aspnet:10.0` (Debian slim) taban imajında `ca-certificates`
  paketi hiç kurulu değildi — Dockerfile'a `ca-certificates` eklendi (libgssapi-krb5-2 ile aynı
  apt-get satırı). (2) Bu da yetmedi çünkü Aiven'ın broker sertifikası genel güvenilir bir CA'dan
  DEĞİL, projeye özel kendi kendine imzalı bir CA'dan geliyor (`openssl s_client -connect
  <bootstrap>:20751` ile teyit edildi: `issuer=CN=<project-id> Project CA GEN 2`,
  `Verify return code: 19 self-signed certificate in certificate chain`). Kullanıcı bilinçli olarak
  hiç sertifika dosyası eklenmemesini istediğinden, `KafkaAdminClientFactory`'de
  `AdminClientConfig.EnableSslCertificateVerification = false` set edildi — TLS şifrelemesi kalır,
  broker kimlik doğrulaması (MITM koruması) kalkar. Docker image'ı yeniden build edilip
  `docker-compose.local.yml`'deki `app` konteynerinde gerçek Aiven cluster'ına karşı canlı test
  edildi: SSL handshake hatası tamamen kayboldu. Yeniden bir CA dosyası eklemeden veya sertifika
  doğrulamasını tekrar açmadan önce kullanıcıya danışılmalı.
- **`/kafka-audit`'in prerender'da JS interop patlaması, `RendererInfo.IsInteractive` ile düzeltildi:**
  Sayfa `@rendermode InteractiveServer` kullandığından Blazor Server önce statik prerender (JS yok)
  sonra interactive circuit ile iki kez render ediyordu; `OnInitializedAsync` → `LoadAsync` →
  `RenderTopologyAsync` prerender fazında JS import çağırınca "JavaScript interop calls cannot be
  issued at this time" ile 500 dönüyordu (gerçek tarayıcıda da, sadece curl'de değil — production
  loglarında görüldü). Çözüm: `RenderTopologyAsync` çağrısı `RendererInfo.IsInteractive` koşuluna
  bağlandı (Index.razor:133) — ileride benzer JS-interop-in-OnInitializedAsync kalıbı görülürse
  aynı korumayı uygulamak gerekir.
- **Kafka ACL raporu genişletildi — ham ACL listesi + Group ACL topolojisi (kullanıcı isteğiyle):**
  Önceden `GetTopicAclBindingsAsync` yalnızca Topic kaynak tipini ve Allow'u sorguluyor, doğrudan
  `KafkaAclAction` (Write/Read) bilgisine indirgeyip atıyordu (Deny + Group/Cluster/TransactionalId
  tamamen kayboluyordu). Şimdi `IKafkaAdminGateway.GetAclEntriesAsync` (Any/Any/Any filtresiyle)
  TÜM ACL girdilerini `KafkaAclEntry(Principal, PermissionType, ResourceType, ResourceName,
  Operation, PatternType)` olarak ham döner; Write/Read→topic eşlemesi ve Allow filtresi artık
  `KafkaTopicAccessReportService`'te (Application katmanı) yapılıyor, Infrastructure yalnızca
  Confluent enum'larını string'e çeviriyor. `KafkaTopicAccessReport.AclEntries` (tüm kaynak
  tipleri, yalnızca Allow) yeni bir "ACL Detayları" tablosunda (Yetki=Operation, Kullanıcı Adı=
  "User:" öneki soyulmuş principal, Kaynak Tipi, Kaynak Adı) gösteriliyor. `KafkaTopologyBuilder.
  Build` artık ikinci opsiyonel parametre olarak ACL girdilerini alıyor ve Group kaynak tipindeki
  Allow girdilerinden principal→consumer-group "group-acl" kenarları (amber, `#d9a441`) ekliyor —
  bu, bir grubun o an aktif abone olup olmadığından (yeşil "consumes" kenarı) bağımsız olarak
  "kim bu grubu kullanmaya yetkili"yi gösterir. Cluster/TransactionalId kaynak tipleri bilinçli
  olarak topolojiye eklenmedi (yalnızca ACL Detayları tablosunda görünür) — topic-merkezli grafiğe
  doğal bir bağlantı noktaları olmadığından, eklenirse yalnızca izole düğümler olarak kirlilik
  yaratırlardı. `IKafkaAdminGateway.GetTopicAclBindingsAsync` → `GetAclEntriesAsync` olarak yeniden
  adlandırıldı (FakeKafkaAdminGateway ve testler güncellendi). Gerçek Aiven cluster'ına karşı canlı
  testte (browser + docker rebuild ile) üç gerçek bug bulunup düzeltildi:
  1. **vis.js DataSet duplicate edge id:** Aynı principal+group için birden fazla ACL girdisi
     (ör. ayrı Read + Describe operation'ları) `KafkaTopologyBuilder`'da aynı kenar id'sini
     üretiyor, `vis.DataSet.add()` "Cannot add item: item with id ... already exists" ile
     patlıyordu. Çözüm: group-acl kenarları eklenmeden önce `HashSet<string>` ile id bazında
     tekilleştiriliyor.
  2. **Confluent.Kafka 2.6.1'in `ResourceType` enum'ı eksik:** Yalnızca Unknown/Any/Topic/Group/
     Broker(=4) tanımlı; gerçek Kafka'daki TransactionalId(=5)/DelegationToken(=6)/User(=7) için
     isim yok, `.ToString()` ham sayı ("5") döndürüyordu (ACL Detayları tablosunda görüldü) —
     `ConfluentKafkaAdminGateway.ResourceTypeName(ResourceType)` ile `(int)type` üzerinden manuel
     eşleme eklendi (aynı zamanda "Broker" ismi Kafka'nın gerçek terimi olan "Cluster" ile
     değiştirildi). Bir üst Confluent.Kafka sürümüne geçerken bu boşluğun kapanıp kapanmadığı
     (`[enum]::GetValues([Confluent.Kafka.Admin.ResourceType])` ile PowerShell'den reflection)
     kontrol edilmeli.
  3. **`RenderTopologyAsync`'te yakalanmamış `JSDisconnectedException`:** Circuit tam JS çağrısı
     sırasında kapanırsa (sayfadan ayrılma/yenileme yarış durumu) `DisposeAsync`'teki gibi
     yakalanmıyordu, "Unhandled exception in circuit" logu üretiyordu — `LoadAsync`'teki
     `RenderTopologyAsync` çağrısı da aynı `catch (JSDisconnectedException)` korumasına alındı.
  Ayrıca librdkafka client'ı `DescribeAclsResponse returned unknown resource type 6` diye stdout'a
  zararsız bir uyarı basıyor (DelegationToken ACL'leri varsa native kütüphane bunları
  ayrıştıramıyor olabilir) — bu bizim kodumuzdan değil, librdkafka/Confluent.Kafka'nın kendi
  sınırlamasından kaynaklanıyor, düzeltilemez/düzeltilmemeli.
- **Topoloji topic'leri bağlantısız kalıyordu — ACL kaynak adı topic adı değil, kalıptı (kullanıcı geri bildirimi):**
  Aiven'da topic ACL'leri `*` (tüm topic'ler) kaynak adıyla, bazıları özel topic adıyla tanımlı; ilk
  uygulama ACL'i `ResourceName == topic adı` ile eşliyordu, `*` hiçbir topic'e uymadığından event-1/
  event-2 grafikte izole kaldı (canlı veride `Topic *` satırları vardı ama tablo yalnızca ham listeydi).
  Düzeltme: `KafkaTopicAccessReportService.ResourceMatchesTopic` — Literal `*` hepsine, Literal ad
  birebir, Prefixed önek olarak eşler. Ayrıca Aiven principal VE kaynak adlarını kaçışlı döner
  (`dom\-user\-1`, `event\-1`); `KafkaPrincipalFormatter.Unescape/DisplayName` ile hem eşlemede hem
  gösterimde çözülüyor (ham `KafkaAclEntry` değerleri kaçışlı kalır, testler buna göre). Topolojide
  her kullanıcıda bulunan Group `*` ACL'leri (bilgi taşımayan) artık düğüm/kenar üretmiyor.
  ACL Detayları tablosu Aiven konsolu gibi: `KafkaAclSummaryBuilder` (kullanıcı, kaynak) başına tek
  satır + Aiven izin adları (admin/readwrite/read/write); Blazor sayfasında arama/kaynak tipi/izin
  filtreleri. Tarayıcı aracı bu turda yoktu: veri curl ile (prerender HTML) doğrulandı, grafik
  görseli gözle doğrulanmadı.
- **Kafka host/kullanıcı adı da ortam değişkeninden okunuyor (public repo hazırlığı, kullanıcı isteği):**
  Repo public olacağından `appsettings.Development.json`'daki gerçek Aiven host'u ve `avnadmin`
  kullanıcı adı kaldırıldı. `KafkaConnectionOptions` artık yalnızca ortam değişkeni ADLARINI tutar
  (`BootstrapServersEnvironmentVariable`/`UsernameEnvironmentVariable`/`PasswordEnvironmentVariable`,
  varsayılanlar `KAFKA_BOOTSTRAP_SERVERS`/`KAFKA_USERNAME`/`KAFKA_SASL_PASSWORD`); değerleri
  `KafkaAdminClientFactory` her çağrıda ortamdan okur. compose dosyaları üçünü `.env`'den
  enjekte eder, `.env.example` güncel şablon. Eski tuzak da kapandı: `appsettings.json`'daki boş
  `PasswordEnvironmentVariable: ""` Development dışında varsayılana düşmüyordu; artık boş ad
  varsayılana düşüyor (`DependencyInjection.EnvName`). Bekleyen konu: TLS doğrulamasının
  (`EnableSslCertificateVerification=false`) `Kafka:VerifySsl` ayarına bağlanması önerildi, kullanıcı henüz onaylamadı.

## Teknik Tuzaklar / Öğrenilenler (gelecekte tekrar karşılaşılabilir)

- **`dotnet ef migrations add` sırasında `HostAbortedException` fırlaması normaldir**
  — EF Core Design'ın host'u durdurma şeklidir, `"Done."` çıktısı varsa migration
  başarıyla oluşmuştur, hata değildir.
- **Yeni bir zamanlanmış iş eklerken `JobScheduleConfiguration`'daki `HasData` seed'ine satır
  eklemek ZORUNLUDUR** (+ migration). `JobScheduleRepository.TryClaimAsync` yalnızca VAR OLAN
  satırı koşullu `UPDATE` ile günceller, satır yoksa insert etmez — bu yüzden seed satırı
  olmayan bir job hiçbir zaman claim alamaz ve **sessizce hiç çalışmaz** (hata da vermez).
  Şema katalog job'unda tam olarak bu oldu; ancak canlı testte (JobSchedules tablosuna bakarak)
  yakalandı. Yeni job eklerken JobSchedules tablosundan `LastRunAt`'in ilerlediğini doğrulayın.
- **Aynı sorguda birden fazla koleksiyon `Include` edilirse EF kartezyen çarpım üretir**
  (tablo başına kolon × indeks × kısıt satırı) ve `MultipleCollectionIncludeWarning` loglar.
  Katalog sorgularında `AsSplitQuery()` kullanılıyor. Docker loglarında bu uyarıyı görürseniz
  kaynağı budur; koleksiyon sayısı ikiden fazlaysa split query neredeyse her zaman doğru seçim.
- **EF Core: kendi oluşturduğunuz bir record'a `Select` ile projeksiyon yaptıktan SONRA
  `Where`/`OrderBy` eklerseniz sorgu çevrilemez** ("The LINQ expression could not be
  translated"). Filtreleme/sıralama entity üzerinde yapılmalı, projeksiyon en sona
  konmalıdır (bkz. `SchemaCatalogRepository.GetPagedColumnReportAsync`).
- **EF Core: takip edilen bir entity'nin koleksiyonuna ANAHTARI DOLU bir çocuk eklerseniz
  INSERT değil UPDATE üretir** ve satır olmadığı için `DbUpdateConcurrencyException`
  ("expected to affect 1 row(s), but actually affected 0") atar. Guid PK'ler EF'te
  `ValueGeneratedOnAdd` olduğundan `Id = Guid.NewGuid()` atamak entity'yi "var olan satır"
  gibi gösterir. Çözüm: `DbContext.Add` ile açıkça eklenmeyen, yalnızca navigation
  koleksiyonuna eklenen nesnelerde (bkz. `SchemaCatalogService.CreateColumn/CreateIndex/
  CreateConstraint`) Id'yi ATAMAYIN, EF üretsin. Bu hata ilk taramada görünmez — yeni
  tablonun parent'ı zaten `Added` olduğu için çocukları da `Added` olur; yalnızca MEVCUT
  bir tabloya kolon eklendiğinde patlar.
- **`information_schema.columns` materyalize görünümlerin kolonlarını HİÇ göstermez** (ve
  büyük kataloglarda yavaştır). Katalog taraması bu yüzden kolonları doğrudan
  `pg_attribute` + `format_type` + `information_schema._pg_char_max_length/_pg_numeric_*`
  ile okur. Ayrıca `information_schema.columns.ordinal_position`, silinmiş kolonlar
  yüzünden `pg_attribute.attnum` ile aynı olmayabilir — `col_description`'a ordinal_position
  geçirmek yanlış kolonun açıklamasını getirir.
- **`ReadOnlyGuardTests` artık kelime sınırlı (`DROP` gibi) arıyor:** düz "contains"
  kontrolü `pg_attribute.attisdropped` gibi meşru katalog kolon adlarını ihlal sayıyordu.
  Yeni sorgu sabiti eklerken `[InlineData]` listesine eklemeyi unutsanız bile, tüm
  `...Query` sabitlerini yansımayla tarayan test kapsar.
- **Blazor Server ilk form submit'i bazen sessizce başarısız olur** (SignalR circuit
  henüz tam kurulmamışken): "Attempting to reconnect to the server" görülür, birkaç
  saniye bekleyip aynı butona tekrar tıklamak yeterlidir. Bu oturumda defalarca
  karşılaşıldı — form submit sonrası her zaman DB'den doğrulama yapmak (ekran
  görüntüsüne güvenmemek) daha güvenilir.
- **Docker build context'e `bin/`/`obj/` sızarsa** (Windows host'ta derlenmiş,
  Windows path'lerine referans veren dosyalar) container içinde `dotnet publish`
  `NuGet.ProjectModel` yükleme hatasıyla başarısız olur. Çözüm: `.dockerignore`'da
  `**/bin/` ve `**/obj/` olmalı (bu projede zaten var).
- **`docker compose`'un varsayılan `depends_on`'u yalnızca container'ın başladığını
  bekler, servisin gerçekten hazır olduğunu değil.** Postgres servisleri için
  `healthcheck` + `condition: service_healthy` şart — aksi halde app, DB henüz
  bağlantı kabul etmiyorken migration denemesi yapıp fatal hata ile çıkabilir.
- **Sistemde bir kez .NET SDK kurulumu (`C:\Program Files\dotnet\`) büyük ölçüde
  boşaldı** (muhtemelen antivirüs/EDR, MailKit'in getirdiği `BouncyCastle.Cryptography`
  gibi yeni derlenmiş kriptografi DLL'lerini yanlış pozitif olarak karantinaya aldı).
  Belirti: `dotnet --version` bile "No .NET SDKs were found" hatası verir. Çözüm
  kullanıcı tarafında (antivirüs karantina geçmişi / SDK yeniden kurulum) — bu tür
  bir hata görülürse önce `Get-ChildItem "C:\Program Files\dotnet\sdk\<version>\"`
  ile dosya sayısını kontrol edip gerçekten bozulma olup olmadığını doğrulayın.
- **Aynı `obj/`/`bin/` çıktısına iki `dotnet build` süreci (ör. arka planda çalışan
  bir subagent'in kendi build'i) aynı anda yazmaya çalışırsa** `CS2012: dosya ...
  başka bir işlem tarafından kullanılıyor (VBCSCompiler)` hatası alınır — geçicidir,
  diğer süreç bitince tekrar denemek yeterlidir; `taskkill /IM dotnet.exe` gibi
  saldırgan bir müdahaleye gerek yoktur (çalışan bir subagent'in işini kesebilir).
- **EF Core `ExecuteUpdateAsync`** (EF Core 7+) atomik koşullu güncellemeler için
  idealdir — "SELECT sonra UPDATE" pattern'inin race condition riskini taşımaz,
  tek bir SQL UPDATE statement'ı üretir.
- **.NET 8 → .NET 10 yükseltmesi** (tüm `.csproj`'larda `TargetFramework`, EF Core/
  Npgsql/Serilog paketleri 10.x'e bump edildi) sırasında iki gerçek regresyon çıktı:
  - **Npgsql 10.x, bağlantı açarken `libgssapi_krb5.so.2`'yi native olarak yüklemeye
    çalışır**; bu, `mcr.microsoft.com/dotnet/aspnet:10.0` (Debian slim) imajında
    yoktur ve eksikse her bağlantıda gürültülü bir yükleme hatası loglanır (SCRAM/plain
    auth'a döner ama gerçek bir Kerberos/GSS ortamında sert hataya dönüşür). Çözüm:
    Dockerfile runtime aşamasına `apt-get install -y libgssapi-krb5-2` eklemek.
  - **Docker multi-stage build'de `COPY *.csproj` → `dotnet restore` → `COPY src/` →
    `dotnet publish --no-restore` kalıbı, Blazor Server'ın `_framework/blazor.web.js`
    dosyasını sessizce 404'e düşürür.** Kök neden: bu JS dosyasını sağlayan örtük
    `Microsoft.AspNetCore.App.Internal.Assets` NuGet paketinin restore edilip
    edilmeyeceği, restore anında projede bir `wwwroot/` klasörünün var olup olmadığına
    bağlı; katmanlama optimizasyonu restore'u yalnızca `.csproj` dosyaları kopyalanmışken
    çalıştırdığından (henüz `wwwroot` yok), paket hiç restore edilmez ve `dotnet publish
    --no-restore` bunu asla telafi etmez. Sonuç: SignalR circuit hiç kurulamaz, tüm
    `InteractiveServer` bileşenleri sessizce statik/etkileşimsiz render'a düşer (form
    submit'leri ham HTTP POST gibi davranır, hiçbir şey kaydetmez, sunucu tarafında hata
    logu da yoktur — teşhisi zorlaştıran asıl sebep budur). **Çözüm: `dotnet publish`
    çağrısından `--no-restore`'u kaldırmak** (restore paket indirmesi zaten global NuGet
    cache'inden geldiği için maliyeti küçük, ama `wwwroot` mevcutken restore/evaluate'in
    yeniden çalışmasını garanti eder). Bu tür "publish sonrası temel bir framework
    dosyası 404" belirtisi görülürse önce bunu kontrol edin.
- **Blazor Server'ın `blazor-error-ui` (`#blazor-error-ui`) div'i her zaman DOM'dadır**
  (CSS `display:none` ile gizli, yalnızca gerçek bir circuit hatasında görünür hale
  gelir). Chrome automation'ın `read_page` (accessibility tree) çıktısı bu elementi
  her zaman "generic" olarak listeler — **gerçekten görünür olup olmadığını anlamak
  için ekran görüntüsü almak veya `getComputedStyle(...).display` kontrol etmek
  gerekir**, yoksa yanlışlıkla "hata var" sanılıp gereksiz debug'a girilir (bu oturumda
  olduğu gibi).
- **Blazor Server bileşenlerinde birden fazla Application servisini `Task.WhenAll` ile
  "paralel" çağırmak, aynı circuit scope'undaki `DbContext`'i eşzamanlı kullanmaya
  çalışır ve `InvalidOperationException: A second operation was started on this
  context instance...` ile sayfayı 500'e düşürür.** Bu, kod incelemesinde
  `ConnectionHealthCheckService` için zaten tespit edilip düzeltilmiş olan sınıfa
  giren bir hatadır (bkz. kod inceleme bulgusu #1) — bu oturumda Ana Sayfa
  dashboard'unu yazarken (4 servisi `Task.WhenAll` ile çağırarak) **aynı hata
  tekrar yapıldı** ve canlı testte 500 olarak yakalandı. **Kural: bir Blazor
  bileşeninin `@code` bloğunda birden fazla scoped Application servisi
  çağrılıyorsa, bunlar her zaman sırayla (`await` ardışık), asla `Task.WhenAll`
  ile paralel çağrılmamalı** — DbContext'in kendisi thread-safe değildir, aynı
  scope'ta paralellik güvenli değildir (yalnızca gerçekten ayrı DbContext scope'ları
  varsa, ör. arka plan job'larında `IServiceScopeFactory.CreateScope()` ile açılan
  bağımsız scope'lar arasında paralellik güvenlidir).
- **Bir PostgreSQL veritabanı, AYNI sunucu üzerinde hem logical replication SUBSCRIBER
  hem PUBLISHER olduğunda (başka bir veritabanının ona abone olması), `CREATE
  SUBSCRIPTION` süresiz kilitlenebilir (deadlock).** Kök neden: komut kendi açtığı
  walsender'ın (`CREATE_REPLICATION_SLOT ... SNAPSHOT 'nothing'`) bitmesini beklerken,
  o walsender da komutun tuttuğu transactionid kilidini bekler — döngüsel bekleme.
  `pg_stat_activity`'de `wait_event: transactionid` + `backend_type: walsender` görülüyorsa
  bu budur (sıralama/apply-worker sayısıyla ilgisi yok, denendi — yalnızca "aynı sunucuya
  geri bağlanan subscription" tetikliyor). **Çözüm: "hem hedef hem kaynak" rolündeki
  düğümü ayrı bir üçüncü Postgres instance'ına koymak** (bkz. `docker-compose.local.yml`
  `mid-db` servisi, `dom-lending-api` için) — hiçbir subscription artık kendi sunucusuna
  dönmüyor. Ayrıca ~25+ subscription/slot kullanan bir test ortamında Postgres'in
  `max_replication_slots`/`max_wal_senders`/`max_logical_replication_workers` (varsayılan
  sırasıyla 10/10/4) ve `max_worker_processes` (varsayılan 8) yetersiz kalır, yükseltilmeli.
- **`AlertEvaluationService` ve `ReconciliationService`, yalnızca durumu `Confirmed`/`Manual` olan
  CDC ilişkilerini değerlendirir** (`Status is CdcRelationshipStatus.Confirmed or CdcRelationshipStatus.
  Manual` filtresi) — yeni keşfedilen bir ilişki `Inferred` ("onay bekliyor") durumundayken sağlıksız
  olsa bile (`CdcDiscoveryService`'in yazdığı "CDC ilişkisi sağlıksız" log uyarısı görülse bile) **hiçbir
  zaman alarm/e-posta üretmez**, çünkü `EvaluateCdcRelationshipsAsync`/`ReconcileOneAsync` döngüleri
  `trusted` listesine hiç girmez. Bu, kullanıcının "log'da hata görüyorum ama alarm gelmiyor" diye
  kafasının karıştığı gerçek bir senaryoydu — kök neden ilişkinin `/relationships` ekranından henüz
  onaylanmamış olmasıydı, kod hatası değildi. Bu tür bir şikayet gelirse önce ilgili ilişkinin durumunu
  kontrol edin.
- **Chrome automation `computer` aracının `left_click`'i (hem koordinat hem `ref` ile)
  bazen bir `<button type="submit">` üzerinde tıklama olayını sayfaya iletmiyor**
  (görsel olarak buton üzerinde gibi görünse de sunucuda hiçbir istek/log oluşmuyor).
  Birkaç kez retry işe yaramazsa, `javascript_tool` ile
  `document.querySelector('button[type="submit"]').click()` çalıştırmak güvenilir bir
  fallback'tir.

- **Serilog kullanılan bir projede `appsettings.json`'daki standart `"Logging": { "LogLevel": {...} }`
  bölümü, Serilog'un kendi filtrelemesini HİÇ etkilemez.** `UseSerilog(...).ReadFrom.Configuration(...)`
  çağrısı yalnızca `"Serilog": { "MinimumLevel": { "Default": ..., "Override": {...} } }` bölümünü
  okur (Serilog.Settings.Configuration paketinin konvansiyonu). Bu oturumda EF Core'un her SQL
  komutunu Information seviyesinde loglaması (`Microsoft.EntityFrameworkCore.Database.Command`)
  önce yanlışlıkla `Logging` bölümüne override eklenerek "düzeltilmeye" çalışıldı — hiçbir etkisi
  olmadı, loglar aynı hacimde devam etti. Doğru çözüm `Serilog.MinimumLevel.Override` altına
  eklemekti. Belirti: appsettings'te bir log-level override'ı ekleyip rebuild ettikten sonra
  log hacminde HİÇ değişiklik yoksa, muhtemelen yanlış bölüme yazılmıştır.
- **MailKit `SecureSocketOptions.Auto`, bir boolean "StartTLS kullan" toggle'ının "kapalı"
  durumu için YANLIŞ varsayılan olabilir.** Auto, sunucu STARTTLS'i destekliyorsa (bozuk/yanlış
  bir sertifikayla bile) fırsatçı şekilde onu dener ve başarısız TLS handshake'i sessizce
  plaintext'e düşürmez — hata fırlatır. Port 25 üzerinde STARTTLS'i destekleyen ama sertifikası
  bağlanılan host adıyla uyuşmayan bir kurumsal relay'de bu, "SslHandshakeException: host name
  did not match" olarak ortaya çıktı. Çözüm: toggle kapalıyken porta göre karar veren bir
  `ResolveSecureSocketOptions` — 465 için `SslOnConnect` (implicit TLS, Ayarlar ekranındaki
  tooltip'in zaten vaat ettiği davranış), diğer portlar için tamamen `None` (hiç TLS denemez).
  Mailpit gibi "no encryption" bir sunucuya karşı local test yaparken de SmtpPort=1025 +
  StartTLS kapalı olmalı — 587 (varsayılan) veya StartTLS açık bırakmak "unexpectedly
  disconnected" ile sonuçlanır.
- **PostgreSQL'de `sum(bigint)` aslında `numeric` (sınırsız hassasiyet) döndürür** — tam da
  taşmayı önlemek için. Bu toplamı tekrar `::bigint`'e zorlamak, yeterince satır (veya birkaç
  uç değerli hash) birikince "22003: bigint out of range" (routine: `numeric_int8`) hatasına yol
  açar. Bir checksum/toplam zaten string olarak saklanıp karşılaştırılıyorsa `::bigint` yerine
  `::text`'e çevirmek taşmayı büyüklükten bağımsız tamamen ortadan kaldırır.
- **`docker-compose.local.yml`'deki servisler kalıcı volume kullanmadığından** container'lar
  (`app`, `cdc-fixture` dahil) `docker compose up`/`up --build` her çağrıldığında yeniden
  oluşabilir/restart olabilir — bu, `cdc-fixture-entrypoint.sh`'in (idempotent olsa da) baştan
  sona tekrar tekrar çalışmasına yol açar. Çözüm: script'in başına, `pub-db/orders`'ta kalıcı
  bir işaret tablosu (`_cdc_fixture_marker`) ekleyip kurulum daha önce tamamlandıysa script'i
  saniyeler içinde bitirmek (bkz. script içindeki yorum). Ayrıca: bu ortamda tüm container'ların
  aynı anda tamamen KAYBOLDUĞU (yalnızca durmuş değil, silinmiş — muhtemelen bir `docker compose
  down`) birkaç kez gözlemlendi; volume olmadığından bu, tüm local verinin (SystemSettings
  özelleştirmeleri dahil) sıfırlanması demektir — normal, ama beklenmedik "verim nereye gitti"
  sorularına yol açabilir.

- **Blazor Server'da `DateTimeOffset.ToLocalTime()` tarayıcının değil sunucunun saat dilimini
  kullanır** — kullanıcı `/settings` → Görünüm'den tr-TR seçse bile saat kaymaz, çünkü culture
  yalnızca biçimi (gg.aa.yyyy vb.) etkiler, saat dilimini değil; render sunucu tarafında
  çalıştığından "yerel saat" sunucunun OS saat dilimidir (çoğu container/sunucu UTC çalışır →
  hiç kayma görünmez). Çözüm: `ClientTimeZoneService` (Web katmanı, Scoped) ilk render sonrası
  JS interop ile (`wwwroot/js/timezone.js`, `Intl.DateTimeFormat().resolvedOptions().timeZone`)
  tarayıcının IANA saat dilimini alır, `TimeZoneInfo.ConvertTime` ile çevirir; zaman gösteren
  sayfalar `TimeZoneAwareComponentBase`'den türetilip `FormatLocal(...)` kullanır. Sunucudaki tüm
  saat damgaları zaten `DateTimeOffset` (`IClock.UtcNow`) — `DateTime.ToLocalTime()`'a değil bu
  servise geçilmeli.

## Yapı/Konvansiyonlar

- Katmanlar: `Domain` (entity/enum, framework bağımsız) → `Application` (servisler,
  arayüzler `Abstractions/` altında) → `Infrastructure` (EF Core, Npgsql, Quartz,
  MailKit, Data Protection implementasyonları) → `Web` (Blazor Server).
- Her mutasyon (Connection CRUD, ilişki onay/red, ayar güncelleme) `AuditLogs`
  tablosuna yazılır; **parola/SMTP şifresi asla audit kaydına girmez** (özel
  `ToAuditSnapshot` DTO'ları ile hariç tutulur).
- İzlenen PostgreSQL örneklerine giden her sorgu `internal const string ...Query`
  olarak sabitlenir ve `CdcMonitoring.UnitTests/Postgres/ReadOnlyGuardTests.cs`
  bunların hepsinin `SELECT` ile başladığını doğrular (FR-14 garantisi, yeni bir
  sorgu eklenirse bu teste de eklenmelidir).
- Local test ortamı: `docker-compose.local.yml` (metadata-db + pub-db/sub-db/mid-db +
  mailpit + app + cdc-fixture). `cdc-fixture` servisi `scripts/cdc-fixture-entrypoint.sh`'i
  koşturup iki senaryo kurar: (1) basit orders/customers/products/invoices/payments
  fan-out'u, (2) kurgusal bir kütüphane/katalog domaini (dom-catalog-api → dom-lending-api
  → 5 hedef servis) — isimler bilinçli olarak jenerik, gerçek şirket domain'iyle
  örtüşmesin diye. Her kaynak (publisher) tablosuna birkaç örnek satır eklenir (yalnızca
  tablo boşsa) ve replikasyonla hedefe taşınır; `orders_replica.orders`'a ayrıca kasıtlı
  bir fazladan kolon (`last_updated_on_utc`) eklenir — Şema Karşılaştırma ekranının
  "hedefte fazladan kolon" senaryosunu canlı göstermek için. Script, `pub-db/orders`'taki
  bir işaret tabloya (`_cdc_fixture_marker`) bakarak kurulum daha önce tamamlandıysa
  tekrar çalışmayı atlar (bkz. Teknik Tuzaklar). `app`, Development'ta açılışta bu
  bağlantıların tamamını Connections ekranına otomatik kaydeder (`Program.cs`,
  `Seed:LocalCdcFixtureConnections`) — elle form doldurmaya gerek yok.
  `scripts/setup-local-cdc-test.sh` yalnızca eski/tekil senaryoyu host'tan elle
  tekrarlamak isteyenler için ikincil olarak duruyor, normal akışta gerekmiyor.
- **Sayfalama + filtreleme deseni** (Alarmlar, Veri Tutarlılık Kontrolü ekranları):
  `Application.Common.PagedResult<T>` (Items/TotalCount/Page/PageSize/TotalPages) ortak
  dönüş tipi; repository katmanında `GetPagedAsync(filter, page, pageSize)` — filtreleme
  ve `Skip/Take` DB seviyesinde (EF Core), tüm kayıtları çekip bellekte filtrelemez.
  Web tarafında paylaşılan `Components/Shared/Pagination.razor` bileşeni (Page/TotalPages/
  TotalCount/OnPageChange) her iki ekranda da aynı. Yeni bir listeleme ekranı eklenirse bu
  deseni tekrarlayın — `GetRecentAsync` gibi eski, filtresiz/sayfalamasız metotlar (Ana
  Sayfa widget'ları gibi başka yerlerde kullanıldığı için) kaldırılmadı, yanına eklendi.
  Her iki ekranda da filtre satırının sağında (`ms-auto`) bir manuel **Yenile** butonu var
  (`_isRefreshing` bool'u ile yüklenirken `disabled`, `LoadAsync`'i doğrudan çağırır) —
  otomatik canlı güncelleme (SignalR push vb.) yok, kullanıcı listeyi elle tazeliyor.
- **Retention (kayıt saklama) deseni** (Health Check, Veri Tutarlılık Kontrolü, şema tarama
  geçmişi ve şema değişiklik kayıtları — katalogun KENDİSİ kapsam dışı, o geçmiş değil güncel
  durumdur;
  audit log KASITLI olarak kapsam dışı, kalıcı tutulmalı): `SystemSettings`'te
  `*RetentionDays` alanı + `RetentionCleanupService.RunOnceAsync` (`DeleteOlderThanAsync`
  ile `ExecuteDeleteAsync` — toplu, entity yüklemeden siler) + `SchedulerTickJob`'da sabit
  24 saatlik bir iş (`JobNames.RetentionCleanup`, kullanıcıya açılmayan tek sabit interval).
  Yeni bir "sınırsız büyüyen log tablosu" eklenirse bu deseni tekrarlayın; ilgili tarih
  kolonuna (`CheckedAt`/`RunAt` gibi) ayrı bir indeks eklemeyi unutmayın (bkz.
  `CdcRelationshipHealthConfiguration`/`ReconciliationResultConfiguration`).
