# CDC Monitoring — Proje Durumu

> Bu dosya, projeye sonradan (yeni bir Claude Code oturumunda) devam edebilmek için
> tutulur. Faz bazlı tamamlanma durumunu ve açık/kalan işleri listeler. Genel
> proje bağlamı için `memory.md` ve `README.md`'ye, gereksinimler için
> `CDC_Monitoring_BRD.md`'ye bakın.
>
> Son güncelleme: 2026-09-14

## Genel Durum

BRD'nin 3 fazı da (FR-01..FR-14, NFR-01..NFR-08) uygulanmış, `docker-compose.local.yml` ile gerçek PostgreSQL logical replication ve gerçek SMTP (Mailpit) üzerinden uçtan uca canlı doğrulanmıştır. Ayrıca BRD'de olmayan dört ek iş tamamlanmıştır: tüm operasyonel ayarların DB+UI'a taşınması, UI'daki İngilizce metinlerin Türkçeleştirilmesi, **.NET 8 → .NET 10 yükseltmesi** ve **UI'ın görsel modernizasyonu**.

**UI modernizasyonu** (`app.css`, `MainLayout.razor.css`, `NavMenu.razor.css`) CSS-only olarak yapıldı — hiçbir `.razor` markup'ı değişmedi, mevcut Bootstrap sınıfları (`table`, `btn`, `badge`, `alert`, `form-control`) korunarak üzerine modern bir tasarım dili (CSS custom properties ile renk paleti, kart görünümlü içerik alanı, yumuşak gölgeler, modern tipografi, koyu lacivert sidebar, pill-shaped badge'ler) bindirildi. `/connections`, `/connections/new`, `/topology`, `/settings` ekranlarında görsel olarak doğrulandı. Harici font/CDN bağımlılığı eklenmedi (sistem fontu kullanıldı) — projenin "vis-network self-hosted, CDN bağımlılığı yok" ilkesiyle tutarlı.

**.NET 10 yükseltmesi tamamlandı ve canlı doğrulandı** (`dotnet build` 0 hata, `dotnet test` 42/42, Docker Compose üzerinde gerçek pub-db/sub-db ile uçtan uca). Yükseltme sırasında iki regresyon bulunup düzeltildi: (1) Npgsql 10'un ihtiyaç duyduğu `libgssapi-krb5-2` native kütüphanesi `aspnet:10.0` imajında eksikti — Dockerfile'a eklendi; (2) Dockerfile'ın `COPY *.csproj` → restore → `COPY src/` → `publish --no-restore` katmanlama kalıbı, `wwwroot` restore anında henüz kopyalanmadığından Blazor'un `_framework/blazor.web.js`'ini sağlayan örtük paketin hiç restore edilmemesine ve dolayısıyla SignalR circuit'in hiç kurulamamasına yol açıyordu (tüm etkileşimli formlar sessizce bozuktu) — `--no-restore` kaldırılarak düzeltildi. Ayrıntılar için `memory.md`'deki "Teknik Tuzaklar" bölümüne bakın.

**Kod incelemesi tamamlandı** (`/code-review high --fix`, ~17 dk sürdü, 172 tool-call, 9 finder agent). 10 gerçek bulgu buldu ve hepsini düzeltti; 2 aday bulguyu (vis-network üzerinden XSS iddiası, `ConsecutiveHealthCheckFailures=0` senaryosu) inceleyip yanlış olduğunu kanıtlayarak gereksiz değişiklik yapmadı. `dotnet build` (0 hata) ve `dotnet test` (42/42) bu oturumda ayrıca doğrulandı.

