#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
backend_root="$(cd -- "$script_dir/.." && pwd)"

cd "$backend_root"

dotnet restore ZMovie.slnx
dotnet build ZMovie.slnx --no-restore --nologo
dotnet test ZMovie.slnx --no-restore --no-build --nologo "$@"
