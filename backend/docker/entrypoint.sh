#!/usr/bin/env sh
set -e

if [ -f .env.docker ]; then
    cp .env.docker .env
elif [ ! -f .env ]; then
    cp .env.example .env
fi

if [ ! -d vendor ] || [ ! -f vendor/autoload.php ]; then
    composer install --no-interaction --prefer-dist
fi

if ! grep -q '^APP_KEY=base64:' .env; then
    php artisan key:generate --force
fi

php artisan config:clear

if [ "${DB_CONNECTION}" = "pgsql" ]; then
    until php -r "new PDO('pgsql:host=${DB_HOST};port=${DB_PORT};dbname=${DB_DATABASE}', '${DB_USERNAME}', '${DB_PASSWORD}');" >/dev/null 2>&1; do
        echo "Waiting for database..."
        sleep 2
    done
fi

php artisan migrate --force

exec "$@"
