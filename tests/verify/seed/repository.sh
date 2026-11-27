#!/usr/bin/env bash
# Seed for the Repository screen (docs/mockups/repository.html), run by tf-verify-screens.sh before it
# opens /repository, with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws check-ins that only process branches hold ("a process wrote it") and rows under
# "Branches a process made". This seed makes that state real: two run/ branches (run/BRD-96-101 and
# run/BRD-88-95), one commit each, of the repository the running app has open, carrying requirement ids
# in their messages (REQ-FN-048). It writes the commits with git's own plumbing, so the open branch,
# its working tree and its pending changes are left exactly as they were.
#
# The history list (HEAD plus every run/ branch, newest first) is made to read, from the top, the way the
# mockup's rows do: hand, hand, process, hand, process, hand (the mockup's rows 3 and 5 are "a process wrote it").
#
#   newest  h1 hand     (empty check-in on the open branch)
#           h2 hand     (empty check-in on the open branch)
#           P1 process  run/BRD-96-101   (today 08:52)
#           h4 hand
#           P2 process  run/BRD-88-95    (yesterday 16:07)
#   oldest  h6 hand, on top of whatever the fixture's own history ended with
#
# Hand check-ins are EMPTY commits (the tree of the commit under them), written with commit-tree and moved onto
# the branch HEAD points at with update-ref, so the working tree, the index and the pending changes the Changes
# panel shows do not change. Both process commits sit on h6 (not on the fixture's own tip), so the fixture's own
# tip is only ever listed after h6, whatever its date.
#
# Idempotent: hand commits carry the marker "[verify-seed]" in their message. Each run first steps below the
# contiguous seeded commits at the top of the branch and builds on what is under them; dates, trees, parents and
# messages are fixed, so a second run writes the very same commits, and a commit the seed did not make is never
# rewritten. Each run branch is reset to exactly ONE seeded commit.
#
# The open project is read from the running copy's own database: the Setting row SelectedProjectId,
# then that Project row's Path (REQ-FN-006). The seed only ever writes to a repository under
# tests/.artifacts/ (the verify fixtures), never to Chatur's own repository.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE_URL="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE_URL")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
# Each account has its own database (BRD-210): seed the file of whoever signed in last, named by
# harness-data/accounts/current.txt, or the install's own before anyone has signed in.
POINTER="$(dirname "$DB")/accounts/current.txt"
if [ -s "$POINTER" ]; then DB="$(dirname "$DB")/accounts/$(tr -d '\r\n' < "$POINTER")/chatur.db"; fi
[[ -f "$DB" ]] || { echo "no database for the app at $BASE_URL (looked for $DB)"; exit 1; }

REPO="$(python3 - "$DB" <<'PY'
import sqlite3, sys
c = sqlite3.connect(sys.argv[1])
row = c.execute("SELECT p.Path FROM Setting s JOIN Project p ON p.ProjectId = CAST(s.Value AS INTEGER) WHERE s.Key = 'SelectedProjectId'").fetchone()
print(row[0] if row else "")
PY
)"
# A solution's project path is its .sln (or .slnx) file; its repository is the folder around it (BRD-227, 18 Nov).
[[ -f "$REPO" ]] && REPO="$(dirname "$REPO")"
[[ -n "$REPO" && -d "$REPO" ]] || { echo "the app at $BASE_URL has no project open"; exit 1; }
case "$(cd "$REPO" && pwd)/" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $REPO: the seed only writes to repositories under tests/.artifacts/"; exit 1 ;;
esac

export GIT_AUTHOR_NAME="Verify Seed" GIT_AUTHOR_EMAIL="verify-seed@example.com"
export GIT_COMMITTER_NAME="Verify Seed" GIT_COMMITTER_EMAIL="verify-seed@example.com"

