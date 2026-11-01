---
# Kişi notu şablonu. Yeni notu notes/people/<full-name>.md olarak aç ve aşağıdaki satırın
# başındaki "# " işaretini kaldır. Şablonun kendisi kişi sayılmasın diye kapalı duruyor.
# type: person
date: YYYY-MM-DD
updated: YYYY-MM-DD
status: accepted
source: user
origin: user
title:
aliases: []
facts:
  org:
  role:
  relationship:
  last_contact:
visibility: internal
tags: [person]
links: []
---

# Ad Soyad

## Stated

Kullanıcının doğrudan söyledikleri, tarihli ve kaynağıyla.

- 2026-11-01 (kullanıcı): ...

## Inferred (awaiting confirmation)

Ajanın tahmini. Kullanıcı onaylayana kadar olgu sayılmaz. Onaylanırsa Beyan'a taşınır,
reddedilirse silinmez, "reddedildi" olarak tarihiyle işaretlenir.

## Contacts

Tarih, kanal, bir satır özet.

## Sources

Bilginin geldiği not, sayfa ya da açık kaynak bağlantısı.

<!--
Rules:

- title tam ad, aliases kullanıcının kullandığı kısa adlar ve kullanıcı adları. İkisi de
  aramada eşleşir.
- Aynı kişi için ikinci not açma. Yazım farklıysa alias ekle.
- Kullanıcının söylediği Beyan'a, senin tahminin Çıkarım'a gider. İkisini karıştırma.
- Açık kaynaktan eklenen bilgi kaynağıyla birlikte Kaynaklar'a girer. Kişinin kamusal
  mesleki faaliyetinin ötesine geçme.
- Sağlık, mahrem hayat, siyasi ya da dini görüş, finansal durum kişi notuna yazılmaz.
  Kullanıcı açıkça isterse notu visibility: private yap.
- Kullanıcı bir kişi notunun silinmesini isterse sil.
- Kaynakta geçen ama kullanıcının hayatında olmayan kişi wiki/entities/ altına gider.
- Kişiyle ilgili iş tasks/ altında görev olur; kişi notunda yalnız bağlantısı durur.
-->
