# Chatur — Misses

| | |
|---|---|
| App | Chatur |
| Count | 195 logged: 5 open, 190 fixed, 0 will not fix |
| Source | `docs/metrics/misses.jsonl`, one row per miss record. Rewritten by `tf-misses-md.sh` on every new record. Never edit it: a wrong row is corrected by a new record. |
| Updated | 2026-10-04 |

**Whose gap** answers the four questions of the miss protocol: **the app's spec** did not say it, so the checklist line is fixed; **the framework never said it**, so one requirement line and a check are added; **the check was too weak** (a review, or a script that did not fire), so the check is fixed; **said and ignored**, so the rule becomes a hook or is deleted. **not sorted** means the record predates the sort or nobody has answered yet; `bash .tfcore/utils/tf-emit.sh --amend <miss> sort <spec|unsaid|weak-check|ignored>` completes it.

## Open (5)

| Miss | Found | Whose gap | What went wrong |
|---|---|---|---|
| MISS-Chatur-20261004-06 (REQ-NFR-006) | 2026-10-04 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-03 | 2026-10-02 by agent-review | not sorted | The Settings account page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-02 | 2026-10-02 by agent-review | not sorted | The Start mockup draws four active Get started buttons, but the app keeps them disabled and no row says which is right. |
| MISS-Chatur-20260930-36 (REQ-FN-012) | 2026-09-30 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-01 (REQ-NFR-006) | 2026-09-30 by owner | the app's spec | BRD §2 Scope and Technical-Decisions 'Release' promised a nightly download from every push to main, but no BRD requirement carried the release pipeline, so no checklist row built or verified it (added as BRD-158 / REQ-NFR-006 on 2026-09-30). |

## Fixed (190)

