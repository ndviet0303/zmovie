# Docker

## Run backend

```bash
cd backend
docker compose up --build
```

Laravel backend se chay tai:

```text
http://127.0.0.1:8000
```

PostgreSQL se chay tai `127.0.0.1:5432` voi thong tin:

```text
database: zmovie
username: zmovie
password: zmovie_secret
```

## Useful commands

```bash
docker compose exec app php artisan migrate:fresh
docker compose exec app php artisan migrate:fresh --seed
docker compose exec app php artisan scout:sync-index-settings
docker compose exec app php artisan scout:import "App\\Models\\Movie"
docker compose exec app php artisan test
docker compose exec app php artisan tinker
docker compose down
docker compose down -v
```

`docker compose down -v` se xoa volume PostgreSQL va mat data local.

Meilisearch chay tai `http://127.0.0.1:7700`, key dev la `zmovie_dev_master_key`.
