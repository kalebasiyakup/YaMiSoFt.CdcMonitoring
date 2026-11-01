# Log

Yalnız sona eklenir. Biçim: `## [YYYY-MM-DD] işlem | başlık`

Önekler: `ingest`, `query`, `lint`, `handoff`, `schema`.

Son beş kaydı okumak için: `grep "^## \[" LOG.md | tail -5`

## [YYYY-MM-DD] schema | vault düzeni kuruldu
- Oluşturulan: AGENTS.md, index.md, log.md, _schema/, wiki/, companion/, notes/, tasks/,
  projects/, scripts/, .claude/agents/, .claude/skills/
- Katalog üretiliyor: `py -3 scripts/generate-index.py .`
- Bu satırı kurulum tarihiyle güncelle, sonraki kayıtları altına ekle.

## [2026-11-01] ingest | CDC Monitoring BRD (CDC-MON-2026, v0.3)
- Oluşturulan: [[cdc-monitoring-brd]], [[cdc-monitoring-sistemi]], [[postgresql-mantiksal-replikasyon]], [[cdc-alarm-esikleri]], [[2026-09-12-kafka-yerine-yerlesik-replikasyon]], [[2026-09-12-salt-gozlem-kisiti]], [[2026-09-12-bildirim-kanali-eposta]]
- Güncellenen: [[overview]]
- Çelişki: [[cdc-alarm-esikleri]] içinde FR-10 ile Bölüm 8 (lag önceliği), [[cdc-monitoring-brd]] içinde FR-15/16 kapsam dışı kalması işaretlendi; ikisi de belgenin kendi içinde

## [2026-11-01] ingest | CDC Monitoring BRD, tamamlama
- Oluşturulan: [[2026-09-12-iliski-cikarimi-kullanici-onayi]], [[2026-09-12-zaman-serisi-icin-grafana]]
- Güncellenen: [[cdc-monitoring-brd]], [[cdc-monitoring-sistemi]], [[overview]]
- Çelişki: [[2026-09-12-iliski-cikarimi-kullanici-onayi]] içinde FR-06 önceliği (Orta) ile riskler tablosundaki "onay zorunlu" ifadesi işaretlendi; belgenin kendi içinde
- Karar tarihi belgede ayrı verilmedi, iki yeni karar sayfası belge tarihini (2026-09-12) taşıyor

## [2026-11-01] lint | CDC Monitoring BRD ingest sonrası
- Betiğin ölçtüğü: 12 not, 105 bağlantı, puan 100 / 100, katalog güncel (10 sayfa)
- İşaretlenmiş çelişki: 5 (3 sayfada), hepsi belgenin kendi içinde
- Atıf örneklemesi: 7 sayfa (protokolün 5 sayfa sınırının üstünde, ingest'te dokunulan 10 sayfanın tamamı küçük olduğu için tek grep turu yetti), 25 iddia grep ile doğrulandı, bulunamadı ya da kaymış 0

## [2026-11-01] ingest | CDC Monitoring PROJE_DURUMU ve memory
- Oluşturulan: [[cdc-monitoring-proje-durumu]], [[cdc-monitoring-memory]], [[cdc-monitoring-uygulama]], [[2026-09-17-parola-sifreleme-data-protection]], [[2026-09-17-tick-zamanlama-atomik-claim]], [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]], [[2026-09-17-kafka-topic-erisim-raporu]], [[2026-09-17-kafka-tls-dogrulama-kapali]], [[postgresql-izleme-tuzaklari]], [[blazor-server-tuzaklari]], [[efcore-tuzaklari]], [[dotnet-docker-tuzaklari]]
- Güncellenen: [[cdc-monitoring-brd]], [[cdc-alarm-esikleri]], [[2026-09-12-kafka-yerine-yerlesik-replikasyon]], [[2026-09-12-iliski-cikarimi-kullanici-onayi]], [[2026-09-12-salt-gozlem-kisiti]], [[postgresql-mantiksal-replikasyon]], [[cdc-monitoring-sistemi]], [[overview]]
- Çelişki (işaretlendi, çözülmedi): Kafka dışlaması ile /kafka-audit, Kafka raporunun gerçek cluster’a karşı denenip denenmediği (iki kaynak farklı), BRD ile uygulanmış hal (parola, reconciliation sıklığı, Quartz, SSO), tablo düşüşünde olay üretimi (memory.md kendi içinde), test sayısı (PROJE_DURUMU kendi içinde), "Pazar 03:00" atıfı BRD’de bulunamadı
- Kararların kesin günü kaynaklarda yok, dosya adlarındaki 2026-09-17 PROJE_DURUMU’nun yazılı tarihi
- Dosya adı çakışması: kökte 0 bayt efcore-tuzaklari.md (Obsidian kırık bağlantıdan üretmiş olabilir) wiki sayfasını gölgeliyor, dokunulmadı

