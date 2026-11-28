#!/usr/bin/env bash
# The Mac screen walk (27 Nov 2026, the owner's first night on a Mac): every screen, light and dark, desktop and
# phone width, headless, against the web harness signed in through a LOCAL App Manager stand-in
# (support/appmanager-standin.py) — never the real App Manager, and with no test-user password needed.
#
#   bash tests/verify/support/mac-walk.sh
#
# Starts the stand-in on 9245 and the harness on 9244 (both must be free; 9243 is never used), with the harness's
# data in tests/.artifacts/mac-walk/harness-data, runs tests/verify/mac-screen-walk.spec.ts, and stops both by
# their own pid however the run ends. Screenshots and results.json land in tests/.artifacts/mac-walk/.
# Needs node_modules/@playwright/test in the repo (npm install --no-save @playwright/test@1.65.0) and a built
# harness (dotnet build src/Chatur.WebHarness ... with the Mac package feed; see the build lines below).
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
ART="$ROOT/tests/.artifacts/mac-walk"
STANDIN="$ART/standin"
HARNESS_PORT=9244
STANDIN_PORT=9245

for vPort in $HARNESS_PORT $STANDIN_PORT; do
  if lsof -nP -iTCP:$vPort -sTCP:LISTEN >/dev/null 2>&1; then
    echo "Port $vPort is in use; stop whatever holds it first." >&2
    exit 2
  fi
done

mkdir -p "$STANDIN" "$ART/harness-data"
: > "$STANDIN/requests.log"

# A throwaway RSA pair for the stand-in; the harness encrypts the (made-up) password against its public half.
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$STANDIN/private.pem" 2>/dev/null
openssl pkey -in "$STANDIN/private.pem" -pubout -out "$STANDIN/public.pem"

# The harness's own test-settings file, named by Chatur:TestSettingsPath: only the stand-in owner's address, so the
# harness seeds its fixture project into that account (Program.cs AttachAccountSeeding). No password, no provider.
cat > "$STANDIN/standin-settings.json" <<'JSON'
{ "testUsers": { "Owner": { "number": 1, "email": "mac-walk@example.invalid", "password": "", "onAppManager": false } } }
JSON

vStandinPid=""
vHarnessPid=""
cleanup() {
  [ -n "$vHarnessPid" ] && kill "$vHarnessPid" 2>/dev/null && wait "$vHarnessPid" 2>/dev/null
  [ -n "$vStandinPid" ] && kill "$vStandinPid" 2>/dev/null && wait "$vStandinPid" 2>/dev/null
}
trap cleanup EXIT INT TERM

python3 -I "$ROOT/tests/verify/support/appmanager-standin.py" $STANDIN_PORT "$STANDIN/public.pem" "$STANDIN/requests.log" &
vStandinPid=$!

export NUGET_PACKAGES="$ROOT/bin/mac-packages/cache"
dotnet build "$ROOT/src/Chatur.WebHarness/Chatur.WebHarness.csproj" --no-restore \
  -p:ChaturTechieRagVersion=1.3.0 -p:ChaturTrBlazeUIVersion=2.2.5 -v q -nologo || exit 3

cd "$ROOT/src/Chatur.WebHarness" || exit 4
ASPNETCORE_ENVIRONMENT=Development CHATUR_TEST_DATA="$ART/harness-data" \
  dotnet "$ROOT/src/Chatur.WebHarness/bin/Debug/net10.0/Chatur.WebHarness.dll" \
  --urls "http://localhost:$HARNESS_PORT" \
  --contentRoot "$ROOT/src/Chatur.WebHarness" \
  "--AppManager:BaseUrl=http://127.0.0.1:$STANDIN_PORT/" \
  "--Chatur:TestSettingsPath=$STANDIN/standin-settings.json" \
  > "$ART/harness.log" 2>&1 &
vHarnessPid=$!
cd "$ROOT" || exit 4

for _ in $(seq 1 120); do
  curl -fs "http://localhost:$HARNESS_PORT/healthz" >/dev/null 2>&1 && break
  sleep 1
done
curl -fs "http://localhost:$HARNESS_PORT/healthz" >/dev/null || { echo "The harness did not come up; see $ART/harness.log" >&2; exit 5; }

npx playwright test -c tests/verify/mac-walk.config.ts "$@"
