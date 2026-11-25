# AppManager feedback — found while building Chatur

| | |
|---|---|
| App | Chatur |
| Upstream | AppManager |
| Updated | 2026-11-19 |

## Summary

2 entries: 1 blocking now, 1 filed and not blocking, 0 fixed upstream.

Every signed-in check of Chatur is blocked: the production App Manager refuses every sign-in with a server error.

## Entries

### AM-001 — Sign-in fails with a server error; the deep health check reports Unhealthy

> ✅ **Closed 2026-11-20** — re-checked here: Re-checked 2026-11-20 16:29 UTC: /healthz returns Healthy 200 and /health healthy 200; test user 1 signed in through App Manager on every run of tests/verify/menus-many-projects.spec.ts against port 9263 (6 of 6 passed), and sign-in worked all through the 20 Nov verify.

- **Severity:** blocker
- **Blocks:** yes — every Chatur row that needs a signed-in account (all of phases 1 to 4 except the Sign in form itself); the `*verify ui Chatur --phase all` run of 2026-11-19 could not grade them.
- **Repro:** `curl -s -w ' %{http_code}' https://appmgrapi.techierathore.com/healthz`, and sign in to Chatur as test user 1.
- **Expected:** `Healthy` 200; `POST /AuthSvc/device-login` returns the tokens, as it did on 2026-11-18.
- **Actual:** `/healthz` says `Unhealthy` 503 (`/health` says healthy). With Chatur's API key every call, even `GET /AuthSvc/public-key`, returns 500 with an empty body. Without the key, `public-key` returns 200 but `device-login` returns 500 `An error occurred during login`.
- **Encountered in:** `*verify ui Chatur --phase all --port 9263`, 2026-11-19 12:20–12:45 UTC.
- **Workaround:** tried signing in without the API key (the ApplicationId-only way); it fails the same way. None.
- **Suggested fix:** find which dependency `/healthz` marks unhealthy (probably the database) and bring it back; make an unexpected failure return the `INTERNAL_ERROR` envelope with a body, not an empty 500.

#### Detail

```
GET  /AuthSvc/public-key   (X-Api-Key: Chatur's key)  -> 500, content-length: 0, server: Kestrel via Caddy
GET  /AuthSvc/public-key   (no key)                   -> 200
POST /AuthSvc/device-login (no key, applicationId 1)  -> 500 "An error occurred during login"
GET  /health  -> 200 {"status":"healthy","apiVersion":"1.8"}
GET  /healthz -> 503 Unhealthy
```

Chatur's code and configuration did not change the call: the same tests signed in on 2026-11-18.

### AM-002 — No place to keep a developer's roles and rules, so they cannot follow the developer between machines

- **Severity:** major
- **Blocks:** no — REQ-FN-078 (BRD-148) works only in the web harness until this is answered. Chatur has the sync (`IRoleSyncActions`) and a store port (`IRoleSyncStore`), checked in the web harness against a file standing in for App Manager. On a real machine the store says App Manager keeps nothing yet.
- **Repro:** read `docs/AppManager-api-usage-guide.md`: no endpoint stores or returns per-user, per-application data a client writes (a document or key-value area keyed by user and device).
- **Expected:** an authenticated call that saves a named JSON document (a few hundred KB) for the signed-in user and this application, and one that lists every device's document for that user, so a second machine can take what the first sent.
- **Actual:** none documented. Licences, feedback and devices exist; user data does not.
- **Encountered in:** REQ-FN-078, All projects sync panel, 2026-11-20.
- **Workaround:** none in Chatur, by project rule. The harness double proves the logic only.
- **Suggested fix:** add `PUT /UserDataSvc/{key}` and `GET /UserDataSvc/{key}` (per user, per application, with the `X-Device-Id` header kept as part of the record) and tell Chatur the shape.

### AM-LIC-A — The device cap and the warning can be read only at sign-in; no call returns them afterwards

- **Severity:** minor
- **Blocks:** no — REQ-UI-065 and REQ-UI-064 work with it (the real source shows the cap and the warning from the last sign-in in this session, and only while still over the cap).
- **Repro:** read `docs/AppManager-api-usage-guide.md`: `deviceCap` (`maxDevices`, `isOverCap`, `warning`) is returned by `device-login` and `device-register` only. `GET /LicenseSvc` carries no `maxDevices`.
- **Expected:** `maxDevices` on `GET /LicenseSvc`, or a `GET /AuthSvc/devices` envelope carrying the same `deviceCap` block.
- **Actual:** after a restart with a remembered session, Chatur has no cap and no warning to show until the next sign-in.
- **Encountered in:** REQ-UI-064, REQ-UI-065, 2026-11-20.
- **Workaround:** none in Chatur, by project rule.
- **Suggested fix:** add `deviceCap` to the `GET /AuthSvc/devices` response.

### AM-LIC-B — A developer cannot unblock a device, and a feature cannot be switched from the client

- **Severity:** minor
- **Blocks:** no — it limits the Licence screen's Unblock button and its feature switches. On the real source Unblock answers "App Manager has no way to unblock a machine yet" and the switches are shown disabled.
- **Repro:** read the guide: `POST /AuthSvc/devices/{id}/block` exists, there is no unblock; `FeatureSvc` is read-only.
- **Expected:** `POST /AuthSvc/devices/{id}/unblock`. Features are set by the licence in App Manager's admin, which is fine; Chatur only needs the feature codes it reads to be agreed: `auto-run`, `worktrees`, `verifier`, `board`, `measurements`, `integrations` (a code App Manager does not name is treated as on).
- **Actual:** none.
- **Encountered in:** REQ-UI-066, REQ-FN-075, 2026-11-20.
- **Workaround:** none in Chatur; forgetting a blocked device is offered instead.
- **Suggested fix:** add the unblock route and define the six feature codes on Chatur's application.
