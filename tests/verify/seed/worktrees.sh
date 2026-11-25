#!/usr/bin/env bash
# Seed for Worktrees (docs/mockups/worktrees.html), run by tf-verify-screens.sh and tf-mockup-parity.sh before
# they open /worktrees, with TF_BASE set to the running app's address (verify of 21 Nov 2026).
#
# The mockup draws the project's own folder (main) and three working copies: one a run is working in (run 482),
# one free, and one whose run has finished (run 471). This seed opens the screens' shared seed project (a
# repository of its own, _seedlib.open_project), adds the three copies beside it as real working copies of that
# repository (folders named 482, free and done), and records the two runs against each copy's own hidden project
# row, the way "Run here" does. Run 482 is waiting, so it counts as in use without the engine picking it up.
# Idempotent. Only ever writes under tests/.artifacts/, and only to the seed project's own repository.
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

python3 - "$DB" "$ROOT" "$(dirname "${BASH_SOURCE[0]}")" <<'PY'
import os, subprocess, sys
sys.path.insert(0, sys.argv[3])
import _seedlib as s

db, root = sys.argv[1], sys.argv[2]
c = s.connect(db, root)
s.open_project(c, root)
repo = os.path.join(root, "tests", ".artifacts", "verify", "fixtures", "screen-seed", "SeedOpenApp")
if not s.is_own_repository(repo):
    raise SystemExit(f"refusing to go on: {repo} is not a repository of its own")
env = dict(os.environ, GIT_AUTHOR_NAME="Verify Seed", GIT_AUTHOR_EMAIL="verify-seed@example.com",
           GIT_COMMITTER_NAME="Verify Seed", GIT_COMMITTER_EMAIL="verify-seed@example.com")
listed = subprocess.run(["git", "-C", repo, "worktree", "list", "--porcelain"], capture_output=True, text=True, env=env).stdout
copies = os.path.join(root, "tests", ".artifacts", "verify", "fixtures", "screen-seed", "wt")
os.makedirs(copies, exist_ok=True)
subprocess.run(["git", "-C", repo, "worktree", "prune"], check=False, env=env)
for name in ("482", "free", "done"):
    path = os.path.join(copies, name)
    if f"worktree {path}\n" in listed and os.path.isdir(path):
        continue
    if os.path.isdir(path) and os.listdir(path):
        raise SystemExit(f"refusing to go on: {path} holds files but is no working copy of the seed project")
    subprocess.run(["git", "-C", repo, "worktree", "add", "-q", "-B", f"seed-wt-{name}", path], check=True, env=env)

now = s.now_utc()
# A screen check opens "Run here" and starts real runs in these copies; each seed run puts back exactly the drawn
# two, so the newest run of each copy is again 482 and 471 (seen 21 Nov 2026: runs 483 and 484 hid run 471).
for name in ("482", "free", "done"):
    pid = s.hidden_project(c, root, os.path.join("wt", name))
    c.execute("DELETE FROM ProcessRun WHERE ProjectId = ? AND RunId NOT IN (482, 471)", (pid,))
for run_id, name, state, step in ((482, "482", "waiting", 4), (471, "done", "finished", 11)):
    pid = s.hidden_project(c, root, os.path.join("wt", name))
    c.execute("DELETE FROM ProcessRun WHERE RunId = ?", (run_id,))
    c.execute("INSERT INTO ProcessRun (RunId, ProjectId, ProcessCode, State, CurrentStep, StartedUtc, EndedUtc) VALUES (?, ?, 'build-phase', ?, ?, ?, ?)",
              (run_id, pid, state, step, s.iso(now - s.mins(90)), None if state == "waiting" else s.iso(now - s.mins(30))))
c.commit()
print("worktrees: main and three copies (482 in use, free, done)")
PY
