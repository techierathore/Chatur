#!/usr/bin/env bash
# Seed for Integrations (docs/mockups/integrations.html), run by tf-verify-screens.sh and tf-mockup-parity.sh
# before they open /integrations, with TF_BASE set to the running app's address (verify of 21 Nov 2026).
#
# The mockup draws three window checks by the verifier and the Mac and Windows downloads of a release with who
# signed them. A fresh harness database has none, so this seed writes those records into the signed-in test
# account's database, as the verifier and the release step would, dated days back so a test's own newer record stays first. Idempotent. Only ever writes under tests/.artifacts/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
POINTER="$(dirname "$DB")/accounts/current.txt"
if [ -s "$POINTER" ]; then DB="$(dirname "$DB")/accounts/$(tr -d '\r\n' < "$POINTER")/chatur.db"; fi
[[ -f "$DB" ]] || { echo "no database for the app at $BASE (looked for $DB)"; exit 1; }
case "$DB" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $DB: the seed only writes under tests/.artifacts/"; exit 1 ;;
esac

python3 - "$DB" <<'PY'
import datetime, sqlite3, sys
c = sqlite3.connect(sys.argv[1], timeout=20)
now = datetime.datetime.now(datetime.timezone.utc)
iso = lambda d: d.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
c.execute("DELETE FROM VerifierWindowCheck WHERE Machine LIKE 'seed-%'")
for i, (app, platform, machine, shots, result) in enumerate([
        ("TfLens", "Windows", "seed-win-desk", 14, "passed"),
        ("MyDiary", "Mac", "seed-macbook", 9, "passed"),
        ("Xpenser", "Windows", "seed-win-desk", 11, "2 screens failed")]):
    c.execute("INSERT INTO VerifierWindowCheck (Application, Platform, Machine, ScreenshotsKept, CheckedUtc, Result) VALUES (?, ?, ?, ?, ?, ?)",
              (app, platform, machine, shots, iso(now - datetime.timedelta(days=3 * (i + 1))), result))
c.execute("DELETE FROM ReleaseDownload WHERE Version = '0.9.0-seed'")
c.execute("INSERT INTO ReleaseDownload (Version, Platform, FileName, SignedBy, Notarisation, PublishedUtc) VALUES (?, ?, ?, ?, ?, ?)",
          ("0.9.0-seed", "Mac", "Chatur-0.9.0-mac.zip", "Developer ID Application: Test Signer", "Notarised", iso(now - datetime.timedelta(days=1))))
c.execute("INSERT INTO ReleaseDownload (Version, Platform, FileName, SignedBy, Notarisation, PublishedUtc) VALUES (?, ?, ?, ?, ?, ?)",
          ("0.9.0-seed", "Windows", "Chatur-0.9.0-win.zip", "Test Signer (code signing)", "NotApplicable", iso(now - datetime.timedelta(days=1))))
c.commit()
print("integrations: three window checks and a signed Mac and Windows download")
PY
