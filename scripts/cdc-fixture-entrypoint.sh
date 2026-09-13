#!/bin/sh
# docker-compose.local.yml içindeki cdc-fixture servisinin entrypoint'i.
# `docker compose exec` yerine compose network'ü üzerinden doğrudan pub-db/sub-db'ye
# bağlanır, böylece `docker compose up` ile birlikte otomatik çalışır. Her adım önce
# var olup olmadığını kontrol eder, bu yüzden tekrar tekrar çalıştırmak log'da
# gürültülü ERROR satırları üretmez.
#
# İki senaryo kurar:
#
# 1) Basit senaryo (pub-db/orders, kaynak):
#      pub_orders    -> orders_replica, analytics_replica   (fan-out)
#      pub_customers -> orders_replica, audit_replica       (fan-out)
#      pub_products  -> orders_replica
#      pub_invoices  -> analytics_replica
#      pub_payments  -> audit_replica
#
# 2) Zengin senaryo (kurgusal bir "kütüphane/katalog" domain akışı — gerçek bir
#      şirketin domain/servis isimleriyle örtüşmesin diye bilinçli olarak jenerik):
#      dom-catalog-api (pub-db/catalog) 8 publication yayınlar; dom-lending-api
#      (mid-db/lending) bunlardan bazılarına HEM abone olur HEM de kendi
#      publication'ını (dom_lending_loan_event_pub) aşağı akışa yayınlar — yani
#      hem hedef hem kaynak. 5 hedef servis (notification, search-index, billing,
#      analytics, recommendation) bu hub'lara farklı kombinasyonlarla abone olur,
#      çoğu hub 2-3 hedefe dallanır (fan-out).
#
#      dom-lending-api NEDEN AYRI BİR INSTANCE (mid-db)?: "hem hedef hem kaynak" bir
#      düğüm, hem yukarı akıştan (catalog) hem de aşağı akışa (notification/billing/
#      analytics) bağlanır. Bunlardan biri AYNI Postgres sunucusuna geri bağlanan bir
#      CREATE SUBSCRIPTION olsaydı, komut kendi açtığı walsender'ın bitmesini beklerken
#      walsender de komutun tuttuğu transactionid kilidini bekler — süresiz deadlock
#      (denenip doğrulandı). pub-db/sub-db'den farklı üçüncü bir instance kullanmak
#      bunu yapısal olarak imkansız kılar: hiçbir subscription kendi sunucusuna dönmez.
set -eu

PUB_HOST=pub-db
PUB_DB=orders
SUB_HOST=sub-db
MID_HOST=mid-db

echo "pub-db, sub-db ve mid-db'nin hazır olması bekleniyor..."
until PGPASSWORD=postgres pg_isready -h "$PUB_HOST" -U postgres >/dev/null 2>&1; do sleep 1; done
until PGPASSWORD=postgres pg_isready -h "$SUB_HOST" -U postgres >/dev/null 2>&1; do sleep 1; done
until PGPASSWORD=postgres pg_isready -h "$MID_HOST" -U postgres >/dev/null 2>&1; do sleep 1; done

# --- Genel yardımcılar (host/db parametreli) ---------------------------------

psql_c() { PGPASSWORD=postgres psql -h "$1" -U postgres -d "$2" -v ON_ERROR_STOP=1 -c "$3"; }
psql_tuple() { PGPASSWORD=postgres psql -h "$1" -U postgres -d "$2" -tAc "$3"; }

# --- Tekrar çalıştırmayı tamamen atlama işareti -------------------------------
# Alttaki her adım zaten idempotent (var olup olmadığını kontrol eder), ama bu
# container'ların kalıcı volume'ü olmadığından `docker compose up --build`
# her çağrıldığında cdc-fixture yeniden oluşabiliyor ve script'in tamamı baştan
# çalışır — onlarca "zaten mevcut, atlanıyor" kontrolü için gereksiz yere pub-db/
# sub-db/mid-db'ye tekrar tekrar bağlanır. pub-db/orders kalıcıysa (yalnızca
# container yeniden oluştu, veri silinmediyse) bu işaret script'in tamamını
# saniyeler içinde atlamayı sağlar.
psql_c "$PUB_HOST" "$PUB_DB" "CREATE TABLE IF NOT EXISTS public._cdc_fixture_marker (id int PRIMARY KEY, completed_at timestamptz NOT NULL);"
already_done=$(psql_tuple "$PUB_HOST" "$PUB_DB" "SELECT 1 FROM public._cdc_fixture_marker WHERE id = 1;")
if [ "$already_done" = "1" ]; then
  echo "cdc-fixture daha önce tamamlanmış (pub-db/orders kalıcı), kurulum atlanıyor."
  exit 0
fi

ensure_database() {
  # $1=host $2=db
  exists=$(psql_tuple "$1" postgres "SELECT 1 FROM pg_database WHERE datname = '$2';")
  if [ "$exists" != "1" ]; then
    psql_c "$1" postgres "CREATE DATABASE $2;"
    echo "$1/$2 veritabanı oluşturuldu."
  fi
}

