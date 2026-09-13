#!/bin/sh
# docker-compose.local.yml içindeki cdc-fixture servisinin entrypoint'i.
# pub-db üzerinde 5 tablo/publication, sub-db üzerinde 3 hedef veritabanı ve
# bunlar arasında gerçek logical replication subscription'ları kurar — 2 tablo
# (orders, customers) birden fazla hedefe abone edilerek "bir publication'a
# birden fazla subscription bağlanabilir" (fan-out) senaryosunu gösterir:
#
#   pub-db/orders (kaynak)
#     ├─ pub_orders    -> sub-db/orders_replica, sub-db/analytics_replica   (fan-out)
#     ├─ pub_customers -> sub-db/orders_replica, sub-db/audit_replica       (fan-out)
#     ├─ pub_products  -> sub-db/orders_replica
#     ├─ pub_invoices  -> sub-db/analytics_replica
#     └─ pub_payments  -> sub-db/audit_replica
#
# `docker compose exec` yerine compose network'ü üzerinden doğrudan pub-db/sub-db'ye
# bağlanır, böylece `docker compose up` ile birlikte otomatik çalışır. Her adım önce
# var olup olmadığını kontrol eder, bu yüzden tekrar tekrar çalıştırmak log'da
# gürültülü ERROR satırları üretmez.
set -eu

PUB_HOST=pub-db
PUB_DB=orders
SUB_HOST=sub-db

echo "pub-db ve sub-db'nin hazır olması bekleniyor..."
until PGPASSWORD=postgres pg_isready -h "$PUB_HOST" -U postgres >/dev/null 2>&1; do sleep 1; done
until PGPASSWORD=postgres pg_isready -h "$SUB_HOST" -U postgres >/dev/null 2>&1; do sleep 1; done

pub_psql() { PGPASSWORD=postgres psql -h "$PUB_HOST" -U postgres -d "$PUB_DB" -v ON_ERROR_STOP=1 -c "$1"; }
pub_psql_tuple() { PGPASSWORD=postgres psql -h "$PUB_HOST" -U postgres -d "$PUB_DB" -tAc "$1"; }
sub_psql() { PGPASSWORD=postgres psql -h "$SUB_HOST" -U postgres -d "$1" -v ON_ERROR_STOP=1 -c "$2"; }
sub_psql_tuple() { PGPASSWORD=postgres psql -h "$SUB_HOST" -U postgres -d "$1" -tAc "$2"; }

ensure_database() {
  db="$1"
  exists=$(sub_psql_tuple postgres "SELECT 1 FROM pg_database WHERE datname = '$db';")
  if [ "$exists" != "1" ]; then
    sub_psql postgres "CREATE DATABASE $db;"
    echo "$db veritabanı oluşturuldu."
  fi
}

ensure_publication() {
  table="$1"
  pub="pub_$table"
  pub_psql "CREATE TABLE IF NOT EXISTS $table(id serial primary key, name text);"

  exists=$(pub_psql_tuple "SELECT 1 FROM pg_publication WHERE pubname = '$pub';")
  if [ "$exists" = "1" ]; then
    echo "$pub zaten mevcut, atlanıyor."
  else
    pub_psql "CREATE PUBLICATION $pub FOR TABLE $table;"
    echo "$pub oluşturuldu."
  fi
}

ensure_subscription() {
  db="$1"; table="$2"
  sub="sub_${db}_${table}"
  pub="pub_${table}"

  sub_psql "$db" "CREATE TABLE IF NOT EXISTS $table(id serial primary key, name text);"

  exists=$(sub_psql_tuple "$db" "SELECT 1 FROM pg_subscription WHERE subname = '$sub';")
  if [ "$exists" = "1" ]; then
    echo "$sub zaten mevcut, atlanıyor."
  else
    sub_psql "$db" "CREATE SUBSCRIPTION $sub CONNECTION 'host=$PUB_HOST port=5432 dbname=$PUB_DB user=postgres password=postgres' PUBLICATION $pub;"
    echo "$sub oluşturuldu."
  fi
}

# Önceki (tek tablo/tek hedef) sürümden kalan sabit isimli "sub_orders" varsa,
# resync gerektirmeden yeni adlandırma şemasına taşı.
legacy_exists=$(sub_psql_tuple orders_replica "SELECT 1 FROM pg_subscription WHERE subname = 'sub_orders';")
if [ "$legacy_exists" = "1" ]; then
  sub_psql orders_replica "ALTER SUBSCRIPTION sub_orders RENAME TO sub_orders_replica_orders;"
  echo "sub_orders -> sub_orders_replica_orders olarak taşındı (eski isimlendirme)."
fi

# --- Kaynak: pub-db/orders veritabanında 5 tablo + publication ---
for table in orders customers products invoices payments; do
  ensure_publication "$table"
done

# --- Hedefler: sub-db üzerinde 3 abone veritabanı ---
ensure_database orders_replica
ensure_database analytics_replica
ensure_database audit_replica

# orders_replica: orders + customers (fan-out'un ilk ayağı) + products
ensure_subscription orders_replica orders
ensure_subscription orders_replica customers
ensure_subscription orders_replica products

# analytics_replica: orders (fan-out) + invoices
ensure_subscription analytics_replica orders
ensure_subscription analytics_replica invoices

# audit_replica: customers (fan-out) + payments
ensure_subscription audit_replica customers
ensure_subscription audit_replica payments

echo "CDC test fixture hazır: 5 tablo/publication (orders, customers, products, invoices, payments)."
echo "Fan-out: orders -> orders_replica + analytics_replica; customers -> orders_replica + audit_replica."