# Another spec may have left a plain fixture folder open (the metrics fixture is not a repository). The screen
# draws a repository, so make this fixture one: one initial check-in of what is there, on main.
#
# "Is a repository" means git names THIS folder as the top one. A .git folder that has lost its objects, refs or
# config (the 7-day artifact sweep deletes old files one at a time) is skipped by git, which then finds the
# repository above the fixture: Chatur's own. On 2026-11-13 that happened, and this seed put its four empty hand
# check-ins on the owner's main branch. A folder test (-d .git) cannot see that, so git itself is asked, a broken
# .git is made again, and the seed stops rather than write anywhere else.
own_repo() { [[ "$(git -C "$REPO" rev-parse --show-toplevel 2>/dev/null)" == "$(cd "$REPO" && pwd -P)" ]]; }
if ! own_repo; then
  rm -rf "$REPO/.git"
  git -C "$REPO" init -q --initial-branch=main
  own_repo || { echo "refusing to write: $REPO is not a git repository of its own after git init"; exit 1; }
  git -C "$REPO" add -A
  git -C "$REPO" commit -q --allow-empty -m "initial" --no-gpg-sign
fi

MARKER="[verify-seed]"
PROCESS_NOTE="process check-in seeded for the Repository mockup"

stamp() { # $1 = epoch; prints "<epoch> <utc offset>"
  echo "$1 $(python3 -c "import time; print(time.strftime('%z', time.localtime($1)))")"
}

# Dates. Process check-ins follow the mockup's "Last touched" column (today 08:52, yesterday 16:07) and the hand
# check-ins sit 1 minute either side of them, never later than now. But the history sorts by date, so a check-in
# only a process branch holds that somebody else made (the Repository spec makes one on its own run/ branch, dated
# when it ran) would sort above everything and push the seeded rows down. So all six seeded commits must be newer
# than every commit the seed did not make: when the nominal times are not, the six follow each other one second
# apart right after the newest such commit (waiting out those seconds if now is that close). That date is read from
# commits the seed did not make, so a second run finds the same one and writes the same commits.
FLOOR="$(git -C "$REPO" log --all --format='%ct%x09%s' | awk -F'\t' -v m="$MARKER" -v n="$PROCESS_NOTE" 'index($2, m) != 1 && index($2, n) == 0 && $1 > max { max = $1 } END { print max + 0 }')"
NOW="$(date +%s)"
T_P1="$(python3 -c "import datetime,time; d=datetime.datetime.combine(datetime.date.today(),datetime.time(8,52)); print(int(d.timestamp()))")"
if (( T_P1 > NOW - 180 )); then T_P1="$(python3 -c "import datetime,time; d=datetime.datetime.combine(datetime.date.today()-datetime.timedelta(days=1),datetime.time(23,30)); print(int(d.timestamp()))")"; fi
T_P2="$(python3 -c "import datetime,time; d=datetime.datetime.combine(datetime.date.today()-datetime.timedelta(days=1),datetime.time(16,7)); print(int(d.timestamp()))")"
if (( T_P2 >= T_P1 )); then T_P2=$(( T_P1 - 3600 )); fi
T_H6=$(( T_P2 - 60 )); T_H4=$(( T_P2 + 60 )); T_H2=$(( T_P1 + 60 )); T_H1=$(( T_P1 + 120 ))
if (( T_H6 <= FLOOR )); then
  T_H6=$(( FLOOR + 1 )); T_P2=$(( FLOOR + 2 )); T_H4=$(( FLOOR + 3 )); T_P1=$(( FLOOR + 4 )); T_H2=$(( FLOOR + 5 )); T_H1=$(( FLOOR + 6 ))
  while (( $(date +%s) < T_H1 )); do sleep 1; done
fi

