# İş Gereksinimleri Dokümanı (BRD)
## PostgreSQL Bağlantı Kayıt Defteri ve CDC Takip Uygulaması

- **Proje Kodu:** CDC-MON-2026
- **Hazırlayan:** Yakup Kalebaşı — Yazılım Çözümleri Mimarı, YaMiSoFt
- **Tarih:** 12.09.2026
- **Durum:** Taslak — v0.3

---

## Revizyon Geçmişi

| Versiyon | Tarih | Açıklama | Yazan |
|---|---|---|---|
| 0.1 | 11.09.2026 | İlk taslak — Kafka/Debezium tabanlı CDC izleme varsayımıyla hazırlandı | Yakup Kalebaşı |
| 0.2 | 12.09.2026 | Mimari düzeltildi: Kafka/Strimzi/Debezium kapsam dışı bırakıldı. PostgreSQL örnekleri Kubernetes dışında çalışıyor; uygulama önce bağlantıları kayıt altına alan bir defter, sonrasında bağlantılar arası CDC ilişkilerini gösteren bir takip ekranı sağlayacak şekilde yeniden tanımlandı. | Yakup Kalebaşı |
| 0.3 | 12.09.2026 | Uygulamanın **salt gözlem (read-only)** amaçlı olduğu netleştirildi — PostgreSQL üzerinde publication/subscription/replication slot oluşturma, silme veya yeniden oluşturma (recreate) gibi hiçbir yazma işlemi yapılmayacaktır. Bildirim kanalı Slack'ten **e-posta**'ya değiştirildi. | Yakup Kalebaşı |

---

## 1. Yönetici Özeti

Şirketin CDC (Change Data Capture) ihtiyacı, Kubernetes cluster'ı dışında (on-prem sunucularda) çalışan PostgreSQL örnekleri arasında, doğrudan PostgreSQL'in yerleşik mantıksal replikasyon (logical replication / publication-subscription) mekanizması ile karşılanmaktadır. Kafka, Strimzi veya Debezium gibi ara bileşenler kullanılmamaktadır.

Mevcut durumda hangi PostgreSQL örneklerinin var olduğu, bunlar arasında hangi CDC ilişkilerinin (publication → subscription) kurulu olduğu ve bu ilişkilerin sağlık durumu merkezi olarak kayıt altında ve görünür değildir. Bu doküman, önce her bir PostgreSQL bağlantısının kayıt altına alınmasını sağlayan bir bağlantı defteri (connection registry), ardından bu kayıtlı bağlantılar arasındaki CDC ilişkilerini keşfeden, sağlık durumunu izleyen ve bir takip ekranında görselleştiren bir uygulamanın iş gereksinimlerini tanımlar.

> **Önemli:** Bu uygulama tamamen **salt gözlem (read-only / observability)** amaçlıdır. PostgreSQL örnekleri üzerinde publication, subscription veya replication slot **oluşturma, silme veya yeniden oluşturma (recreate)** gibi herhangi bir yazma/değiştirme işlemi yapmaz. Uygulama yalnızca mevcut durumu okur, kaydeder ve raporlar; CDC yapılandırmasına dair her türlü işlem (kurulum, bozulma sonrası yeniden kurulum, silme) ilgili veritabanı ekibi tarafından, uygulama dışında, manuel olarak yapılmaya devam eder.

---

## 2. İş Arka Planı ve Problem Tanımı

### 2.1 Mevcut Durum

- PostgreSQL örnekleri Kubernetes cluster'ının dışında, ayrı sunucularda (on-prem, farklı veri merkezlerinde olabilir) çalışmaktadır.
- CDC, PostgreSQL'in kendi mantıksal replikasyon altyapısı (publication/subscription, replication slot) ile örnekler arasında doğrudan kurulmaktadır.
- Kafka, Strimzi veya Debezium bu akışın bir parçası değildir.
- Şirket içi ~40 .NET mikroservisi on-prem Kubernetes cluster'da çalışmaktadır; gözlemlenebilirlik altyapısı (Prometheus/Grafana/Alertmanager, ELK) mevcuttur ve bu uygulama da o cluster üzerinde çalışacaktır.

