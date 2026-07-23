#!/usr/bin/env bash

set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
api_pid=""
frontend_pid=""

stop_children() {
  local exit_code=$?
  trap - EXIT INT TERM

  for pid in "$api_pid" "$frontend_pid"; do
    if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null || true
    fi
  done

  wait "$api_pid" 2>/dev/null || true
  wait "$frontend_pid" 2>/dev/null || true
  exit "$exit_code"
}

trap stop_children EXIT INT TERM

cd "$project_root"

dotnet run --project backend/src/ZMovie.Api --launch-profile http &
api_pid=$!

(
  cd frontend
  npm run dev -- --host 0.0.0.0
) &
frontend_pid=$!

echo "ZMovie development servers are starting:"
echo "  Frontend: http://localhost:3000"
echo "  API:      http://localhost:5275"
echo "Press Ctrl+C to stop both servers."

while kill -0 "$api_pid" 2>/dev/null && kill -0 "$frontend_pid" 2>/dev/null; do
  sleep 1
done

if ! kill -0 "$api_pid" 2>/dev/null; then
  wait "$api_pid"
fi

wait "$frontend_pid"
