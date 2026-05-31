FROM registry.gitlab.com/bantool/devops/frankenphp:latest AS runner

RUN apk add --no-cache \
    ffmpeg \
    git \
    postgresql-dev \
    unzip \
    && docker-php-ext-install pdo pdo_pgsql

COPY --from=composer:2 /usr/bin/composer /usr/bin/composer

WORKDIR /app/zmovie-api

COPY backend/composer.json backend/composer.lock ./
RUN composer install \
    --no-dev \
    --no-interaction \
    --no-progress \
    --no-scripts \
    --prefer-dist \
    --optimize-autoloader

COPY backend/ ./
# COPY docker/env/zmovie-api.prod.env .env

RUN composer dump-autoload --optimize \
    && php artisan package:discover --ansi \
    && chown -R www-data:www-data storage bootstrap/cache

COPY docker/configs/zmovie.prod.Caddyfile /etc/frankenphp/Caddyfile
COPY docker/configs/php.ini-production "$PHP_INI_DIR/php.ini"

EXPOSE 8000
