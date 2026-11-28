#!/usr/bin/env python3
"""A local stand-in for App Manager, for the Mac screen walk only (tests/verify/mac-screen-walk.spec.ts).

Python 3 stdlib only. It answers exactly the calls Chatur's App Manager clients make
(src/Chatur.Core/Accounts/AppManagerClient.cs, src/Chatur.Core/Licences/AppManagerLicenceClient.cs) with the
envelope they read ({ success, data, message }), so the web harness can be signed in on a machine that has no
test-user passwords. It accepts ANY password for ANY email (the password arrives RSA-OAEP encrypted and is never
decrypted or stored) and keeps nothing between runs. It never talks to the real App Manager.

Usage: python3 appmanager-standin.py <port> <public-key.pem> <request-log-file>
The public key is made by the walk's runner with `openssl genpkey` under tests/.artifacts/mac-walk/standin/.
Feedback and roles never reach here: the harness uses its own doubles for those (appsettings.json).
"""
import hashlib
import json
import sys
from datetime import datetime, timedelta, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

PORT = int(sys.argv[1])
PUBLIC_KEY = open(sys.argv[2], encoding="utf-8").read()
LOG_PATH = sys.argv[3]


def _iso(aDelta: timedelta) -> str:
    return (datetime.now(timezone.utc) + aDelta).strftime("%Y-%m-%dT%H:%M:%SZ")


def _user(aEmail: str, aFirst: str = "Mac", aLast: str = "Walk") -> dict:
    vId = int(hashlib.sha256(aEmail.lower().encode("utf-8")).hexdigest()[:8], 16) % 900000 + 100000
    return {
        "userId": vId,
        "email": aEmail,
        "firstName": aFirst,
        "lastName": aLast,
        "accessToken": "standin-access-" + str(vId),
        "refreshToken": "standin-refresh-" + str(vId),
        "tokenExpiresAt": _iso(timedelta(hours=8)),
        "deviceCap": None,
    }


class Handler(BaseHTTPRequestHandler):
    def log_message(self, aFormat, *aArgs):  # one line per call, path only, never a body
        with open(LOG_PATH, "a", encoding="utf-8") as vLog:
            vLog.write(f"{datetime.now().isoformat(timespec='seconds')} {self.command} {self.path} {aArgs[1] if len(aArgs) > 1 else ''}\n")

    def _body(self) -> dict:
        vLength = int(self.headers.get("Content-Length") or 0)
        if vLength == 0:
            return {}
        try:
            return json.loads(self.rfile.read(vLength).decode("utf-8"))
        except ValueError:
            return {}

    def _send(self, aData=None, aSuccess=True, aMessage=None, aStatus=200):
        vText = json.dumps({"success": aSuccess, "data": aData, "message": aMessage}).encode("utf-8")
        self.send_response(aStatus)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(vText)))
        self.end_headers()
        self.wfile.write(vText)

    def _path(self) -> str:
        return self.path.split("?", 1)[0].strip("/").lower()

    def do_GET(self):
        vPath = self._path()
        if vPath == "authsvc/public-key":
            return self._send({"publicKey": PUBLIC_KEY})
        if vPath == "licensesvc":
            return self._send([{
                "licenseName": "Stand-in Professional",
                "licenseKey": "STANDIN-0000-0000",
                "status": "Active",
                "activationDate": _iso(timedelta(days=-30)),
                "expiryDate": _iso(timedelta(days=335)),
                "daysRemaining": 335,
            }])
        if vPath == "authsvc/devices":
            return self._send([{
                "userDeviceId": 1,
                "deviceIdentifier": "standin-device",
                "deviceName": "Stand-in MacBook",
                "platform": "macOS",
                "appVersion": "1.0.0",
                "signInCount": 1,
                "lastSeenDate": _iso(timedelta(0)),
                "isBlocked": False,
            }])
        if vPath == "featuresvc":
            return self._send([])  # a feature not listed is on (LicenceActions.Join)
        if vPath == "healthz":
            return self._send({"ok": True})
        return self._send(None, False, "The stand-in has no such endpoint.", 404)

    def do_POST(self):
        vPath = self._path()
        vBody = self._body()
        if vPath == "authsvc/device-login":
            return self._send(_user(str(vBody.get("email") or "mac-walk@example.invalid")))
        if vPath == "authsvc/device-register":
            return self._send(_user(str(vBody.get("email") or "mac-walk@example.invalid"),
                                    str(vBody.get("firstName") or "Mac"), str(vBody.get("lastName") or "Walk")))
        if vPath == "authsvc/refresh":
            return self._send({"accessToken": "standin-access-refreshed", "refreshToken": "standin-refresh-refreshed",
                               "expiresAt": _iso(timedelta(hours=8))})
        if vPath == "authsvc/logout" or (vPath.startswith("authsvc/devices/") and vPath.endswith("/block")):
            return self._send({})
        return self._send(None, False, "The stand-in has no such endpoint.", 404)

    def do_DELETE(self):
        if self._path().startswith("authsvc/devices/"):
            return self._send({})
        return self._send(None, False, "The stand-in has no such endpoint.", 404)


if __name__ == "__main__":
    ThreadingHTTPServer(("127.0.0.1", PORT), Handler).serve_forever()
