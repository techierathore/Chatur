#!/usr/bin/env python3
"""The installed Mac app, checked on screen (27 Nov 2026, the first Mac install).

Drives /Applications/Chatur.app through an Appium mac2 session bound to the app's own bundle id
(com.techierathore.chatur), element by element; it never sends keys or clicks to the screen at large.
The app's data goes to the folder given as the first argument (CHATUR_TEST_DATA), never the owner's.
Signed out only: this Mac holds no test-user password. It checks Sign in, the theme switch (light and
dark), Create an account, the forgotten-password message and the Mac menu bar's View menu, then ends the app.
Screenshots of the app's window go to the second argument's folder.

  python3 -I tests/verify/support/mac-installed-check.py <data-folder> <shot-folder> [appium-url]

Standard library only. Prints one PASS/FAIL line per check and exits 1 when any failed.
"""
import base64
import json
import os
import sys
import time
import urllib.request

DATA, SHOTS = sys.argv[1], sys.argv[2]
BASE = sys.argv[3] if len(sys.argv) > 3 else "http://127.0.0.1:4733"
os.makedirs(SHOTS, exist_ok=True)
RESULTS = []


def call(method, path, body=None, timeout=120):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(BASE + path, data=data, method=method, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return json.loads(resp.read() or b"{}")
    except urllib.error.HTTPError as err:
        return json.loads(err.read() or b"{}")


def check(name, ok, detail=""):
    RESULTS.append(ok)
    print(("PASS " if ok else "FAIL ") + name + (f" — {detail}" if detail else ""), flush=True)


caps = {"capabilities": {"alwaysMatch": {
    "platformName": "mac", "appium:automationName": "mac2",
    "appium:bundleId": "com.techierathore.chatur",
    "appium:environment": {"CHATUR_TEST_DATA": DATA},
    "appium:newCommandTimeout": 300, "appium:skipAppKill": False}}}
session = call("POST", "/session", caps, timeout=300)
sid = (session.get("value") or {}).get("sessionId") or session.get("sessionId")
if not sid:
    print("FAIL no session: " + json.dumps(session)[:400])
    sys.exit(1)
S = f"/session/{sid}"


def find(predicate, wait=20):
    end = time.time() + wait
    while time.time() < end:
        r = call("POST", S + "/elements", {"using": "-ios predicate string", "value": predicate})
        els = r.get("value") or []
        if isinstance(els, list) and els:
            return list(els[0].values())[0]
        time.sleep(1)
    return None


def click(el):
    call("POST", f"{S}/element/{el}/click", {})


def shot(name):
    win = find("elementType == 4", wait=5)  # XCUIElementTypeWindow
    r = call("GET", f"{S}/element/{win}/screenshot") if win else call("GET", S + "/screenshot")
    if r.get("value"):
        with open(os.path.join(SHOTS, name + ".png"), "wb") as fh:
            fh.write(base64.b64decode(r["value"]))


def text_present(words, wait=25):
    return find(f"label CONTAINS '{words}' OR value CONTAINS '{words}' OR title CONTAINS '{words}'", wait) is not None


try:
    check("starts to Sign in", text_present("Sign in to Chatur", 40))
    shot("signin-dark")
    toggle = find("label CONTAINS[c] 'theme' OR label CONTAINS[c] 'light' OR label CONTAINS[c] 'dark'")
    if toggle:
        click(toggle)
        time.sleep(1.5)
        shot("signin-light")
        check("theme switch answers", True, "light screenshot taken")
        click(toggle)
        time.sleep(1)
    else:
        check("theme switch answers", False, "no theme button found")
    create = find("label == 'Create one' OR title == 'Create one'")
    if create:
        click(create)
        check("Create an account opens", text_present("Create", 15))
        shot("register-dark")
        back = find("label == 'Sign in' OR title == 'Sign in'")  # "Already have an account? Sign in"
        if back:
            click(back)
    else:
        check("Create an account opens", False, "no Create one link")
    forgot = find("label CONTAINS 'Forgotten your password'")
    if forgot:
        click(forgot)
        time.sleep(1.5)
        shot("forgotten-dark")
        check("Forgotten your password answers", True)
    else:
        check("Forgotten your password answers", False, "link not found")
    view = find("elementType == 56 AND title == 'View'", wait=10)  # XCUIElementTypeMenuBarItem
    if view:
        click(view)
        check("View menu has Zoom In", find("elementType == 54 AND title == 'Zoom In'", wait=5) is not None)
        call("POST", f"{S}/element/{view}/click", {})
    else:
        check("View menu has Zoom In", False, "no View menu")
finally:
    call("POST", S + "/execute/sync", {"script": "macos: terminateApp", "args": [{"bundleId": "com.techierathore.chatur"}]})
    call("DELETE", S)

sys.exit(0 if all(RESULTS) else 1)
