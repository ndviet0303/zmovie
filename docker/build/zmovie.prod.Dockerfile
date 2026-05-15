ARG API_IMAGE=registry.gitlab.com/bantool/ziet-projects/zmovie/api
ARG API_TAG=latest

FROM ${API_IMAGE}:${API_TAG} AS api

FROM registry.gitlab.com/bantool/devops/frankenphp:latest AS runner
COPY --from=api /app /app/zmovie-api

COPY configs/zmovie.prod.Caddyfile /etc/frankenphp/Caddyfile
COPY configs/php.ini-production "$PHP_INI_DIR/php.ini"
