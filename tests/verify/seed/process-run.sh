#!/usr/bin/env bash
# Seed for the Process run screen (docs/mockups/process-run.html), run by tf-verify-screens.sh and
# tf-mockup-parity.sh before they open /processes/<id>/run, with TF_BASE set to the running app's address.
#
# The screen carries the same conversation panel as the Workbench, and its mockup draws earlier chats in the
# panel's history list. On a fresh harness database there are none, so the list held only the disabled
# "Nothing worked on yet" and the screen check read the history menu as opening nothing (build-phase,
# 20 Nov 2026). The Workbench's own seed puts that drawn state in place for the open project, so this one runs it.
set -euo pipefail
exec bash "$(dirname "${BASH_SOURCE[0]}")/main.sh"