## [2026-11-01] lint | PROJE_DURUMU ve memory ingest sonrası
- Betiğin ölçtüğü: 24 not, 297 bağlantı, puan 97 / 100, katalog güncel (22 sayfa). Tek bulgu yetim efcore-tuzaklari, nedeni yukarıdaki kök dosya
- İşaretlenmiş çelişki: 17 (11 sayfada)
- Atıf örneklemesi: 5 sayfa, 2 küçük atf kayması (ifade yanlış dosyaya atıflı, commit ibaresi kapsamı eksik), ikisi de düzeltildi, içerik hatası yok

## [2026-11-01] ingest | PROJE_DURUMU ve memory, eksik karar sayfaları
- Yeni ham dosya yoktu. memory.md’de gerekçesi yazılı ama sayfası olmayan beş seçim karar sayfasına çevrildi
- Oluşturulan: [[2026-09-17-blazor-server-secimi]], [[2026-09-17-excel-export-xlsx]], [[2026-09-17-tablo-aciklamalari-configure]], [[2026-09-17-public-repo-apache-lisans]], [[2026-09-17-veri-tutarlilik-kontrolu-terimi]]
- Güncellenen: [[cdc-monitoring-uygulama]], [[cdc-monitoring-memory]], [[overview]]
- Kararların kesin günü kaynaklarda yok, dosya adlarındaki 2026-09-17 PROJE_DURUMU’nun yazılı tarihi
- Açık: .NET 10 yükseltmesi için karar sayfası açılmadı, kaynaklar yalnız kullanıcı isteği diyor, gerekçe yok

## [2026-11-01] lint | karar sayfaları sonrası
- Betiğin ölçtüğü: 29 not, 375 bağlantı, puan 97 / 100, katalog güncel (27 sayfa). Tek bulgu yine yetim efcore-tuzaklari, nedeni kökteki 0 bayt dosya
- İşaretlenmiş çelişki: 17 (11 sayfada), değişmedi
- Atıf örneklemesi: 5 yeni sayfa, ayırt edici ifadelerin tamamı grep ile doğrulandı. Yazım sırasında iki düzeltme yapıldı: doğrulanamayan bir "ham kaynaklarda işveren adı yok" cümlesi silindi, FR-11'in reconciliation sözcüğünü kullandığı iddiası BRD'ye göre düzeltildi

## [2026-11-01] handoff | süreç takibi brain’e taşındı
- Oluşturulan: projects/cdc-monitoring/Proje.md ve Kurallar.md, tasks/ altında 14 görev, ana projede CLAUDE.md
- Güncellenen: companion/Core.md, Kurallar.md, Threads.md, Last-Session.md, [[cdc-monitoring-memory]], [[cdc-monitoring-proje-durumu]]
- Ana projede: Dockerfile ve KafkaAdminClientFactory.cs yorumları düzeltildi, memory.md ve PROJE_DURUMU.md silindi (raw/ kopyaları aynıydı, git’ten geri alınabilir), commit atılmadı
- Kökteki boş efcore-tuzaklari.md kullanıcı tarafından silinmiş, gölgeleme sorunu kalktı

## [2026-11-01] lint | yeniden denetim, yeni kaynak yok
- Yeni ham dosya yoktu, raw/ altındaki dört dosyanın üçü wiki’de karşılığı var, raw/README.md vault’ün şablonu
- Betiğin ölçtüğü: 29 not, 386 bağlantı, puan 100 / 100, katalog güncel (27 sayfa), işaretli çelişki 17
- Atıf örneklemesi: ilk kez tasks/ sayfaları örneklendi (5 sayfa, 20 ayrıt edici ifade BRD ve PROJE_DURUMU’nda bulundu). Bir kayma: helm-k8s-testi içinde çıkarım kaynak söylüyor gibi yazılmıştı, çıkarım olarak işaretlendi
- Yeni karar sayfası açılmadı: .NET 10, CSS tabanlı UI modernizasyonu ve hiyerarşik topoloji için kaynaklarda yazılı gerekçe yok

## [2026-11-01] handoff | kökteki BRD kopyası kaldırıldı
- Güncellenen: [[cdc-monitoring-brd]], [[wiki-celiskilerini-karara-bagla]], projects/cdc-monitoring/Proje.md ve Kurallar.md, companion/Last-Session.md
- Ana projede: README.md ve CLAUDE.md BRD yolu brain/raw’a çevrildi, CDC_Monitoring_BRD.md silindi (raw/ ile aynıydı, git’ten geri alınabilir), commit atılmadı
- Kural: BRD güncellemesini yalnız kullanıcı raw/ içinde yapar, sonra yeniden ingest istenir

