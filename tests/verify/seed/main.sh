#!/usr/bin/env bash
# Seed for the Workbench (docs/mockups/main.html), run by tf-verify-screens.sh before it opens /, with
# TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup's conversation is one question and one reply carrying its steps: two tool steps, a refused
# `git commit` and a build. A verify copy holds whatever conversation its tests left behind, so this seed
# starts a new session on the open project (the most recently opened one) and writes that exchange into
# it the way AgentActions does: SessionEvent rows user → tool → tool → refusal → tool → assistant, so each
# step is drawn under the reply it led to (REQ-UI-023).
# Idempotent: a session already seeded (its first event is the seed's question) is left as it is.
# It only ever writes to the database of the app under tests/.artifacts/ that TF_BASE names.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
[[ -f "$DB" ]] || { echo "no database for the app at $BASE (looked for $DB)"; exit 1; }
case "$DB" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $DB: the seed only writes under tests/.artifacts/"; exit 1 ;;
esac

python3 - "$DB" <<'PY'
import datetime, sqlite3, sys
c = sqlite3.connect(sys.argv[1], timeout=20)
QUESTION = "The repos table on the coverage screen needs paging — 25 rows a page, keep the sort."

project = c.execute("SELECT ProjectId FROM Project WHERE LastOpenedUtc IS NOT NULL ORDER BY LastOpenedUtc DESC LIMIT 1").fetchone()
if not project:
    print("no project has been opened in this copy: open one first, the seed never invents a project")
    sys.exit(1)
latest = c.execute("SELECT SessionId, RoleId, Mode FROM Session WHERE ProjectId = ? ORDER BY SessionId DESC LIMIT 1", project).fetchone()
if latest:
    first = c.execute("SELECT Payload FROM SessionEvent WHERE SessionId = ? ORDER BY Seq LIMIT 1", (latest[0],)).fetchone()
    if first and first[0] == QUESTION:
        print("main: the open project's newest session is already the seeded one")
        sys.exit(0)
role = latest[1] if latest else c.execute("SELECT RoleId FROM Role ORDER BY RoleId LIMIT 1").fetchone()[0]
mode = latest[2] if latest else "AskFirst"

now = datetime.datetime.now(datetime.timezone.utc)
iso = lambda d: d.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
cur = c.execute("INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (?, ?, ?, 'Active', ?)",
                (project[0], role, mode, iso(now)))
session = cur.lastrowid
rows = [
    ("user", QUESTION, None, None),
    ("tool", "read Coverage.razor and Query.cs", None, None),
    ("tool", "searched for RepoRow — 6 hits", None, None),
    ("refusal", "asked to run git commit — refused, you check in yourself", None, None),
    ("tool", "dotnet build TfLens.sln", None, None),
    ("assistant", "The table is bound straight to the list, so the paging belongs in the query, not the component.", "Claude Opus 5", 3412),
]
for seq, (kind, text, model, tokens) in enumerate(rows, start=1):
    c.execute("INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, Model, Tokens, CreatedUtc) VALUES (?, ?, ?, ?, ?, ?, ?)",
              (session, seq, kind, text, model, tokens, iso(now + datetime.timedelta(milliseconds=seq))))
c.commit()
print(f"main: session {session} on project {project[0]} holds the mockup's exchange")
PY
