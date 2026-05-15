FROM registry.gitlab.com/bantool/devops/frankenphp:latest AS runner
COPY --from=registry.gitlab.com/bantool/ziet-projects/zmovie/api:latest /app /app/zmovie-api

COPY configs/zmovie.prod.Caddyfile /etc/frankenphp/Caddyfile
COPY configs/php.ini-production "$PHP_INI_DIR/php.ini"
