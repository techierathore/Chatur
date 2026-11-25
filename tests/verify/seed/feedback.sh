#!/usr/bin/env bash
# Seed for Feedback and issues (docs/mockups/feedback.html), run by tf-verify-screens.sh and tf-mockup-parity.sh
# before they open /feedback, with TF_BASE set to the running app's address (verify of 21 Nov 2026).
#
# The mockup draws six entries, numbered 114 down to 109: two waiting to send (so the "kept on this machine" note
# shows), one answered, two fixed and one raised. This seed makes exactly those rows in the signed-in test
# account's own database, and turns the harness's stand-in for App Manager "off the network" so the two
# waiting entries stay waiting while the screen is looked at. Idempotent. Only ever writes under tests/.artifacts/.
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

# the stand-in for App Manager is off the network, so waiting entries are not sent while the screen is graded
curl -s -o /dev/null -X POST "$BASE/test/feedback/network?down=true" || true

python3 - "$DB" <<'PY'
import datetime, sqlite3, sys
c = sqlite3.connect(sys.argv[1], timeout=20)
now = datetime.datetime.now(datetime.timezone.utc)
iso = lambda d: d.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
rows = [
    (114, "Chatur", "Major", "Queued runs came back in a different order", "Queued three runs, restarted Chatur, and they came back in a different order.", "Waiting", None, None, None),
    (113, "TrBlazeUI", "Minor", "A submenu runs off a narrow window", "At 390 px File, Recent projects ran past the window's edge.", "Waiting", None, None, None),
    (112, "TechieRag", "Major", "A long answer is cut at the model's limit", "Answers stop mid-sentence on the long documents.", "Answered", "fb-112", "112", "Raise the output limit in Routing; the next release does it for you."),
    (111, "App Manager", "Blocker", "Sign-in fails with a server error", "Every sign-in answered 500 for an hour.", "Fixed", "fb-111", "111", "The database came back; fixed."),
    (110, "Chatur", "Minor", "Labels end in three dots", "Several menus end their labels with three dots.", "Fixed", "fb-110", "110", "Every label now ends without dots."),
    (109, "Chatur", "Minor", "The switcher wraps a long name", "A 30-character project name wraps onto two lines.", "Raised", "fb-109", "109", None),
]
c.execute("DELETE FROM FeedbackEntry WHERE FeedbackEntryId BETWEEN 109 AND 114")
for i, (fid, about, sev, title, detail, state, rid, rnum, answer) in enumerate(rows):
    c.execute("INSERT INTO FeedbackEntry (FeedbackEntryId, About, Severity, Title, Detail, Environment, State, RemoteId, RemoteNumber, Answer, LastError, CreatedUtc) "
              "VALUES (?, ?, ?, ?, ?, NULL, ?, ?, ?, ?, NULL, ?)",
              (fid, about, sev, title, detail, state, rid, rnum, answer, iso(now - datetime.timedelta(hours=i * 7))))
c.commit()
print("feedback: six entries 114 to 109, two waiting to send")
PY
