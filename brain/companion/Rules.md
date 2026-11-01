# Kullanıcının düzeltmeleri

Her işte geçerli kurallar burada, tam metinle durur. Tek bir projeye ya da alana ait kural
`projects/<project>/Rules.md` dosyasına gider; burada yalnız `[scope: <project>]` etiketli
tek satırlık işaretçi kalır.

- [scope: cdc-monitoring] cdc-monitoring işinde önce projects/cdc-monitoring/Rules.md dosyasını oku.

Kayıt biçimi:

```
- 2026-11-01: Kuralın kendisi, bir iki satır.
- [scope: project-name] Bu proje işinde önce projects/project-name/Rules.md dosyasını oku.
```

Rules:

- Kullanıcı açıkça düzelttiğinde kuralı beklemeden kaydet.
- Bir iki satır yaz. Uzun gerekçe ve olayın anlatımı `Rules-Detail.md` dosyasına gider;
  kuraldan ona bağlantı bırak.
- Tek seferlik biçim isteğini kalıcı kurala çevirme.
- Çelişen eski kuralı silmeden, açıklayarak düzelt.