## [2026-11-01] handoff | kişi notu ve Core bağlantıları
- Oluşturulan: notes/kisiler/yakup-kalebasi.md (görev sahibi olarak, kullanıcının isteği)
- Güncellenen: 14 görev dosyası (Sahibi satırı), companion/Core.md (Bağlantılar bölümü), projects/cdc-monitoring/Proje.md, companion/Last-Session.md
- Neden: Core, Journal, Kurallar ve Last-Session hiçbir yerden bağlı değildi, çünkü yollar düz metin olarak anılmıştı

## [2026-11-01] ingest | kaynak sayfalarına ham dosya bağlantıları
- Güncellenen: [[cdc-monitoring-brd]], [[cdc-monitoring-proje-durumu]], [[cdc-monitoring-memory]] (İlgili bölümüne raw/ bağlantısı)
- Neden: raw/ dosyaları grafikte tek başına duruyordu, source alanı bağlantı sayılmıyordu. raw/ dosyalarına dokunulmadı

## [2026-11-01] schema | iskelet İngilizce adlara ve yeni sürüme taşındı
- Yeniden adlandırılan: `companion/Kurallar.md` → `Rules.md`, `companion/Arsiv/` → `Archive/`, `notes/kisiler/` → `notes/people/`, `projects/cdc-monitoring/Kurallar.md` → `Rules.md`, `Proje.md` → `Project.md`, `index.md` → `INDEX.md`, `Denetim.cmd` → `lint.cmd`, `_schema/kisi.md` → `person.md`, `.claude/skills/sureklilik/` → `continuity/`.
- Değişen biçimler: sayfa bölüm başlıkları (`## Bağlam/İçerik/İlgili` → `## Context/Content/Related`), işaretçi satırı, damga biçimi, `[kapsam: x]` → `[scope: x]`, kişi notundaki `facts` anahtarları, katalog bölüm adları.
- Betikler, ajanlar, skill dosyaları ve `AGENTS.md` yeni iskeletten alındı; kurulumdan beri değişmemiş olanlar değiştirildi, kullanıcı içeriği (wiki, görevler, companion, notlar) yalnız adları güncellendi. `raw/` altındaki kaynak dosyalara dokunulmadı; `raw/articles`, `papers`, `datasets`, `images` klasörleri açıldı.
- Yedek: oturum geçici klasöründe. Ölçüm: katalog güncel (27 sayfa), 100/100, genişletilmiş kapsamda kırık bağlantı ve yetim sayfa yok.

## [2026-11-01] schema | grafikte açıkta kalanlar kapatıldı
- Güncellenen: _schema/template.md (örnek bağlantılar backtick içine alındı, hayalet düğüm üretmesin), _schema/conventions.md (Şıablonlar bölümü, [[template]] ve [[person]] bağlantısı)
- .obsidian/graph.json: dolayı geçici „-path:_schema” filtresi kaldırıldı, _schema artık bağlı ve görünür
- Ana projede: kök CLAUDE.md eski adlardan (Kurallar, sureklilik, [kapsam]) yeni adlara (Rules, continuity, [scope]) güncellendi
- Kökteki boş baska-sayfa.md ve efcore-tuzaklari.md Obsidian’ın ürettikleri boş dosyalardı, ikisi de silinmiş

## [2026-11-01] schema | log.md → LOG.md, companion/archive ve renk grupları
- Yeniden adlandırılan: `log.md` → `LOG.md`, `companion/Archive/` → `companion/archive/`. Kullanıcı içeriğinde yalnız bu adlara giden bağlantı ve yol metinleri güncellendi.
- Altyapı yenilendi: betikler, ajanlar, skill dosyaları, `AGENTS.md`, `CLAUDE.md`, `lint.cmd`, izin örneği. `_schema/template.md` ve `_schema/conventions.md` kullanıcı düzenlemeleri (şablon bağlantılarının backtick içinde durması, `Şablonlar` bölümü) taşıdığı için değiştirilmedi.
- Düzeltme: önceki taşıma `conventions.md` içindeki "eski biçimler hâlâ tanınır" cümlesini yanlışlıkla yeni biçime çevirmişti, eski haline getirildi.
- Grafik: `wiki/overview`, `wiki/archive`, `projects/` ve kök belgeler için renk grubu eklendi.

## [2026-11-01] handoff | commit kontrolü
- Güncellenen: companion/Last-Session.md, companion/Threads.md
- Bulgu: git log.md olarak izliyor, disk ve referanslar LOG.md (core.ignorecase=true); commit kapısı kapalı (core.hooksPath boş)
- Temiz: çalışma ağacı, taşımalar R100, gizli dosya izlenmiyor, gerçek Aiven host geçmişte yok
