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

## Teknik Tuzaklar / Öğrenilenler (gelecekte tekrar karşılaşılabilir)

- **`dotnet ef migrations add` sırasında `HostAbortedException` fırlaması normaldir**
  — EF Core Design'ın host'u durdurma şeklidir, `"Done."` çıktısı varsa migration
  başarıyla oluşmuştur, hata değildir.
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
- **Retention (kayıt saklama) deseni** (Health Check + Veri Tutarlılık Kontrolü kayıtları;
  audit log KASITLI olarak kapsam dışı, kalıcı tutulmalı): `SystemSettings`'te
  `*RetentionDays` alanı + `RetentionCleanupService.RunOnceAsync` (`DeleteOlderThanAsync`
  ile `ExecuteDeleteAsync` — toplu, entity yüklemeden siler) + `SchedulerTickJob`'da sabit
  24 saatlik bir iş (`JobNames.RetentionCleanup`, kullanıcıya açılmayan tek sabit interval).
  Yeni bir "sınırsız büyüyen log tablosu" eklenirse bu deseni tekrarlayın; ilgili tarih
  kolonuna (`CheckedAt`/`RunAt` gibi) ayrı bir indeks eklemeyi unutmayın (bkz.
  `CdcRelationshipHealthConfiguration`/`ReconciliationResultConfiguration`).
