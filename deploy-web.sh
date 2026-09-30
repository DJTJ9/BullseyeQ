#!/bin/bash
# Build BullseyeQ for the web and publish it to https://darts.thinkshark.de
# Close the Unity editor first (project lock).
set -euo pipefail

UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe}"
HOST="${DEPLOY_HOST:-root@195.201.121.96}"
WEBROOT=/var/www/bullseyeq
ROOT="$(cd "$(dirname "$0")" && pwd)"
OUT="$ROOT/Builds/Web"
LOG="$ROOT/Logs/web-build.log"

echo "[1/3] Unity web build ..."
rm -rf "$OUT"
set +e
"$UNITY" -batchmode -quit -projectPath "$(cd "$ROOT" && pwd -W)" -buildTarget WebGL \
  -executeMethod WebBuild.Build -logFile "$(cd "$ROOT" && pwd -W)/Logs/web-build.log"
code=$?
set -e
if [ "$code" -ne 0 ] || [ ! -f "$OUT/index.html" ]; then
  echo "Build failed (exit $code). Last 40 lines of $LOG:"
  tail -n 40 "$LOG" || true
  exit 1
fi

echo "[2/3] Upload to $HOST:$WEBROOT ..."
tar -C "$OUT" -cf - . | ssh "$HOST" "rm -rf $WEBROOT.new && mkdir -p $WEBROOT.new && tar -xf - -C $WEBROOT.new"
ssh "$HOST" "rm -rf $WEBROOT.old && { [ ! -d $WEBROOT ] || mv $WEBROOT $WEBROOT.old; } && mv $WEBROOT.new $WEBROOT && rm -rf $WEBROOT.old"

echo "[3/3] Live: https://darts.thinkshark.de"
