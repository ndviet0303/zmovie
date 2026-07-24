#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_root/backend"

# Defaults to every public catalog page. Use --start-page N to resume after a
# failed page. Add --with-episodes to import detail/episode data too.
# Detail requests use 3 concurrent workers by default; tune with:
#   OPHIM_CONCURRENCY=2 bash backend/scripts/import-ophim-catalog.sh --with-episodes
has_concurrency=false
for arg in "$@"; do
  if [[ "$arg" == "--concurrency" ]]; then
    has_concurrency=true
    break
  fi
done

extra_args=()
if [[ "$has_concurrency" == false ]]; then
  extra_args+=(--concurrency "${OPHIM_CONCURRENCY:-3}")
fi

DOTNET_ENVIRONMENT=Development dotnet run --project src/ZMovie.Api --no-launch-profile -- \
  --import-ophim-catalog --all "${extra_args[@]}" "$@"
