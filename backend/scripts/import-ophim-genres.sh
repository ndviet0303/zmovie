#!/usr/bin/env bash

set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_root/backend"
DOTNET_ENVIRONMENT=Development dotnet run --project src/ZMovie.Api --no-launch-profile -- --import-ophim-genres "$@"
