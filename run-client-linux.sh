#!/usr/bin/env bash
# Launch the Linux (SDL3 + OpenGL) Zircon client.
#
# Prerequisites:
#   - Client built:  dotnet build Client/Client.csproj -f net8.0 -c Release
#   - SDL3 + SDL3_ttf available (system packages, or a local build prefix).
#   - Game data present in the client output dir (Data/, Sound/, Map/ + Data/System.db).
#
# Override the SDL3 location with SDL3_PREFIX if you built it locally, e.g.:
#   SDL3_PREFIX=$HOME/work/sdl3-build/prefix ./run-client-linux.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLIENT_DIR="${CLIENT_DIR:-$SCRIPT_DIR/../Debug/Client}"
SDL3_PREFIX="${SDL3_PREFIX:-$SCRIPT_DIR/../sdl3-build/prefix}"

if [ -d "$SDL3_PREFIX/lib64" ]; then
    export LD_LIBRARY_PATH="$SDL3_PREFIX/lib64:${LD_LIBRARY_PATH:-}"
elif [ -d "$SDL3_PREFIX/lib" ]; then
    export LD_LIBRARY_PATH="$SDL3_PREFIX/lib:${LD_LIBRARY_PATH:-}"
fi

cd "$CLIENT_DIR"
echo "[run-client-linux] CLIENT_DIR=$CLIENT_DIR"
echo "[run-client-linux] LD_LIBRARY_PATH=${LD_LIBRARY_PATH:-<unset>}"
exec dotnet Zircon.dll "$@"
