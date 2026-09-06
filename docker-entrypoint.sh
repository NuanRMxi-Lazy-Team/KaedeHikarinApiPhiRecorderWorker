#!/bin/sh
set -e

Xvfb :99 -screen 0 1280x720x24 -nolisten tcp >/dev/null 2>&1 &
XVFB_PID=$!
trap 'kill $XVFB_PID 2>/dev/null || true' EXIT

export DISPLAY=:99
sleep 2

exec dotnet KaedeHikarinCialloTeam.PhiRecorder.Worker.dll
