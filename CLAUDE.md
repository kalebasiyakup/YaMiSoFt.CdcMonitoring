# CDC Monitoring (CDC-MON-2026)

PostgreSQL mantıksal replikasyon ilişkilerini kayıt altına alan, izleyen ve e-posta ile bildiren salt
gözlem bir .NET uygulaması. Gereksinimler: `brain/raw/CDC_Monitoring_BRD.md` (kanonik kopya, kökteki kopya
kaldırıldı). Kullanım ve yerel test: `README.md`.

## Süreç takibi `brain/` klasöründe

Bu projenin hafızası, kararları, görevleri ve oturum devirleri `brain/` klasöründeki vault'ta tutulur.
Eskiden `memory.md` ve `PROJE_DURUMU.md` vardı, artık yoklar (son halleri `brain/raw/` altında arşiv).

**Oturum başında** şu dosyaları oku (yollar `brain/` altında):

1. `companion/Core.md`
2. `companion/Rules.md`, ve `[scope: cdc-monitoring]` satırı gösterdiği için `projects/cdc-monitoring/Rules.md`
3. `companion/Last-Session.md` (en üstteki kart)
4. `companion/Threads.md`
5. `tasks/` altında `status: active`, `waiting` ve `blocked` olan görevler

**Çalışırken:**

- Karar, tuzak ya da mimari bilgi kalıcıysa `brain/wiki/` altına yaz (`decisions/`, `concepts/`,
  `architecture/`), `origin: session`. Yazmadan önce `brain/AGENTS.md` dosyasını oku, frontmatter ve
  tazelik kuralları orada.
- Yeni iş ya da açık kalan şey `brain/tasks/<slug>.md` olur, biçimi `brain/.claude/skills/continuity/SKILL.md`
  içinde "Görevler" bölümünde.
- `brain/raw/` altına yazma, taşıma ya da silme yapma, o klasör kullanıcıya aittir. BRD'yi (v0.4 gibi
  yeni sürümleri) yalnız kullanıcı orada elle günceller, sonra "BRD'yi yeniden ingest et" der. Ajan BRD'yi
  düzenlemez, yalnız wiki'yi günceller.

**Anlamlı bir iş parçası bitince** devir kartını `brain/companion/Last-Session.md` en üstüne yaz ve
`companion/Threads.md` içinde ilgili konuyu yerinde güncelle. Protokol `continuity` SKILL.md dosyasında.

Vault'un skill ve ajan tanımları (`llm-wiki`, `continuity`, `archivist`, `scribe`) `brain/.claude/`
altında durur. Kök dizinden açılan oturumda bunlar otomatik yüklenmeyebilir, dosyaları yoluyla oku.
Ingest ve denetim gibi vault bakım işlerini `brain/` içinden açılan bir oturumda yapmak daha sağlamdır.

## Proje kuralları

Kısa liste. Tamamı ve gerekçeleri `brain/projects/cdc-monitoring/Rules.md` içinde:

- Arayüz metinleri Türkçe, kod İngilizce. "Reconciliation" ekranda "Veri Tutarlılık Kontrolü".
- Özellik bitmeden gerçek PostgreSQL replikasyonu ve Mailpit ile uçtan uca canlı doğrula.
- Uygulama izlenen PostgreSQL'de hiçbir yazma yapmaz, tüm sorgular SELECT ile başlar (`ReadOnlyGuardTests`).
- Gerçek işveren adı ve iç altyapı adı sızdırma, bu repo public olabilir.
- README.md güncel tutulur, özellikle "Local Geliştirme ve Test".

Teknik tuzaklar için önce `brain/wiki/concepts/` altındaki `postgresql-izleme-tuzaklari`,
`blazor-server-tuzaklari`, `efcore-tuzaklari` ve `dotnet-docker-tuzaklari` sayfalarına bak.
