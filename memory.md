# Proje Belleği (memory.md)

> Bu dosya, projeye yeni başlayan bir Claude Code oturumunun hızlıca bağlam kazanması
> için tutulur: kim, ne, neden, hangi kararlar neden alındı, geliştirme sırasında
> öğrenilen teknik tuzaklar. Faz/görev durumu için `PROJE_DURUMU.md`'ye, genel
> mimari/kullanım için `README.md`'ye bakın.

## Kim, Ne, Neden

- **Proje sahibi:** Yakup Kalebaşı — Yazılım Çözümleri Mimarı, YaMiSoFt.
- **Ne:** `CDC_Monitoring_BRD_v0.3.md`'de tanımlı, PostgreSQL örnekleri arasındaki
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
  Standart bir PostgreSQL "checksum without ORDER BY" idiyomu.

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
- **Chrome automation `computer` aracının `left_click`'i (hem koordinat hem `ref` ile)
  bazen bir `<button type="submit">` üzerinde tıklama olayını sayfaya iletmiyor**
  (görsel olarak buton üzerinde gibi görünse de sunucuda hiçbir istek/log oluşmuyor).
  Birkaç kez retry işe yaramazsa, `javascript_tool` ile
  `document.querySelector('button[type="submit"]').click()` çalıştırmak güvenilir bir
  fallback'tir.

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
- Local test ortamı: `docker-compose.local.yml` (metadata-db + pub-db/sub-db +
  mailpit + app) ve `scripts/setup-local-cdc-test.sh` (test publication/subscription
  kurulumu — bilinçli olarak uygulama dışında, DB ekibinin yapacağı işi simüle eder).