ensure_publication() {
  # $1=host $2=db $3=table $4=pubname
  psql_c "$1" "$2" "CREATE TABLE IF NOT EXISTS $3(id serial primary key, name text);"
  exists=$(psql_tuple "$1" "$2" "SELECT 1 FROM pg_publication WHERE pubname = '$4';")
  if [ "$exists" = "1" ]; then
    echo "$4 zaten mevcut, atlanıyor."
  else
    psql_c "$1" "$2" "CREATE PUBLICATION $4 FOR TABLE $3;"
    echo "$4 oluşturuldu."
  fi
}

ensure_subscription() {
  # $1=sub_host $2=sub_db $3=sub_name $4=pub_host $5=pub_db $6=pub_name $7=table
  psql_c "$1" "$2" "CREATE TABLE IF NOT EXISTS $7(id serial primary key, name text);"
  exists=$(psql_tuple "$1" "$2" "SELECT 1 FROM pg_subscription WHERE subname = '$3';")
  if [ "$exists" = "1" ]; then
    echo "$3 zaten mevcut, atlanıyor."
  else
    psql_c "$1" "$2" "CREATE SUBSCRIPTION $3 CONNECTION 'host=$4 port=5432 dbname=$5 user=postgres password=postgres' PUBLICATION $6;"
    echo "$3 oluşturuldu."
  fi
}

# ==============================================================================
# Senaryo 1: basit (orders/customers/products/invoices/payments)
# ==============================================================================

# Önceki (tek tablo/tek hedef) sürümden kalan sabit isimli "sub_orders" varsa,
# resync gerektirmeden yeni adlandırma şemasına taşı.
legacy_exists=$(psql_tuple "$SUB_HOST" orders_replica "SELECT 1 FROM pg_subscription WHERE subname = 'sub_orders';")
if [ "$legacy_exists" = "1" ]; then
  psql_c "$SUB_HOST" orders_replica "ALTER SUBSCRIPTION sub_orders RENAME TO sub_orders_replica_orders;"
  echo "sub_orders -> sub_orders_replica_orders olarak taşındı (eski isimlendirme)."
fi

for table in orders customers products invoices payments; do
  ensure_publication "$PUB_HOST" "$PUB_DB" "$table" "pub_$table"
done

ensure_database "$SUB_HOST" orders_replica
ensure_database "$SUB_HOST" analytics_replica
ensure_database "$SUB_HOST" audit_replica

# orders_replica: orders + customers (fan-out'un ilk ayağı) + products
ensure_subscription "$SUB_HOST" orders_replica sub_orders_replica_orders "$PUB_HOST" "$PUB_DB" pub_orders orders
ensure_subscription "$SUB_HOST" orders_replica sub_orders_replica_customers "$PUB_HOST" "$PUB_DB" pub_customers customers
ensure_subscription "$SUB_HOST" orders_replica sub_orders_replica_products "$PUB_HOST" "$PUB_DB" pub_products products

# analytics_replica: orders (fan-out) + invoices
ensure_subscription "$SUB_HOST" analytics_replica sub_analytics_replica_orders "$PUB_HOST" "$PUB_DB" pub_orders orders
ensure_subscription "$SUB_HOST" analytics_replica sub_analytics_replica_invoices "$PUB_HOST" "$PUB_DB" pub_invoices invoices

# audit_replica: customers (fan-out) + payments
ensure_subscription "$SUB_HOST" audit_replica sub_audit_replica_customers "$PUB_HOST" "$PUB_DB" pub_customers customers
ensure_subscription "$SUB_HOST" audit_replica sub_audit_replica_payments "$PUB_HOST" "$PUB_DB" pub_payments payments

echo "Senaryo 1 hazır: 5 tablo/publication (orders, customers, products, invoices, payments)."

# ==============================================================================
# Senaryo 2: zengin domain akışı (kurgusal kütüphane/katalog sektörü)
# ==============================================================================

CATALOG_DB=catalog

ensure_database "$PUB_HOST" "$CATALOG_DB"

# --- dom-catalog-api (pub-db/catalog): 8 publication ---
ensure_publication "$PUB_HOST" "$CATALOG_DB" book dom_catalog_book_pub
ensure_publication "$PUB_HOST" "$CATALOG_DB" author dom_catalog_author_pub
ensure_publication "$PUB_HOST" "$CATALOG_DB" imprint dom_catalog_imprint_pub
ensure_publication "$PUB_HOST" "$CATALOG_DB" category dom_catalog_category_pub
ensure_publication "$PUB_HOST" "$CATALOG_DB" edition dom_catalog_edition_pub
ensure_publication "$PUB_HOST" "$CATALOG_DB" isbn_record dom_catalog_isbn_record_pub
ensure_publication "$PUB_HOST" "$CATALOG_DB" shelf_location dom_catalog_shelf_location_pub
ensure_publication "$PUB_HOST" "$CATALOG_DB" review dom_catalog_review_pub

