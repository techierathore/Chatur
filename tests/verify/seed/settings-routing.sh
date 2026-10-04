#!/usr/bin/env bash
# Seed for the Routing screen (docs/mockups/settings-routing.html), run by tf-verify-screens.sh before it
# opens /settings/routing, with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws a chain of models in each of the three tiers (tier-1-row-1, tier-2-row-1, tier-3-row-1 ...).
# A verify copy that has only had its tests run holds the shipped chains, where a tier no model is filed
# under is empty. This seed files real models into every empty chain the way the page does (REQ-FN-020):
# the chain is the JSON list of ModelIds in RoutingSetting 'Tier<N>Chain'.
#
# It uses only the models of providers that are Connected in the copy's own database. If there is none, it
# fails with a message and writes nothing: it never invents a provider or a model.
#   Tier 1: the first three connected models, Tier 3: a model of a local (Ollama) provider when there is
#   one, else the last connected model. Tier 2 is filled the same way as Tier 1 only when it is empty.
# Idempotent: a chain that already holds a model is left exactly as it is, so a second run changes nothing.
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
import json, sqlite3, sys
c = sqlite3.connect(sys.argv[1], timeout=20)

models = c.execute(
    "SELECT m.ModelId, p.Connector FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId "
    "WHERE p.State = 'Connected' ORDER BY m.ModelId").fetchall()
if not models:
    print("no connected provider in this copy has a model: connect a provider and let it list its models first, "
          "the seed never invents one")
    sys.exit(1)
known = {r[0] for r in c.execute("SELECT ModelId FROM Model")}
ids = [m[0] for m in models]
local = [m[0] for m in models if m[1] == "Ollama"]
wanted = {1: ids[:3], 2: ids[:3], 3: local[:1] or ids[-1:]}

for tier in (1, 2, 3):
    key = f"Tier{tier}Chain"
    row = c.execute("SELECT Value FROM RoutingSetting WHERE Key = ?", (key,)).fetchone()
    try:
        chain = [i for i in json.loads(row[0]) if i in known] if row and row[0] else []
    except ValueError:
        chain = []
    if chain:
        print(f"tier {tier} already holds {len(chain)} model(s), left alone")
        continue
    value = json.dumps(wanted[tier], separators=(",", ":"))
    if row is None:
        c.execute("INSERT INTO RoutingSetting (Key, Value) VALUES (?, ?)", (key, value))
    else:
        c.execute("UPDATE RoutingSetting SET Value = ? WHERE Key = ?", (value, key))
    print(f"tier {tier}: filed models {wanted[tier]}")

# The page refills empty chains from the shipped tiers until it has been marked as filled once.
c.execute("INSERT OR IGNORE INTO RoutingSetting (Key, Value) VALUES ('TiersInitialised', '1')")
c.commit()
PY
