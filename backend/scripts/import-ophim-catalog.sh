#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_root/backend"

# Defaults to every public catalog page. Use --start-page N to resume after a
# failed page. Add --with-episodes only for a slower episode/HLS pass.
DOTNET_ENVIRONMENT=Development dotnet run --project src/ZMovie.Api --no-launch-profile -- --import-ophim-catalog --all "$@"