### 2.2 Problem

- Hangi PostgreSQL bağlantılarının (host/port/db) var olduğuna dair merkezi, güncel bir kayıt (envanter) yoktur — bilgi dağınık ve kişilere bağımlıdır.
- Bağlantılar arasında hangi CDC ilişkilerinin (kim kimin publication'ına subscribe oluyor) kurulu olduğu tek bir yerden görülememektedir.
- Replication slot'un sessizce inaktif kalması, subscription'ın disabled/error durumuna düşmesi veya lag'in büyümesi merkezi olarak izlenmemektedir; bu durumlar disk dolması (WAL birikimi) veya hedef tarafın güncel olmaması gibi sonuçlar doğurabilir.
- Yeni bir CDC ilişkisi kurulduğunda veya bir bağlantı bilgisi değiştiğinde (parola rotasyonu, host değişikliği) bunu takip edecek bir süreç/araç yoktur.

### 2.3 İş Etkisi

- Envanter eksikliği → operasyonel körlük, hangi sistemin hangi sisteme bağımlı olduğunun bilinmemesi.
- Fark edilmeyen CDC kopması → hedef ortamın güncel olmaması, olası veri tutarsızlığı.
- Disk dolma riski → inaktif slot nedeniyle kaynak PostgreSQL'de WAL birikimi.
- Manuel/ad-hoc kontrol → operasyonel yük, yavaş kök neden analizi.

---

## 3. Hedefler ve Başarı Kriterleri

| # | Hedef | Başarı Kriteri (KPI) |
|---|---|---|
| G1 | Tüm PostgreSQL bağlantılarının merkezi bir defterde kayıt altına alınması | Prod/DR ortamındaki tüm bilinen PostgreSQL örnekleri sistemde kayıtlı |
| G2 | Kayıtlı bağlantılar arasındaki CDC ilişkilerinin otomatik keşfi ve görünürlüğü | Tüm aktif publication/subscription çiftleri takip ekranında topoloji olarak görünür |
| G3 | CDC sağlık durumunun gerçek zamanlı izlenmesi | Slot/subscription durumu ve lag, takip ekranında ve Prometheus'ta ≤30 sn gecikmeyle güncel |
| G4 | Kritik arızaların proaktif tespiti | Ortalama tespit süresi (MTTD) < 5 dakika |
| G5 | Disk dolma riskinin önlenmesi | Slot inaktif/wal_status kritik durumunda 5 dk içinde e-posta ile alarm tetiklenir |
| G6 | Sessiz veri kaybının yakalanması | Haftalık reconciliation raporu ile kaynak-hedef tutarsızlığı tespit edilir |

---

## 4. Kapsam

### 4.1 Kapsam Dahilinde

- Bağlantı Kayıt Defteri: PostgreSQL bağlantılarının (host, port, veritabanı, kullanıcı, parola, ortam/DC etiketi, açıklama) CRUD ile kaydedilmesi *(not: bu CRUD yalnızca uygulamanın kendi kayıt defteri metadatasına yöneliktir; PostgreSQL üzerindeki gerçek CDC nesnelerine dokunmaz — bkz. 4.3)*.
- Bağlantı bilgilerinin (özellikle parola) şifreli saklanması.
- Her kayıtlı bağlantı için periyodik sağlık kontrolü (bağlanabilirlik, sürüm bilgisi) — **salt okuma** sorguları ile.
- Her bağlantıda `pg_replication_slots`, `pg_publication`, `pg_subscription`, `pg_stat_subscription`, `pg_stat_replication` görünümlerinin **salt okuma** ile sorgulanarak CDC ilişkilerinin otomatik keşfi.
- Keşfedilen ilişkilerin kullanıcı tarafından onaylanabildiği/düzenlenebildiği bir eşleştirme (mapping) mekanizması — bu onay yalnızca uygulamanın kendi metadata kaydını etkiler, PostgreSQL'deki publication/subscription nesnesini değiştirmez.
- Bağlantılar arası CDC ilişkilerini bir topoloji/graf olarak gösteren takip ekranı (kaynak → hedef, durum, lag).
- Aynı verilerin Prometheus `/metrics` endpoint'i ile mevcut Alertmanager'a entegrasyonu.
- Kritik durumlarda **e-posta ile bildirim**.
- Haftalık kaynak-hedef reconciliation (satır sayısı/checksum karşılaştırması) — **salt okuma** sorguları ile.

### 4.2 Kapsam Dışında

- Kafka, Strimzi, Debezium veya benzeri bir mesajlaşma/CDC-connector altyapısı — bu proje kapsamında kullanılmamaktadır.
- PostgreSQL kurulumu, mantıksal replikasyonun (`wal_level=logical`, publication/subscription) ilk kurulumu — bunların önceden yapılandırılmış olduğu varsayılır.
- Mevcut Grafana'nın yerini alacak genel amaçlı bir zaman serisi görselleştirme aracı geliştirmek; takip ekranı yalnızca bağlantı/topoloji/ilişki yönetimine odaklıdır, zaman serisi grafikleri için Grafana kullanılmaya devam eder.
- Kullanıcı/kimlik yönetimi altyapısının (SSO/IdP) sıfırdan kurulması — mevcut kurumsal kimlik doğrulama entegre edilecektir.
- Slack veya benzeri bir sohbet/mesajlaşma entegrasyonu — bildirimler yalnızca e-posta ile yapılacaktır.

### 4.3 Açık Kısıt: Salt Gözlem (Read-Only), Yazma/Değiştirme İşlemi Yok

- Uygulama, izlediği hiçbir PostgreSQL örneğinde publication, subscription veya replication slot **oluşturmaz, silmez veya yeniden oluşturmaz (recreate)**.
- Uygulama, kritik bir duruma (ör. subscription error, slot inaktif) otomatik müdahale **etmez** — herhangi bir "restart", "recreate", "drop/create" gibi düzeltici aksiyon almaz. Tek çıktısı **e-posta bildirimi ve raporlamadır**; düzeltici aksiyon her zaman ilgili ekip tarafından manuel yapılır.
- Uygulamanın PostgreSQL bağlantılarına kullandığı veritabanı rolü, teknik olarak da salt-okunur (read-only) olmalıdır (bkz. NFR-02); bu, yanlışlıkla yazma işlemi yapılmasını mimari düzeyde engeller.

---

## 5. Paydaşlar

| Rol | Kişi/Ekip | Sorumluluk |
|---|---|---|
| İş Sahibi / Mimar | Yakup Kalebaşı | Gereksinim tanımı, mimari onay |
| Platform/DevOps Ekibi | TBD | K8s dağıtımı, ağ erişimi (K8s → dış PostgreSQL), Prometheus/Alertmanager entegrasyonu, SMTP entegrasyonu |
| Veritabanı Ekibi | TBD | Her PostgreSQL örneğinde salt-okunur izleme rolü/izinleri, publication/subscription bilgisi, gerektiğinde manuel düzeltici aksiyon |
| Geliştirme Ekibi | TBD | .NET uygulaması (backend + takip ekranı) geliştirme ve bakım |

---

## 6. Fonksiyonel Gereksinimler

| ID | Gereksinim | Öncelik |
|---|---|---|
| FR-01 | Sistem, bir PostgreSQL bağlantısını host, port, veritabanı adı, kullanıcı, parola, ortam/DC etiketi ve açıklama ile kaydetme (create), listeleme, güncelleme ve silme (CRUD) imkânı sunmalıdır. *(Bu CRUD yalnızca uygulamanın kendi kayıt defteri metadatasınadır; PostgreSQL'e yazma yapmaz.)* | Yüksek |
| FR-02 | Kayıtlı bağlantıların parolaları şifreli saklanmalı (K8s Secret / Vault), hiçbir ekranda veya logda açık metin görünmemelidir. | Yüksek |
| FR-03 | Sistem, her kayıtlı bağlantı için periyodik olarak (Npgsql ile, yapılandırılabilir aralık, varsayılan 30 sn) **salt okuma** ile bağlanabilirlik kontrolü yapmalı ve sonucu (up/down, gecikme) kaydetmelidir. | Yüksek |
| FR-04 | Sistem, her bağlantıda `pg_replication_slots`, `pg_stat_replication`, `pg_publication`, `pg_subscription` ve `pg_stat_subscription` görünümlerini **yalnızca SELECT ile** sorgulamalıdır; hiçbir koşulda bu nesneler üzerinde DDL/DML çalıştırılmamalıdır. | Yüksek |
| FR-05 | Sistem, toplanan publication/subscription bilgilerinden yola çıkarak hangi bağlantının hangi bağlantıya CDC ile bağlı olduğunu otomatik olarak çıkarımlamalı (ör. subscription'ın conninfo'sundaki hedef host/db eşleştirmesi ile) ve bunu bir ilişki (edge) olarak **kendi metadata deposunda** kaydetmelidir. | Yüksek |
| FR-06 | Kullanıcı, otomatik çıkarımlanan ilişkileri görüntüleyebilmeli, onaylayabilmeli, düzenleyebilmeli veya manuel olarak yeni bir ilişki tanımlayabilmelidir; bu işlemler yalnızca uygulamanın metadata kaydını değiştirir, PostgreSQL'deki gerçek publication/subscription nesnesini etkilemez. | Orta |
| FR-07 | Sistem, kayıtlı tüm bağlantıları ve aralarındaki CDC ilişkilerini bir topoloji (graf) ekranında göstermelidir; her düğüm (node) bir PostgreSQL bağlantısını, her kenar (edge) bir CDC ilişkisini temsil etmelidir. | Yüksek |
| FR-08 | Takip ekranında her ilişki (edge) için durum rengi (sağlıklı/uyarı/kritik), replication slot lag'i (byte), subscription durumu (enabled/disabled/error) ve son senkronizasyon zamanı gösterilmelidir. | Yüksek |
| FR-09 | Sistem, aynı sağlık ve lag verilerini Prometheus scrape formatında `/metrics` endpoint'inden sunmalı ve K8s ServiceMonitor ile uyumlu olmalıdır. | Yüksek |
| FR-10 | Sistem, kritik durumlarda (slot > 5 dk inaktif, subscription disabled/error, lag eşik üstü) yapılandırılabilir alıcı listesine **e-posta (SMTP)** ile bildirim göndermelidir. | Yüksek |
| FR-11 | Sistem, haftalık olarak her kayıtlı kaynak-hedef çifti için **salt okuma** ile satır sayısı/checksum karşılaştırması yapan bir job (Quartz.NET) çalıştırmalı ve tutarsızlık raporunu e-posta ile ilgili alıcılara göndermelidir. | Orta |
| FR-12 | Sistem, bağlantı defterinde yapılan her ekleme/düzenleme/silme işlemini kullanıcı ve zaman bilgisiyle denetim (audit) kaydına yazmalıdır. | Orta |
| FR-13 | Sistem, ASP.NET Core Health Checks ile `/healthz` ve `/readyz` endpoint'lerini sağlamalıdır. | Orta |
| FR-14 | Sistem, hiçbir koşulda izlediği PostgreSQL örneklerinde publication, subscription veya replication slot oluşturma, silme veya yeniden oluşturma (recreate) işlemi **gerçekleştirmemelidir**; bu, yazılım seviyesinde (kod içinde CDC nesnelerine yönelik DDL çalıştırma imkânı bulunmaması) garanti edilmelidir. | Yüksek |
| FR-15 | Sistem, kayıtlı her bağlantının şema kataloğunu (kullanıcı şemalarındaki tablo/görünüm, kolon, indeks ve kısıt bilgisi) yapılandırılabilir bir aralıkta **yalnızca SELECT ile** toplamalı ve kendi metadata veritabanında saklamalıdır; kullanıcı bu kataloğu şema/tablo/kolon adına göre arayarak görüntüleyebilmeli, katalogun tamamını tablo/kolon açıklamalarıyla birlikte filtrelenebilir bir rapor olarak listeleyebilmeli ve bu raporu Excel (.xlsx) dosyası olarak dışa aktarabilmeli, bir bağlantıyı talep üzerine yeniden taratabilmeli ve periyodik taramayı tamamen kapatabilmelidir. | Orta |
| FR-16 | Sistem, ardışık iki katalog taraması arasındaki farkları (tablo/kolon/indeks/kısıt eklenmesi-silinmesi, kolon tipi/NULL kabulü/varsayılan değeri değişimi) tespit edip zaman damgalı bir değişiklik günlüğünde saklamalı ve kullanıcıya listelemelidir. Bir bağlantının ilk taraması (tüm kataloğun "yeni" görüneceği durum) ve başarısız bir tarama değişiklik kaydı üretmemelidir. | Orta |

---

## 7. Fonksiyonel Olmayan Gereksinimler

| ID | Kategori | Gereksinim |
|---|---|---|
| NFR-01 | Ağ | Uygulamanın çalıştığı K8s cluster'ından, Kubernetes dışındaki tüm PostgreSQL sunucularına (farklı DC'ler dahil) ağ erişimi sağlanmalıdır (firewall/route). |
| NFR-02 | Güvenlik | Bağlantı defterindeki parolalar şifreli saklanmalı; her PostgreSQL örneğine izleme için **salt-okunur (read-only)** bir veritabanı rolü kullanılmalıdır — bu rolün yazma yetkisi olmamalıdır. |
| NFR-03 | Güvenlik | Takip ekranına erişim, kurumsal kimlik doğrulama (SSO) ile korunmalı; bağlantı defterini düzenleme yetkisi rol bazlı sınırlandırılmalıdır. |
| NFR-04 | Performans | Kayıtlı bağlantı sayısı arttıkça (ör. 50+ bağlantı) tarama döngüsü paralel çalışmalı ve toplam süre 30 sn'yi aşmamalıdır. |
| NFR-05 | Güvenilirlik | Uygulama tek hata noktası olmamalı; en az 2 replika ile çalışmalı, kendi durum verisini (bağlantı defteri) kalıcı bir veri deposunda (ör. kendi PostgreSQL şeması) tutmalıdır. |
| NFR-06 | Taşınabilirlik | Uygulama mevcut .NET/Kubernetes/Helm dağıtım pipeline'ı ile uyumlu olmalıdır (mevcut ~40 mikroservis standardı). |
| NFR-07 | Gözlemlenebilirlik | Uygulamanın kendi logları mevcut ELK stack'ine akmalıdır. |
| NFR-08 | Bildirim | Bildirim kanalı yalnızca e-posta (SMTP) olmalıdır; Slack veya başka bir sohbet entegrasyonu bu sürümde kullanılmayacaktır. |

---

## 8. Uyarı (Alert) Eşikleri ve Önceliklendirme

> Aşağıdaki eşikler başlangıç önerisidir; devreye alma sonrası gerçek trafik verisiyle kalibre edilmelidir. Tüm bildirimler **e-posta** ile yapılır; sistem hiçbir koşulda otomatik düzeltici aksiyon almaz.

| Öncelik | Koşul | Aksiyon |
|---|---|---|
| Kritik | Replication slot > 5 dk inaktif | E-posta ile anlık bildirim, DBA ekibine eskalasyon (manuel müdahale) |
| Kritik | `wal_status ≠ reserved` / `safe_wal_size` düşük | E-posta ile anlık bildirim, manuel müdahale eskalasyonu |
| Kritik | Subscription durumu disabled veya error | E-posta ile anlık bildirim, ilgili ekibe eskalasyon (manuel müdahale) |
| Uyarı | Subscription lag sürekli artıyor (eşik yapılandırılabilir), 10 dk sürekli | E-posta ile bildirim |
| Uyarı | Bir bağlantıya periyodik health check başarısız (2 ardışık deneme) | E-posta ile bildirim |
| Bilgi | Haftalık reconciliation'da tutarsızlık bulunması | E-posta ile rapor |

---

## 9. Varsayımlar

- İzlenecek her PostgreSQL örneğinde mantıksal replikasyon (`wal_level=logical`) zaten etkin ve gerekli publication/subscription tanımları önceden yapılmıştır.
- K8s cluster'ından, cluster dışındaki tüm PostgreSQL sunucularına ağ erişimi sağlanabilecektir (gerekirse firewall talebi açılacaktır).
- Her PostgreSQL örneğinde izleme için salt-okunur bir rol/kullanıcı oluşturulabilecektir.
- Uygulamanın kendi bağlantı defteri verisini saklayacağı bir veri deposu (ör. küçük bir PostgreSQL şeması) sağlanacaktır.
- Kurumsal SMTP sunucusuna uygulamadan erişim/gönderim izni verilecektir.
- Herhangi bir CDC bozulması (slot kaybı, subscription hatası) tespit edildiğinde düzeltici aksiyonun ilgili ekip tarafından manuel olarak, uygulama dışında yapılacağı kabul edilmektedir.

---

## 10. Kısıtlar

- Kafka, Strimzi veya Debezium bu proje kapsamında kullanılmayacaktır; tüm CDC PostgreSQL'in yerleşik mantıksal replikasyonu ile yürütülür.
- Uygulama on-premises Kubernetes cluster'ında, mevcut deployment standartlarına (Helm) uygun çalışmalıdır; izlenen PostgreSQL örnekleri ise cluster dışındadır.
- Yeni bir üçüncü parti gözlemlenebilirlik SaaS ürünü kapsam dışıdır; zaman serisi grafikleri için mevcut Prometheus/Grafana kullanılacaktır.
- Uygulama **salt gözlem** amaçlıdır; PostgreSQL üzerinde herhangi bir CDC nesnesi (publication/subscription/replication slot) oluşturma, silme veya yeniden oluşturma işlemi yapmaz.
- Bildirim kanalı yalnızca e-postadır; Slack veya benzeri bir entegrasyon bu kapsamda yoktur.

---

## 11. Riskler

| Risk | Etki | Olasılık | Azaltım |
|---|---|---|---|
| Otomatik CDC ilişkisi çıkarımının yanlış eşleştirme yapması | Orta | Orta | Kullanıcı onayı zorunlu kılınır (FR-06); otomatik keşif öneri niteliğinde kalır, PostgreSQL nesnesini etkilemez |
| Bağlantı defterindeki parolaların sızması | Yüksek | Düşük | Şifreleme, Vault/Secret kullanımı, erişim kontrolü, audit log |
| K8s cluster'ından dış PostgreSQL sunucularına ağ erişiminin (firewall) sağlanamaması | Yüksek | Orta | Proje başında ağ ekibiyle erişim talepleri netleştirilir |
| Reconciliation job'unun büyük tablolarda performans/kilit sorunu yaratması | Orta | Orta | Örnekleme/checksum stratejisi, düşük öncelikli zamanlama, salt okuma |
| Kritik alarmın yalnızca e-posta ile iletilmesi nedeniyle gece/hafta sonu geç fark edilmesi (Slack/anlık mesajlaşma gibi bir kanal olmadığından) | Orta | Orta | E-posta dağıtım listesine nöbetçi (on-call) grubunun dahil edilmesi, gerekirse ileride ek kanal değerlendirilmesi |

---

## 12. Teslimat ve Sonraki Adımlar

- **Faz 1:** Bağlantı Kayıt Defteri (CRUD, şifreli saklama, salt-okunur health check) ve temel Prometheus metrikleri.
- **Faz 2:** Otomatik CDC ilişki keşfi (salt okuma), onay mekanizması ve topoloji/takip ekranı.
- **Faz 3:** E-posta (SMTP) bildirim entegrasyonu, kritik alarm kuralları ve haftalık reconciliation job'u.

Bu BRD onaylandıktan sonra mimari/teknik tasarım dokümanı (veri modeli, takip ekranı wireframe'leri, Helm values, SMTP yapılandırması) ayrı bir doküman olarak hazırlanacaktır.

---

## 13. Onay

| Ad Soyad | Rol | Onay Tarihi | İmza |
|---|---|---|---|
| Yakup Kalebaşı | Yazılım Çözümleri Mimarı | | |
| | | | |
