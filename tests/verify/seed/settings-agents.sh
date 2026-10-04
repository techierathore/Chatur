#!/usr/bin/env bash
# Seed for the Agents screen (docs/mockups/settings-agents.html), run by tf-verify-screens.sh before it
# opens /settings/agents, with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws the Analyst at version 4 with four rows in its History panel
# (history-v1 .. history-v4). A fresh app database holds each seeded role at version 1 only, so this
# seed saves the Analyst up to version 4 the way "Save" does: a new RoleVersionHistory row per version
# (what changed, its date, the wording then) and Role.Version / Role.ValidFromUtc moved to the newest.
# It is idempotent (an Analyst already at version 4 or later is left alone) and it only ever writes to
# the database of the app under tests/.artifacts/ that TF_BASE names.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
[[ -f "$DB" ]] || { echo "no database for the app at $BASE (looked for $DB)"; exit 1; }

python3 - "$DB" <<'PY'
import sqlite3, sys
c = sqlite3.connect(sys.argv[1], timeout=20)
row = c.execute("SELECT RoleId, Wording, Version FROM Role WHERE Code = 'analyst'").fetchone()
if row is None:
    print("the app has no analyst role"); sys.exit(1)
role_id, wording, version = row
steps = {
    2: ("Quotes the repository when it contradicts the owner", "2026-09-12T09:00:00Z"),
    3: ("One requirement at a time, each with an ID", "2026-09-18T09:00:00Z"),
    4: ("Asks for the acceptance test before writing the requirement", "2026-09-22T09:00:00Z"),
}
if version >= 4:
    print(f"analyst already at version {version}"); sys.exit(0)
for v in range(version + 1, 5):
    what, when = steps[v]
    c.execute("INSERT INTO RoleVersionHistory (RoleId, Version, Wording, ValidFromUtc, WhatChanged) VALUES (?,?,?,?,?)",
              (role_id, v, wording, when, what))
c.execute("UPDATE Role SET Version = 4, ValidFromUtc = ? WHERE RoleId = ?", (steps[4][1], role_id))
c.commit()
print("seeded the analyst up to version 4")
PY
