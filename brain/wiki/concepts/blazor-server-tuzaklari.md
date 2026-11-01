---
type: concept
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/memory.md
origin: source
summary: Blazor Server geliştirmesinde karşılaşılan tuzaklar, DbContext paralelliği, prerender JS interop, saat dilimi, ilk form gönderimi ve test aracı notları
freshness: wiring
tags: [blazor, tuzaklar, uygulama]
links: [cdc-monitoring-memory]
---

# Blazor Server tuzakları

## Context

CDC izleme uygulamasının arayüzü Blazor Server ([[cdc-monitoring-uygulama]]). [[cdc-monitoring-memory]]
bu yığında tekrar eden hataları kaydeder.

## Content

- **Scoped servisleri `Task.WhenAll` ile paralel çağırma.** Aynı circuit scope'undaki `DbContext`
  eşzamanlı kullanılır ve `InvalidOperationException: A second operation was started on this context
  instance` ile sayfa 500'e düşer. Aynı sınıf hata kod incelemesinde health check servisinde
  düzeltilmişti, Ana Sayfa dashboard'u yazılırken bir kez daha yapıldı. Kural: bir bileşenin `@code`
  bloğunda birden fazla scoped servis sırayla `await` edilir. Yalnız `IServiceScopeFactory` ile açılan
  bağımsız scope'larda (arka plan işleri) paralellik güvenlidir ([[efcore-tuzaklari]]).
- **Prerender fazında JS interop.** `@rendermode InteractiveServer` sayfalar önce statik prerender,
  sonra circuit ile iki kez render edilir. `OnInitializedAsync` içinden JS import çağrısı prerender'da
  "JavaScript interop calls cannot be issued at this time" ile 500 verir. Çözüm: çağrıyı
  `RendererInfo.IsInteractive` koşuluna bağlamak. Circuit çağrı sırasında kapanırsa fırlayan
  `JSDisconnectedException` da yakalanmalı.
- **Saat dilimi.** `DateTimeOffset.ToLocalTime()` tarayıcının değil sunucunun saat dilimini kullanır,
  culture yalnız biçimi etkiler. Çözüm: ilk render sonrası JS interop ile tarayıcının IANA saat
  dilimini alan `ClientTimeZoneService` ve `FormatLocal(...)` kullanan ortak taban bileşen.
- **İlk form gönderimi sessizce başarısız olabilir**, SignalR circuit henüz tam kurulmamışken. "Attempting
  to reconnect" görülür, birkaç saniye sonra yeniden tıklamak yeter. Sonucu ekrandan değil DB'den
  doğrulamak daha güvenilir. Kök neden bir imaj sorunu olabilir, bkz. [[dotnet-docker-tuzaklari]]
  (`blazor.web.js` 404).
- **`#blazor-error-ui` her zaman DOM'da** (CSS ile gizli). Erişilebilirlik ağacı onu her zaman
  "generic" olarak listeler, gerçekten görünür olup olmadığını ekran görüntüsü ya da
  `getComputedStyle(...).display` söyler.
- **Dosya indirme bağlantısı** `target="_top"` taşımalı, yoksa Blazor'un enhanced navigation'ı bağlantıyı
  ele geçirir.
- **Tarayıcı otomasyon aracı**: Chrome automation'ın `left_click`'i bazen `button[type=submit]`
  üzerinde olayı sayfaya iletmez. Güvenilir yedek, `javascript_tool` ile
  `document.querySelector('button[type="submit"]').click()`.

## Related

- [[cdc-monitoring-memory]] — tuzakların kaynağı
- [[cdc-monitoring-uygulama]] — arayüzün bağlı olduğu mimari
- [[efcore-tuzaklari]] — DbContext paralelliğinin veri katmanı yüzü
- [[dotnet-docker-tuzaklari]] — circuit'in hiç kurulamamasına yol açan imaj hatası