**Topoloji ekranı hiyerarşik modele geçirildi** (kaynak → publication/hub → hedef), gerçek fan-out senaryolarıyla (`docker-compose.local.yml` üzerinden `cdc-fixture` servisi otomatik kurar) uçtan uca doğrulandı; ayrıca "hem hedef hem kaynak" bir düğümün aynı Postgres instance'ında kendi kendini kilitlemesi (deadlock) keşfedilip ayrı bir `mid-db` instance'ıyla yapısal olarak çözüldü (bkz. `memory.md` Teknik Tuzaklar). Ayarlar sayfasına her alan için info-tooltip ve e-postayı kaydetmeden test etme butonu eklendi. Repo **public'e almaya hazırlandı**: Apache-2.0 `LICENSE` eklendi, gerçek işveren adı "YaMiSoFt" ile değiştirildi, local fixture verisi kurgusal bir sektöre (kütüphane/katalog) çevrildi, secret taraması yapıldı (bulgu yok). Detaylar için `README.md`'nin "Local Geliştirme ve Test" bölümüne ve `memory.md`'ye bakın.

Tüm bu çalışma (kod incelemesi + .NET 10 yükseltmesi + UI modernizasyonu + UI iyileştirmeleri/dashboard + topoloji/public-repo işi) **commit edilmiştir** (`git log` ile teyit edilebilir).

## Faz Durumu

| Faz | Kapsam | Durum |
|---|---|---|
| Faz 1 | Bağlantı Kayıt Defteri (CRUD, şifreli parola, health check, temel Prometheus metrikleri, Helm iskeleti) | ✅ Tamamlandı, canlı doğrulandı |
| Faz 2 | CDC İlişki Keşfi + Topoloji ekranı | ✅ Tamamlandı, gerçek logical replication ile doğrulandı |
| Faz 3 | E-posta bildirimleri (alarm eşikleri) + Veri Tutarlılık Kontrolü (reconciliation) | ✅ Tamamlandı, gerçek SMTP (Mailpit) ile doğrulandı |
| Ek-1 | Tüm operasyonel ayarların (appsettings.json → DB + `/settings` UI) taşınması, tick-tabanlı zamanlama, çoklu replika güvenli claim mekanizması | ✅ Tamamlandı, çoklu replika testiyle doğrulandı |
| Ek-2 | UI'daki İngilizce metinlerin Türkçeleştirilmesi | ✅ Tamamlandı |
| Ek-3 | Genel kod incelemesi (`/code-review high --fix`) | ✅ Tamamlandı, 10 bulgu düzeltildi, commit edildi |
| Ek-4 | .NET 8 → .NET 10 yükseltmesi (TargetFramework, EF Core/Npgsql/Serilog paketleri, Dockerfile) | ✅ Tamamlandı, canlı doğrulandı, commit edildi |
| Ek-5 | UI görsel modernizasyonu (CSS-only: renk paleti, tipografi, kart/tablo/form/badge/buton stilleri, sidebar) | ✅ Tamamlandı, tüm ana sayfalarda görsel doğrulandı, commit edildi |
| Ek-6 | UI iyileştirmeleri: menüye özel ikonlar, Düzenle/Sil ikon butonları, Ayarlar sayfası sekmeli görünüm, "Bağlantı Defteri"→"Bağlantılar" yeniden adlandırma, Ana Sayfa dashboard'u (özet kartları, sağlık dağılımı, son alarmlar, hızlı erişim) | ✅ Tamamlandı, görsel doğrulandı, commit edildi |
| Ek-7 | Topoloji hiyerarşik modele geçirildi (kaynak→hub→hedef, fan-out, odaklama/dimming), local fixture genişletildi + otomatik kuruldu (`cdc-fixture`, `mid-db`), Ayarlar sayfasına info-tooltip + e-posta test butonu, public repo hazırlığı (LICENSE, jenerik isimlendirme) | ✅ Tamamlandı, uçtan uca canlı doğrulandı, commit edildi |
| Ek-8 | Topoloji ekranında uzun publication adlarının hub düğümlerinde çakışması giderildi (`widthConstraint` ile satır kaydırma + `nodeSpacing` artışı), Blazor'un varsayılan İngilizce reconnect UI'ı (`ReconnectModal`) Türkçeleştirildi, `/settings` → Görünüm sekmesine çerez tabanlı (tarayıcı bazlı, hesap gerektirmeyen) tarih/saat formatı tercihi (tr-TR/en-US) eklendi | ✅ Tamamlandı, `dotnet build` 0 hata; tarayıcıda görsel doğrulama bekliyor |
| Ek-9 | Ana Sayfa/Alarmlar/Veri Tutarlılık Kontrolü/Ayarlar ekranlarındaki saatlerin sunucu saat dilimi yerine tarayıcı saat dilimine göre gösterilmesi (`ClientTimeZoneService` + JS interop, bkz. `memory.md` Teknik Tuzaklar) | ✅ Tamamlandı, `dotnet build` 0 hata; tarayıcıda görsel doğrulama bekliyor |
| Ek-10 | **Şema Kataloğu** (FR-15/FR-16): kayıtlı her bağlantının tablo/kolon/indeks/kısıt bilgisinin periyodik salt-okuma taramasıyla toplanıp metadata DB'de "güncel durum" olarak saklanması, `/schema-catalog` katalog ekranı (şema filtresi, tablo+kolon adı araması, genişletilebilir kolon/indeks/kısıt sekmeleri, "Şimdi Tara"), `/schema-catalog/changes` şema değişiklik günlüğü, `/schema-catalog/report` rapor ekranı (tüm kolonlar + tablo/kolon açıklamaları; veritabanı/şema/arama/publication/"açıklaması olmayanlar"/"silinenler" filtreleri, tablo bazlı grup renklendirmesi, seçilebilir sayfa boyutuyla sayfalama, filtreleri koruyan **Excel (.xlsx) export**), Ayarlar'a Şema Kataloğu sekmesi ve periyodik taramayı **aç/kapa** anahtarı, retention entegrasyonu | ✅ Tamamlandı, gerçek PostgreSQL üzerinde uçtan uca doğrulandı (tarama → değişiklik tespiti → UI) |