# The mockup's open branch is main (branch-current). The menu walk switches the fixture to other branches and can
# leave it on one, run/BRD-96-101 included (2026-11-27, REQ-UI-042): the hand check-ins below then went onto that
# run branch, and the app, true to the data, marked them "a process wrote it" on rows 1, 2 and 4. So the open branch
# is put back on main first. `git switch` carries pending changes across; a switch git refuses stops the seed.
if git -C "$REPO" rev-parse -q --verify refs/heads/main >/dev/null; then
  if [[ "$(git -C "$REPO" symbolic-ref -q HEAD || echo HEAD)" != "refs/heads/main" ]]; then
    git -C "$REPO" switch -q main || { echo "could not switch $REPO back to main; the seed writes nothing"; exit 1; }
    echo "switched the fixture back to main"
  fi
elif [[ "$(git -C "$REPO" symbolic-ref -q HEAD || echo HEAD)" == refs/heads/run/* ]]; then
  echo "refusing to seed: $REPO is open on a run branch and has no main to go back to"; exit 1
fi

BRANCH_REF="$(git -C "$REPO" symbolic-ref -q HEAD || echo HEAD)" # the branch HEAD points at (or HEAD itself when detached)

BASE="$(git -C "$REPO" rev-parse HEAD)"
while [[ "$(git -C "$REPO" log -1 --format=%s "$BASE")" == "$MARKER"* ]] && git -C "$REPO" rev-parse -q --verify "$BASE^" >/dev/null; do
  BASE="$(git -C "$REPO" rev-parse "$BASE^")"
done
BASE_TREE="$(git -C "$REPO" rev-parse "$BASE^{tree}")"

commit_at() { # $1 tree  $2 parent  $3 epoch  $4 message
  local vWhen; vWhen="$(stamp "$3")"
  GIT_AUTHOR_DATE="$vWhen" GIT_COMMITTER_DATE="$vWhen" git -C "$REPO" commit-tree "$1" -p "$2" -m "$4"
}

# A run branch only moves with update-ref; HEAD, the working tree and the index stay as they were.
seed_run() { # $1 branch  $2 message  $3 epoch  $4 parent
  local vBranch="$1" vBlob vTree vCommit
  vBlob="$(printf 'seeded by tests/verify/seed/repository.sh for %s\n' "$vBranch" | git -C "$REPO" hash-object -w --stdin)"
  vTree="$( (git -C "$REPO" ls-tree "$BASE" | grep -v $'\tverify-seed.txt$' || true; printf '100644 blob %s\tverify-seed.txt\n' "$vBlob") | git -C "$REPO" mktree)"
  vCommit="$(commit_at "$vTree" "$4" "$3" "$2")"
  git -C "$REPO" update-ref "refs/heads/$vBranch" "$vCommit"
  echo "seeded $vBranch at ${vCommit:0:7} in $REPO"
}

H6="$(commit_at "$BASE_TREE" "$BASE" "$T_H6" "$MARKER Tidy the notes by hand")"
seed_run "run/BRD-88-95" "[REQ-UI-088] [REQ-UI-095] $PROCESS_NOTE" "$T_P2" "$H6"
H4="$(commit_at "$BASE_TREE" "$H6" "$T_H4" "$MARKER Rename a heading by hand")"
seed_run "run/BRD-96-101" "[REQ-FN-096] [REQ-FN-101] $PROCESS_NOTE" "$T_P1" "$H6"
H2="$(commit_at "$BASE_TREE" "$H4" "$T_H2" "$MARKER Fix a typo by hand")"
H1="$(commit_at "$BASE_TREE" "$H2" "$T_H1" "$MARKER Update the readme by hand")"
git -C "$REPO" update-ref "$BRANCH_REF" "$H1"

# The Repository mockup also shows a plain feature branch in the branch picker. Moved with update-ref, as the
# run branches are: `git branch -f` refuses the branch that is checked out, and an earlier menu walk can leave
# the fixture on feature/board (2026-11-14). When it is the open branch it already holds the hand check-ins.
if [[ "$BRANCH_REF" != "refs/heads/feature/board" ]]; then
  git -C "$REPO" update-ref refs/heads/feature/board "$H6"
fi

echo "seeded the hand check-ins on ${BRANCH_REF#refs/heads/}: tip ${H1:0:7} in $REPO"
