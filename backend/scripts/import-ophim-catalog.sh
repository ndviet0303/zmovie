#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_root/backend"

# Defaults to every public catalog page. Add --with-episodes only when you want
# a slower second pass that fetches each movie's episode/HLS data.
DOTNET_ENVIRONMENT=Development dotnet run --project src/ZMovie.Api --no-launch-profile -- --import-ophim-catalog --all "$@"