| Miss | Found | Closed | Whose gap | What went wrong |
|---|---|---|---|---|
| MISS-Chatur-20261004-05 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac build selected Xcode through an alias folder, and the asset compiler could not find the macOS SDK there. |
| MISS-Chatur-20261004-04 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac build picked Xcode 26.5 from the workload's name, but that workload needs Xcode 26.6, which its own version file states. |
| MISS-Chatur-20261004-03 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac app could not build: it references the ChaturDb console project, which a self-contained Mac app may not reference, and the Mac target had never been compiled before the push. |
| MISS-Chatur-20261004-02 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The second Mac build fix chose the Xcode from the newest Mac workload folder on the runner instead of the pinned one, so the build still failed. |
| MISS-Chatur-20261004-01 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac build on GitHub failed because the workflow took the newest .NET workload, which needs an Xcode the runner does not have. |
| MISS-Chatur-20261003-25 (REQ-FN-039) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-24 (REQ-FN-038) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-23 (REQ-FN-037) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-22 (REQ-UI-009) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-21 (REQ-UI-008) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-20 (REQ-UI-007) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-19 (REQ-UI-006) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-18 (REQ-UI-005) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-17 (REQ-FN-043) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-16 (REQ-FN-042) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-15 (REQ-FN-041) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-14 (REQ-FN-040) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-13 (REQ-FN-039) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-12 (REQ-FN-038) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-11 (REQ-FN-037) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-10 (REQ-UI-034) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-09 (REQ-FN-023) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-08 (REQ-FN-021) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-07 (REQ-FN-020) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-06 (REQ-FN-019) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-05 (REQ-FN-018) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-04 (REQ-FN-017) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-03 (REQ-FN-016) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-02 (REQ-FN-015) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-01 (REQ-FN-014) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-66 (REQ-FN-019) | 2026-10-02 by agent-review | 2026-10-03 by amend-docs | not sorted | The Routing mockup anchors the items of every tier menu as if the menus were always open, without marking them as another state, so the page can never show them on first view. |
| MISS-Chatur-20261002-65 (REQ-UI-038) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-64 (REQ-UI-037) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-63 (REQ-UI-036) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-62 (REQ-UI-035) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-61 (REQ-FN-043) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-60 (REQ-FN-042) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-59 (REQ-FN-041) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-58 (REQ-FN-040) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-57 (REQ-FN-030) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-56 (REQ-UI-030) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-55 (REQ-UI-029) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-54 (REQ-UI-028) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-53 (REQ-UI-027) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-52 (REQ-FN-029) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-51 (REQ-FN-028) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-50 (REQ-UI-026) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-49 (REQ-UI-025) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-48 (REQ-FN-023) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-47 (REQ-FN-021) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-46 (REQ-FN-020) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-45 (REQ-FN-019) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-44 (REQ-FN-018) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-43 (REQ-UI-020) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-42 (REQ-UI-019) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-41 (REQ-FN-017) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-40 (REQ-FN-016) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-39 (REQ-FN-015) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-38 (REQ-FN-014) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-37 (REQ-UI-009) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-36 (REQ-UI-008) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-35 (REQ-FN-006) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-34 (REQ-UI-007) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-33 (REQ-UI-006) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-32 (REQ-UI-005) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-31 (REQ-FN-004) | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Register test creates a new real App Manager account every time it runs, instead of using a UsageGuide test user. |
| MISS-Chatur-20261002-30 | 2026-10-02 by agent-review | 2026-10-03 by amend-docs | not sorted | The Appearance mockup draws a text panel with font size and line height controls that no requirement asks for and no action supports. |
| MISS-Chatur-20261002-29 (REQ-UI-037) | 2026-10-02 by agent-review | 2026-10-03 by amend-docs | not sorted | The Appearance mockup anchors the light, dark and follow-system choices inside a closed menu without marking them as another state, so the page can never show them on first view. |
| MISS-Chatur-20261002-28 (REQ-UI-038) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-27 (REQ-UI-037) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-26 (REQ-UI-036) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-25 (REQ-UI-035) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-24 (REQ-UI-034) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-23 (REQ-UI-030) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-22 (REQ-UI-029) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-21 (REQ-UI-028) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-20 (REQ-UI-027) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-19 (REQ-UI-026) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-18 (REQ-UI-025) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-17 (REQ-UI-020) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-16 (REQ-UI-019) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-15 (REQ-UI-009) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-14 (REQ-UI-008) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-13 (REQ-UI-007) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-12 (REQ-UI-006) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-11 (REQ-UI-005) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-10 (REQ-UI-004) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-09 (REQ-UI-003) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-08 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings routing page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-07 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings measurements page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-06 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings corrections page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-05 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings appearance page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-04 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings agents page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-01 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Measurements mockup draws a Where they are written panel that no checklist row asks for, so it was never built. |
| MISS-Chatur-20261001-01 (REQ-UI-037) | 2026-10-01 by gate | 2026-10-02 by amend-docs | the app's spec | The UIDesign says Amber and dark are the default look, but every mockup draws Indigo and light by default (docs/mockups/chatur.css), so the screen comparison reports a colour difference on every accented control. |
| MISS-Chatur-20260930-97 | 2026-09-30 by gate | 2026-10-01 by amend-docs | the app's spec | None of the 27 mockups mark popup contents, sample rows or later-phase links as state-only or mockup-only, so the render check reports about 210 controls as missing on every screen and no screen row can reach Verified until the mockups are annotated. |
| MISS-Chatur-20260930-96 (REQ-FN-048) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-95 (REQ-FN-047) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-94 (REQ-UI-044) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-93 (REQ-UI-043) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-92 (REQ-UI-042) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-91 (REQ-FN-046) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-90 (REQ-FN-045) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-89 (REQ-UI-041) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-88 (REQ-FN-044) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-87 (REQ-UI-040) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-86 (REQ-UI-039) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-85 (REQ-UI-038) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-84 (REQ-UI-037) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-83 (REQ-UI-036) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-82 (REQ-UI-035) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-81 (REQ-FN-043) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-80 (REQ-FN-042) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-79 (REQ-FN-041) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-78 (REQ-FN-040) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-77 (REQ-FN-039) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-76 (REQ-FN-038) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-75 (REQ-FN-037) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-74 (REQ-UI-034) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-73 (REQ-UI-033) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-72 (REQ-FN-036) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-71 (REQ-FN-035) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-70 (REQ-FN-034) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-69 (REQ-FN-033) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-68 (REQ-UI-032) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-67 (REQ-FN-032) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-66 (REQ-FN-031) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-65 (REQ-UI-031) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-64 (REQ-FN-030) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-63 (REQ-UI-030) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-62 (REQ-UI-029) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-61 (REQ-UI-028) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-60 (REQ-UI-027) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-59 (REQ-FN-029) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-58 (REQ-FN-028) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-57 (REQ-UI-026) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-56 (REQ-UI-025) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-55 (REQ-UI-024) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-54 (REQ-UI-023) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-53 (REQ-UI-022) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-52 (REQ-UI-021) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-51 (REQ-FN-027) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-50 (REQ-FN-026) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-49 (REQ-FN-025) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-48 (REQ-FN-024) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-47 (REQ-FN-023) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-46 (REQ-FN-022) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-45 (REQ-FN-021) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-44 (REQ-FN-019) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-43 (REQ-FN-018) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-42 (REQ-UI-020) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-41 (REQ-UI-019) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-40 (REQ-FN-017) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-39 (REQ-FN-016) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-38 (REQ-FN-015) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-37 (REQ-FN-013) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-35 (REQ-UI-018) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-34 (REQ-UI-017) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-33 (REQ-FN-011) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-32 (REQ-FN-010) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-31 (REQ-UI-016) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-30 (REQ-UI-015) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-29 (REQ-UI-014) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-28 (REQ-UI-013) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-27 (REQ-UI-012) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-26 (REQ-UI-011) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-25 (REQ-FN-009) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-24 (REQ-UI-010) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-23 (REQ-FN-008) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-22 (REQ-FN-007) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-21 (REQ-UI-009) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-20 (REQ-UI-008) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-19 (REQ-FN-006) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-18 (REQ-UI-007) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-17 (REQ-UI-006) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-16 (REQ-UI-005) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-15 (REQ-UI-004) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-14 (REQ-FN-005) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-13 (REQ-UI-003) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-12 (REQ-FN-004) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-11 (REQ-FN-003) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-10 (REQ-FN-002) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-09 (REQ-UI-002) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-08 (REQ-FN-001) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-07 (REQ-UI-001) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-06 (REQ-UI-009) | 2026-09-30 by gate | 2026-10-03 by build-phase | the check was too weak | Removing a project folder failed with a raw FOREIGN KEY database error once any project in it had been opened, because its projects were deleted outright while sessions still pointed at them. |
| MISS-Chatur-20260930-05 (REQ-FN-040) | 2026-09-30 by gate | 2026-10-01 by build-phase | the check was too weak | Nothing in Chatur ever writes a measurement: MeasurementActions.AppendAsync has no caller, so an agent session leaves no docs/metrics files in the project. |
| MISS-Chatur-20260930-04 (REQ-UI-034) | 2026-09-30 by gate | 2026-10-01 by build-phase | the check was too weak | No part of Chatur ever records a correction: there is no agent tool that changes a role, rule or step wording and logs it, so the Corrections page can only ever be empty. |
| MISS-Chatur-20260930-03 (REQ-FN-020) | 2026-09-30 by gate | 2026-10-01 by build-phase | the check was too weak | Routing rows (role tiers, work tiers, chain order, remembered) were marked Implemented at 100% while the Routing page had no controls at all, only three empty tier cards. |
| MISS-Chatur-20260930-02 (REQ-FN-014) | 2026-09-30 by agent-review | 2026-10-01 by build-phase | the check was too weak | Connecting a provider was marked Implemented and live-smoked, but the Add a provider button was disabled, the dialog was never built, and the OpenAI-compatible connector was refused, so the owner could not connect any model. |
