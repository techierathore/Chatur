"""Shared helpers for the phase 3 screen seeds (auto-run.sh, run-queue.sh, run-report.sh).

tf-verify-screens.sh runs each screen's seed ONCE, just before that screen is first opened at 1280, and the 390 pass
reuses what is in the database. So the three seeds must coexist in one verify database, in any order:

  * each seed owns the rows whose Brief starts with its own marker (MARK_AUTO, MARK_QUEUE, MARK_REPORT) and removes
    only those, never another seed's;
  * "the run going" on the open project is the highest AutoRunId among its Running / WaitingForLimit / Paused
    runs (AutoRunStore.GoingAsync), so the End-to-end run seed's run must hold the highest id of them (raise_auto);
  * the Run report opens the NEWEST-STARTED run of the open project (RunReportActions.RunsAsync orders by
    StartedUtc, not by end), so the report seed's run starts after every other run of the open project, and the
    other seeds' runs on the open project start earlier than it (START_GOING_MIN > START_REPORT_MIN);
  * the queue's other rows live on two private projects that are hidden (RemovedUtc set), so they never appear in
    the open project's run list;
  * nothing the app does can consume the state: the going runs are Paused (End-to-end run) or waiting out a usage
    limit for six hours (queue row 1), and the queue starts a waiting row only when no row is running.

Only a database under tests/.artifacts/ is ever written.
"""
import datetime
import os
import sqlite3

MARK_AUTO = "[seed:auto]"
MARK_QUEUE = "[seed:queue]"
MARK_REPORT = "[seed:report]"
ALL_MARKS = "[seed:"

START_GOING_MIN = 317   # a going run on the open project started 5 h 17 m ago: before the report run below
START_REPORT_MIN = 217  # the report run started 3 h 37 m ago and is the newest run of the open project


def now_utc():
    return datetime.datetime.now(datetime.timezone.utc)


def iso(d):
    return d.strftime("%Y-%m-%dT%H:%M:%S.%f0Z")


def mins(m):
    return datetime.timedelta(minutes=m)


def connect(db, root):
    real = os.path.realpath(db)
    if not (real + os.sep).startswith(os.path.realpath(os.path.join(root, "tests", ".artifacts")) + os.sep):
        raise SystemExit(f"refusing to write to {db}: the seed only writes under tests/.artifacts/")
    return sqlite3.connect(db, timeout=20)


def open_project(c, root):
    """The open project for the seeds: ALWAYS the dedicated seed project (tests/.artifacts/verify/fixtures/screen-seed/
    SeedOpenApp), registered if missing and selected (Setting SelectedProjectId). Never a spec fixture, so the
    seeds' checklist and metrics records never land in a folder a spec reads."""
    path = os.path.join(root, "tests", ".artifacts", "verify", "fixtures", "screen-seed", "SeedOpenApp")
    os.makedirs(path, exist_ok=True)
    write_run_target_projects(path)
    found = c.execute("SELECT ProjectId FROM Project WHERE Path = ?", (path,)).fetchone()
    if found is None:
        pid = c.execute("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, ?, ?, 'Folder', ?)",
                        ("SeedOpenApp", path, iso(now_utc()))).lastrowid
    else:
        pid = found[0]
        c.execute("UPDATE Project SET RemovedUtc = NULL, LastOpenedUtc = ? WHERE ProjectId = ?", (iso(now_utc()), pid))
    c.execute("INSERT INTO Setting (Key, Value) VALUES ('SelectedProjectId', ?) "
              "ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value", (str(pid),))
    # Targets found before the project files existed would stay cached; clear them so the Project menu rediscovers.
    c.execute("DELETE FROM RunTarget WHERE ProjectId = ?", (pid,))
    # File > Recent projects lists every project that has been opened, and the screen check clicks each item in that
    # menu. A click on another project switches the selection to it and the Project menu is then read against a
    # project that has no run targets, not against the open project the mockup draws. Only the open project is
    # recent, so the click lands on itself.
    c.execute("UPDATE Project SET LastOpenedUtc = NULL WHERE ProjectId <> ?", (pid,))
    c.commit()
    return pid, path