# --- Hedef servislerin veritabanları (sub-db üzerinde) ---
ensure_database "$SUB_HOST" notification
ensure_database "$SUB_HOST" search_index
ensure_database "$SUB_HOST" billing
ensure_database "$SUB_HOST" analytics
ensure_database "$SUB_HOST" recommendation

# --- dom-lending-api (mid-db/lending, ayrı bir Postgres instance'ı): HEM hedef HEM kaynak ---
# "lending" veritabanı mid-db'nin varsayılan (POSTGRES_DB) veritabanı olduğu için ayrıca
# oluşturulması gerekmiyor. Kaynak tarafı: kendi publication'ını aşağı akışa yayınlar.
ensure_publication "$MID_HOST" lending loan_event dom_lending_loan_event_pub

# --- Diğer katalog hub fan-out'ları ---
ensure_subscription "$SUB_HOST" search_index sub_search_index_book "$PUB_HOST" "$CATALOG_DB" dom_catalog_book_pub book
ensure_subscription "$SUB_HOST" search_index sub_search_index_author "$PUB_HOST" "$CATALOG_DB" dom_catalog_author_pub author
ensure_subscription "$SUB_HOST" search_index sub_search_index_imprint "$PUB_HOST" "$CATALOG_DB" dom_catalog_imprint_pub imprint
ensure_subscription "$SUB_HOST" search_index sub_search_index_category "$PUB_HOST" "$CATALOG_DB" dom_catalog_category_pub category
ensure_subscription "$SUB_HOST" search_index sub_search_index_edition "$PUB_HOST" "$CATALOG_DB" dom_catalog_edition_pub edition

ensure_subscription "$SUB_HOST" recommendation sub_recommendation_book "$PUB_HOST" "$CATALOG_DB" dom_catalog_book_pub book
ensure_subscription "$SUB_HOST" recommendation sub_recommendation_author "$PUB_HOST" "$CATALOG_DB" dom_catalog_author_pub author
ensure_subscription "$SUB_HOST" recommendation sub_recommendation_category "$PUB_HOST" "$CATALOG_DB" dom_catalog_category_pub category
ensure_subscription "$SUB_HOST" recommendation sub_recommendation_review "$PUB_HOST" "$CATALOG_DB" dom_catalog_review_pub review

ensure_subscription "$SUB_HOST" notification sub_notification_book "$PUB_HOST" "$CATALOG_DB" dom_catalog_book_pub book
ensure_subscription "$SUB_HOST" notification sub_notification_review "$PUB_HOST" "$CATALOG_DB" dom_catalog_review_pub review

ensure_subscription "$SUB_HOST" billing sub_billing_imprint "$PUB_HOST" "$CATALOG_DB" dom_catalog_imprint_pub imprint
ensure_subscription "$SUB_HOST" billing sub_billing_isbn_record "$PUB_HOST" "$CATALOG_DB" dom_catalog_isbn_record_pub isbn_record

# --- dom-lending-api'nin kendi publication'ından aşağı akış ---
ensure_subscription "$SUB_HOST" notification sub_notification_loan_event "$MID_HOST" lending dom_lending_loan_event_pub loan_event
ensure_subscription "$SUB_HOST" billing sub_billing_loan_event "$MID_HOST" lending dom_lending_loan_event_pub loan_event
ensure_subscription "$SUB_HOST" analytics sub_analytics_loan_event "$MID_HOST" lending dom_lending_loan_event_pub loan_event

# --- dom-lending-api'nin KENDİ katalog abonelikleri (hedef tarafı) ---
ensure_subscription "$MID_HOST" lending sub_lending_edition "$PUB_HOST" "$CATALOG_DB" dom_catalog_edition_pub edition
ensure_subscription "$MID_HOST" lending sub_lending_isbn_record "$PUB_HOST" "$CATALOG_DB" dom_catalog_isbn_record_pub isbn_record
ensure_subscription "$MID_HOST" lending sub_lending_shelf_location "$PUB_HOST" "$CATALOG_DB" dom_catalog_shelf_location_pub shelf_location

echo "Senaryo 2 hazır: dom-catalog-api (8 publication) + dom-lending-api (hem hedef hem kaynak, 1 publication)."
echo "Hedefler: dom-lending-api, dom-notification-api, dom-search-index-api, dom-billing-api, dom-analytics-api, dom-recommendation-api."

psql_c "$PUB_HOST" "$PUB_DB" "INSERT INTO public._cdc_fixture_marker (id, completed_at) VALUES (1, now()) ON CONFLICT (id) DO UPDATE SET completed_at = now();"
echo "cdc-fixture kurulumu tamamlandı ve işaretlendi — bir sonraki 'up' bu adımların tamamını atlayacak."
