---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
tags: [task]
links: []
---

# Bilerek ertelenen beş düşük öncelikli kod incelemesi bulgusu

Kod incelemesinde düzeltilmeyen bulgular ([[cdc-monitoring-proje-durumu]]):

1. `NpgsqlConnectivityChecker`'da açık `SslMode` yok.
2. `PgConnection` silinince cascade-delete geçmiş health check kayıtlarını da siliyor.
3. `Relationships/Create.razor` kaynak ile hedefi aynı bağlantı seçen manuel ilişkiye izin veriyor.
4. `Program.cs` fatal başlangıç hatasında sıfırdan farklı exit code ile çıkmıyor.
5. `docker-compose.local.yml` ve `appsettings.Development.json`'da düz metin dev parolaları, kapsamlı bir
   secret yönetimi kararı gerektiriyor ([[2026-09-17-public-repo-apache-lisans]]).

- Sıradaki adım: her biri için yap ya da bırak kararı vermek, yapılacaklar ayrı görev olur.
- Sahibi: [[yakup-kalebasi]]