def write_run_target_projects(path):
    """Gives the seed project the three projects the mockups' Project menu draws run targets for (Mac Catalyst
    Debug and Release, Web harness, Tests), the same files the web harness's default fixture holds."""
    files = {
        "SeedOpenApp.csproj": '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFrameworks>net10.0-maccatalyst</TargetFrameworks>'
                              '<UseMaui>true</UseMaui></PropertyGroup></Project>',
        "Web harness.csproj": '<Project Sdk="Microsoft.NET.Sdk.Web"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>',
        "Tests.csproj": '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsPackable>false</IsPackable>'
                        '</PropertyGroup><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.0" /></ItemGroup></Project>',
    }
    for name, text in files.items():
        f = os.path.join(path, name)
        if not os.path.exists(f):
            with open(f, "w") as fh:
                fh.write(text)


def hidden_project(c, root, name):
    """A private project the queue rows on 'other projects' belong to. Hidden (RemovedUtc), so it is in no list."""
    path = os.path.join(root, "tests", ".artifacts", "verify", "fixtures", "screen-seed", name)
    os.makedirs(path, exist_ok=True)
    row = c.execute("SELECT ProjectId FROM Project WHERE Path = ?", (path,)).fetchone()
    if row is not None:
        return row[0]
    return c.execute("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc, RemovedUtc) VALUES (NULL, ?, ?, 'Folder', NULL, ?)",
                     (name, path, iso(now_utc()))).lastrowid


def delete_run(c, run_id, with_process_runs=False):
    if with_process_runs:
        for (pr,) in c.execute("SELECT ProcessRunId FROM AutoRunProcess WHERE AutoRunId = ? AND ProcessRunId IS NOT NULL", (run_id,)).fetchall():
            c.execute("DELETE FROM ProcessRunVerdict WHERE RunId = ?", (pr,))
            c.execute("DELETE FROM ProcessRun WHERE RunId = ?", (pr,))
    c.execute("DELETE FROM AutoRunEvent WHERE AutoRunId = ?", (run_id,))
    c.execute("DELETE FROM AutoRunProcess WHERE AutoRunId = ?", (run_id,))
    c.execute("DELETE FROM AutoRun WHERE AutoRunId = ?", (run_id,))


def clear_own(c, mark, with_process_runs=False):
    """Removes this seed's runs (Brief starts with its marker), and nothing else."""
    for (run_id,) in c.execute("SELECT AutoRunId FROM AutoRun WHERE Brief LIKE ? ESCAPE '\\'", (_like(mark),)).fetchall():
        delete_run(c, run_id, with_process_runs)


def stop_foreign_going(c, project):
    """Ends any run going on the project that no seed wrote (a spec's, or an older seed's), so it cannot be picked."""
    c.execute("UPDATE AutoRun SET State = 'Stopped', EndedUtc = ?, StopReason = 'Replaced by the screen-check seed' "
              "WHERE ProjectId = ? AND State IN ('Running', 'Paused', 'WaitingForLimit', 'Starting') "
              "AND Brief NOT LIKE ? ESCAPE '\\'", (iso(now_utc()), project, _like(ALL_MARKS)))


def raise_auto(c, project):
    """Gives the End-to-end run seed's run the highest AutoRunId, so it stays the run 'going' after the queue seed
    wrote a going run of its own on the same project (GoingAsync takes the highest id)."""
    row = c.execute("SELECT AutoRunId FROM AutoRun WHERE ProjectId = ? AND Brief LIKE ? ESCAPE '\\' ORDER BY AutoRunId DESC LIMIT 1",
                    (project, _like(MARK_AUTO))).fetchone()
    if row is None:
        return
    top = c.execute("SELECT MAX(AutoRunId) FROM AutoRun").fetchone()[0]
    if row[0] == top:
        return
    new = top + 1
    for table in ("AutoRun", "AutoRunProcess", "AutoRunEvent"):
        c.execute(f"UPDATE {table} SET AutoRunId = ? WHERE AutoRunId = ?", (new, row[0]))
    c.execute("UPDATE RunQueueItem SET AutoRunId = ? WHERE AutoRunId = ?", (new, row[0]))


def _like(prefix):
    return prefix.replace("\\", "\\\\").replace("%", "\\%").replace("_", "\\_").replace("[", "[") + "%"
