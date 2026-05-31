FROM registry.gitlab.com/bantool/devops/frankenphp:latest AS runner

COPY backend/ ./
# COPY docker/env/zmovie-api.prod.env .env

RUN composer dump-autoload --optimize \
    && php artisan package:discover --ansi \
    && chown -R www-data:www-data storage bootstrap/cache

COPY docker/configs/zmovie.prod.Caddyfile /etc/frankenphp/Caddyfile
COPY docker/configs/php.ini-production "$PHP_INI_DIR/php.ini"

EXPOSE 8000
