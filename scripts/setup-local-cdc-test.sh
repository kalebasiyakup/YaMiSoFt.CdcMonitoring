#!/usr/bin/env bash
# Local test fixture: pub-db/sub-db arasında gerçek bir logical replication
# publication/subscription kurar. Bu betik, prod'da DB ekibinin CDC kurulumu
# için yaptığı manuel işlemi simüle eder — uygulamanın kendisi bunu YAPMAZ (FR-14).
# Kullanım: docker-compose.local.yml servisleri ayaktayken çalıştırın.
set -euo pipefail

COMPOSE="docker compose -f docker-compose.local.yml"

echo "pub-db ve sub-db'nin hazır olması bekleniyor..."
until $COMPOSE exec -T pub-db pg_isready -U postgres >/dev/null 2>&1; do sleep 1; done
until $COMPOSE exec -T sub-db pg_isready -U postgres >/dev/null 2>&1; do sleep 1; done

$COMPOSE exec -T pub-db psql -U postgres -d orders -c \
  "CREATE TABLE IF NOT EXISTS orders(id serial primary key, name text);"
$COMPOSE exec -T pub-db psql -U postgres -d orders -c \
  "CREATE PUBLICATION pub_orders FOR TABLE orders;" || true

$COMPOSE exec -T sub-db psql -U postgres -d orders_replica -c \
  "CREATE TABLE IF NOT EXISTS orders(id serial primary key, name text);"
$COMPOSE exec -T sub-db psql -U postgres -d orders_replica -c \
  "CREATE SUBSCRIPTION sub_orders CONNECTION 'host=pub-db port=5432 dbname=orders user=postgres password=postgres' PUBLICATION pub_orders;" || true

echo "Kurulum tamam. Test için: $COMPOSE exec pub-db psql -U postgres -d orders -c \"INSERT INTO orders(name) VALUES ('test');\""