## Mimari Özet

Katmanlı çözüm: `CdcMonitoring.Domain` / `.Application` / `.Infrastructure` / `.Web` (Blazor Server). Altı arka plan işi (health check, CDC keşfi, alarm değerlendirme, veri tutarlılık kontrolü, şema katalog taraması, retention temizliği) tek bir `SchedulerTickJob`'da birleşti; her işin gerçek çalışma sıklığı `SystemSettings` tablosundan okunur, çalıştırma hakkı `JobSchedules` tablosunda atomik bir koşullu `UPDATE` ile "claim" edilir (NFR-05, çoklu replika güvenliği). Ayrıntılar için `README.md`.

## Bilinen Açık Konular / Kalan İşler

Bunlar bilinçli olarak kapsam dışı bırakılmış veya gerçek ortamda henüz doğrulanmamış konulardır — BRD'yi ihlal etmez, ama üretime almadan önce netleştirilmeli:

1. **OIDC/SSO entegrasyonu yok (NFR-03).** `ICurrentUserAccessor` altyapısı hazır (`HttpContextCurrentUserAccessor`), ama gerçek bir IdP'ye bağlanmadı — audit loglarında kullanıcı adı yerine "system" görünüyor. Kurumsal IdP detayları (endpoint, client tipi) netleşince ASP.NET Core OIDC middleware eklenmeli.
2. **Quartz clustered Postgres job store kapalı** (`Quartz:UseClusteredPostgresStore=false` varsayılan). Kod hazır ama QRTZ_* şema betiği (`create_postgres_tables.sql`) metadata DB'ye uygulanmadan açılmamalı. Not: `SchedulerTickJob`'un kendi tetikleyicisi için bu artık kritik değil — gerçek iş tekilliği `JobScheduleRepository.TryClaimAsync`'teki atomik UPDATE ile zaten garanti ediliyor; bu ayar yalnızca ek bir tutarlılık katmanı.
3. **.NET sürümü mevcut ~40 mikroservisin standardıyla teyit edilmedi.** Proje başlangıçta .NET 8 LTS varsayımıyla kuruldu, sonradan kullanıcı isteğiyle **.NET 10**'a yükseltildi (tüm projeler `net10.0`, EF Core/Npgsql 10.x). Bu makinede yalnızca .NET 10 SDK kurulu olduğu için başka bir sürüme geri dönmek gerekirse ilgili SDK'nın da kurulması gerekir.
4. **`CdcMonitoring.IntegrationTests` projesi hâlâ boş placeholder.** Plan Testcontainers tabanlı gerçek entegrasyon testleri öneriyordu; bunun yerine yalnızca unit testler (91 adet) + `docker-compose.local.yml` ile manuel/canlı doğrulama yapıldı. İstenirse Testcontainers ile CI'da otomatik çalışacak entegrasyon testleri eklenebilir.
5. **Helm chart gerçek bir Kubernetes cluster'ında test edilmedi** — yalnızca template/values incelemesi ve local docker-compose testi yapıldı.
6. ~~Reconciliation zamanlaması "gün sayısı" tabanlıydı~~ **[Çözüldü]** Artık `ReconciliationIntervalSeconds` olarak saklanıyor, `/settings`'te dakika/saat/gün birimiyle girilebiliyor (hâlâ "son çalışmadan N süre sonra" mantığıyla, BRD'nin "her Pazar 03:00" gibi spesifik gün/saat örneğinden farklı — bu bilinçli basitleştirme sürüyor, spesifik gün/saat kontrolü isteniyorsa ayrı bir cron alanı eklenmeli).
7. **Reconciliation sonuçları ilişki başına tek satırda birleştiriliyor** (tablo bazlı değil) — çok tablolu publication'larda UI'da yalnızca özet metin (`Details` alanı) görünüyor, tablo bazlı ayrı satır/grafik yok.
8. **Alarm çözüldüğünde ayrı bir "resolved" e-postası gönderilmiyor** — bilinçli tasarım kararı (BRD yalnızca tetikleme bildirimini şart koşuyor), ama operasyonel olarak isteniyorsa eklenebilir.
9. **Şema kataloğu büyük veritabanlarında ölçek testi yapılmadı.** Doğrulama en fazla 8 tablo/16 kolonluk test veritabanlarıyla (local fixture: 11 bağlantı) yapıldı; binlerce tablolu bir şemada tarama süresinin varsayılan 30 sn'lik zaman aşımına sığıp sığmadığı gözden geçirilmeli. ~~Üç koleksiyonun tek sorguda `Include` edilmesi (kartezyen çarpım)~~ **[Çözüldü]** — katalog sorgularının üçü de `AsSplitQuery` kullanıyor (EF'in `MultipleCollectionIncludeWarning`'i docker loglarında görülüp giderildi).
10. **K8s → dış PostgreSQL ağ erişimi ve kurumsal SMTP erişimi gerçek ortamda doğrulanmadı** (NFR-01, varsayım BRD §9'da zaten belirtilmiş) — yalnızca local docker-compose ağında test edildi.

## Kod İncelemesinde Bulunup Düzeltilen 10 Bulgu

1. **`ConnectionHealthCheckService`** — paralel health check task'ları aynı scoped `DbContext`'e eşzamanlı `AddAsync` çağırıyordu (thread-safe değil, "a second operation was started on this context" riski). Artık sonuçlar paralel toplanıp `Task.WhenAll` sonrası sırayla ekleniyor.
2. **`CdcDiscoveryService`** — kısmi veri toplama hatası (ör. yalnızca subscription-stats sorgusu zaman aşımına uğrarsa) yanlışlıkla anlık "slot inaktif"/"subscription error" alarmı tetikliyordu (debounce'suz false positive). Artık eksik veri "bu döngüde bilinmiyor" sayılıp o ilişki atlanıyor.
3. **`DependencyInjection`** — Production'da `DataProtection:CertificatePath` yapılandırılmamışsa parola şifreleme anahtarları, şifreli parolalarla aynı DB'de **korumasız** saklanabiliyordu. Artık erken ve net exception fırlatılıyor.
4. **Helm `deployment.yaml`** — `DataProtection__CertificatePath` mount ediliyordu ama `DataProtection__CertificatePassword` hiç set edilmiyordu; parolalı gerçek bir `tls.pfx` başlangıçta patlardı. Artık aynı secret'tan okunuyor.
5. **`ReconciliationService`** — bir ilişkinin kontrolü hata verirse sessizce loglanıp sayılmıyordu; **tüm** ilişkiler hata verirse (ör. genel bağlantı kesintisi) `mismatchSummaries` boş kalıp FR-11'in tek çıktısı olan e-posta hiç gitmiyordu. Artık hatalar da rapora dahil ediliyor.
6. **`AlertEvaluationService`** — `LagWarning` kuralı süreklilik eşiğini **iki kez** uyguluyordu (hem geçmiş pencere filtresi hem de bildirim gecikmesi olarak) — 15 dk ayarlandığında bildirim fiilen ~30 dk'da gidiyordu. Tek gecikmeye indirildi.
7. **`MailKitEmailNotifier`** — alıcı listesindeki tek bir hatalı e-posta adresi `MailboxAddress.Parse`'ı patlatıp **tüm** gönderimi (on-call dahil) iptal ediyordu. Artık yalnızca o alıcı atlanıp loglanıyor.
8. **`JobScheduleRepository` / `SchedulerTickJob`** — bir job claim edildikten hemen sonra hata verirse (`LastRunAt` zaten "now" yazılmış), bir sonraki deneme tam bir interval sonrasına erteleniyordu. Artık başarısız job'un claim'i serbest bırakılıyor (`ReleaseClaimAsync`), bir sonraki 15 sn'lik tick'te tekrar denenir.
9. **`NpgsqlConnectivityChecker`** — Npgsql'in kendi bağlantı/komut zaman aşımı 5 sn'ye sabitlenmişti, `SystemSettings.HealthCheckTimeoutSeconds` (1-60 aralığı) admin tarafından daha yüksek ayarlansa bile sessizce görmezden geliniyordu.
10. **`NpgsqlPostgresInspector`** — aynı sorun keşif/reconciliation tarafında: sabit 10 sn Npgsql zaman aşımı, `DiscoveryTimeoutSeconds` ayarını görmezden geliyordu.

**İncelenip yanlış olduğu kanıtlanan (değiştirilmeyen) 2 aday:**
- vis-network node etiketleri üzerinden stored-XSS iddiası — kütüphane yalnızca `&lt;canvas&gt;`'a çiziyor, `innerHTML` kullanmıyor; risk yok.
- `ConsecutiveHealthCheckFailures=0` senaryosu — migration seed'i `2`, UI da `[Range(1,20)]` ile zorluyor; `0` uygulamanın kendi yolundan asla ulaşılamaz.

**Bilinçli olarak düzeltilmeyen, düşük öncelikli bulgular** (kapsam dışı bırakıldı, gerekirse ayrı ele alınmalı):
- `NpgsqlConnectivityChecker`'da açık bir `SslMode` belirtilmemiş.
- `PgConnection → ConnectionHealthCheck` cascade-delete, bağlantı silinince geçmiş health check kayıtlarını da siliyor.
- `Relationships/Create.razor` kaynak=hedef aynı bağlantı olacak şekilde manuel ilişki girişine izin veriyor.
- `Program.cs` fatal başlangıç hatasında process'i sıfır olmayan bir exit code ile kapatmıyor.
- `docker-compose.local.yml` / `appsettings.Development.json`'daki düz metin dev parolaları (yalnızca local dosyalar, kapsamlı bir secret-yönetimi kararı gerektirir).

Tüm bu bulgular dahil olmak üzere yukarıdaki tüm çalışma commit edilmiştir.
