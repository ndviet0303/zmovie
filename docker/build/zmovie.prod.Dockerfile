FROM composer:2 AS vendor

WORKDIR /app

COPY backend/composer.json backend/composer.lock ./
RUN composer install \
    --no-dev \
    --no-interaction \
    --no-progress \
    --no-scripts \
    --prefer-dist \
    --optimize-autoloader

FROM registry.gitlab.com/bantool/devops/frankenphp:latest AS runner

WORKDIR /app/zmovie-api

COPY backend/ ./
COPY --from=vendor /app/vendor ./vendor
# COPY docker/env/zmovie-api.prod.env .env

RUN composer dump-autoload --optimize \
    && php artisan package:discover --ansi \
    && chown -R www-data:www-data storage bootstrap/cache

COPY docker/configs/zmovie.prod.Caddyfile /etc/frankenphp/Caddyfile
COPY docker/configs/php.ini-production "$PHP_INI_DIR/php.ini"

EXPOSE 8000
