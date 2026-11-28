#!/usr/bin/env bash
# Builds Chatur for the Mac (Release, Mac Catalyst, signed with the free Mac Catalyst profile of the owner's Apple ID,
# as TrStudio is) and installs it as /Applications/Chatur.app (REQ-NFR-012 on the Mac, owner's request of 27 Nov 2026).
#
#   bash scripts/install-mac.sh
#
# What it does, in order:
#  1. Refuses while Chatur runs from /Applications.
#  2. Finds the "Mac Catalyst Team Provisioning Profile: com.techierathore.chatur". When there is none, or it has ended,
#     Xcode's command-line tools make one (no Xcode window opens; this Mac must be signed in to Xcode with the owner's
#     Apple ID). The work is done under this repository's bin folder and deleted again. The profile itself is kept
#     where Xcode keeps profiles (~/Library/Developer/Xcode/UserData/Provisioning Profiles).
#  3. Restores the packages. With a key for the owner's GitHub Packages feed in this Mac's NuGet settings it uses the
#     versions the project names (TrBlazeUI 2.2.3, TechieRag 1.2.1). Without one it says so and uses TrBlazeUI 2.2.5
#     from nuget.org and TechieRag packed from its own source beside this repository (../TechieRag), in a package
#     folder of its own under bin, so the machine's shared package cache never holds a home-made TechieRag.
#  4. Takes the App Manager address and installed-app key (pk_...) from this project's user secrets
#     (~/.microsoft/usersecrets/<id>/secrets.json) into a settings file under src/Chatur/obj/install, builds with it
#     and deletes it. The key is never printed. Without a key the app signs in by its application id alone.
#  5. Builds Release for this Mac's processor, signed with the Apple Development certificate and that profile.
#  6. Replaces /Applications/Chatur.app. Your data stays where it is (~/Library/Containers is not used: Chatur is
#     not sandboxed; its database, log and sign-in are in its application-data folder and the Keychain).
#  7. Readies this repository's bin folder for the owner's test, as scripts/install-windows.ps1 does.
#
# Options: --open starts Chatur once it is installed.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$REPO/src/Chatur/Chatur.csproj"
TFM="net10.0-maccatalyst"
APP_ID="com.techierathore.chatur"
PROFILE_NAME="Mac Catalyst Team Provisioning Profile: $APP_ID"
PROFILES_DIR="$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"
INSTALL_APP="/Applications/Chatur.app"
SETTINGS_DIR="$REPO/src/Chatur/obj/install"
SETTINGS_FILE="$SETTINGS_DIR/appsettings.json"
PKG="$REPO/bin/mac-packages"
LOG_DIR="$REPO/bin/install-mac-logs"
OPEN_AFTER=0
[[ "${1:-}" == "--open" ]] && OPEN_AFTER=1

say() { printf '%s\n' "$*"; }
fail() { printf 'install-mac: %s\n' "$*" >&2; exit 1; }

[[ "$(uname -s)" == "Darwin" ]] || fail "this script builds the Mac app and runs on a Mac only."
command -v dotnet >/dev/null || fail "dotnet is not on PATH. Install the .NET 10 SDK and the maui workload."
xcode-select -p >/dev/null 2>&1 || fail "Xcode is not installed or not selected (xcode-select -p)."
mkdir -p "$LOG_DIR"

# 1. Nothing is replaced under a running copy.
if pgrep -f "$INSTALL_APP/Contents/MacOS/Chatur" >/dev/null 2>&1; then
  fail "Chatur is running from $INSTALL_APP. Quit it (Chatur ▸ Quit, or Cmd+Q), then run this again."
fi

# 2. The signing certificate and the free Mac Catalyst profile.
IDENTITY="$(security find-identity -v -p codesigning | sed -n 's/.*"\(Apple Development: [^"]*\)".*/\1/p' | head -1)"
[[ -n "$IDENTITY" ]] || fail "no Apple Development certificate in the Keychain. Sign in to Xcode with your Apple ID (Xcode ▸ Settings ▸ Accounts) once."

profile_ends() { # prints the end date (epoch seconds) of the newest profile named $PROFILE_NAME, or nothing
  local f tmp best=0 end
  tmp="$(mktemp "$LOG_DIR/profile.XXXXXX")"
  for f in "$PROFILES_DIR"/*; do
    [[ -f "$f" ]] || continue
    security cms -D -i "$f" > "$tmp" 2>/dev/null || continue
    [[ "$(/usr/libexec/PlistBuddy -c 'Print :Name' "$tmp" 2>/dev/null)" == "$PROFILE_NAME" ]] || continue
    end="$(date -j -f '%a %b %d %T %Z %Y' "$(/usr/libexec/PlistBuddy -c 'Print :ExpirationDate' "$tmp")" +%s 2>/dev/null || echo 0)"
    (( end > best )) && best=$end
  done
  rm -f "$tmp"
  (( best > 0 )) && echo "$best"
  return 0
}

ENDS="$(profile_ends)"
if [[ -z "$ENDS" || "$ENDS" -le "$(( $(date +%s) + 86400 ))" ]]; then
  say "Making the free Mac Catalyst profile for $APP_ID (Xcode's command-line tools, no window opens)…"
  WORK="$REPO/bin/mac-signing-work"
  rm -rf "$WORK"; mkdir -p "$WORK"
  cp -R "$REPO/scripts/mac-signing/Probe" "$REPO/scripts/mac-signing/Probe.xcodeproj" "$WORK/"
  if ! (cd "$WORK" && xcodebuild -project Probe.xcodeproj -scheme Probe -configuration Debug \
        -destination "platform=macOS,variant=Mac Catalyst" -derivedDataPath ./dd \
        -allowProvisioningUpdates -allowProvisioningDeviceRegistration build > build.log 2>&1); then
    tail -20 "$WORK/build.log" >&2
    fail "Xcode could not make the profile. Is this Mac signed in to Xcode with your Apple ID (Xcode ▸ Settings ▸ Accounts)?"
  fi
  rm -rf "$WORK"
  ENDS="$(profile_ends)"
  [[ -n "$ENDS" ]] || fail "the profile was not made."
fi
say "Signing with: $IDENTITY"
say "Profile: $PROFILE_NAME (ends $(date -r "$ENDS" '+%d %b %Y'))"

# 3. Packages: the owner's feed when this Mac has its key, else nuget.org plus TechieRag from its own source.
RESTORE_ARGS=()
BUILD_PROPS=()
if dotnet restore "$PROJECT" -nologo -v:q > "$LOG_DIR/restore-feed.log" 2>&1; then
  say "Packages: the owner's GitHub Packages feed (TrBlazeUI and TechieRag at the versions the project names)."
else
  grep -q "401\|NU1301\|NU1101" "$LOG_DIR/restore-feed.log" || { tail -20 "$LOG_DIR/restore-feed.log" >&2; fail "restore failed."; }
  TR_SRC="${CHATUR_TECHIERAG_SOURCE:-$REPO/../TechieRag}"
  [[ -f "$TR_SRC/src/TechieRag/TechieRag.csproj" ]] || fail "this Mac has no key for the owner's package feed, and no TechieRag source at $TR_SRC to build it from. Add the feed key (nuget.config.template in TrStudio shows how) or the TechieRag source."
  TR_VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$TR_SRC/src/TechieRag/TechieRag.csproj" | head -1)"
  TRB_VERSION="2.2.5"
  say "Packages: this Mac has no key for the owner's GitHub Packages feed. Using TrBlazeUI $TRB_VERSION from nuget.org"
  say "          and TechieRag $TR_VERSION packed from $TR_SRC (a copy under bin; that folder is not touched)."
  mkdir -p "$PKG/feed" "$PKG/src/src"
  if [[ ! -f "$PKG/feed/TechieRag.$TR_VERSION.nupkg" ]]; then
    rsync -a --delete --exclude bin --exclude obj "$TR_SRC/src/TechieRag" "$PKG/src/src/"
    cp "$TR_SRC/README.md" "$PKG/src/"
    printf '<Project />' > "$PKG/src/Directory.Build.props"
    printf '<Project />' > "$PKG/src/Directory.Build.targets"
    printf '%s\n' '<?xml version="1.0" encoding="utf-8"?>' \
      '<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' \
      > "$PKG/src/nuget.config"
    (cd "$PKG/src" && dotnet pack src/TechieRag/TechieRag.csproj -c Release -o ../feed -nologo -v:q > "$LOG_DIR/pack-techierag.log" 2>&1) \
      || { tail -20 "$LOG_DIR/pack-techierag.log" >&2; fail "packing TechieRag failed."; }
  fi
  printf '%s\n' '<?xml version="1.0" encoding="utf-8"?>' '<configuration>' '  <packageSources>' '    <clear />' \
    '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />' \
    '    <add key="chatur-mac-local" value="feed" />' '  </packageSources>' '</configuration>' > "$PKG/nuget.config"
  export NUGET_PACKAGES="$PKG/cache"
  BUILD_PROPS=("-p:ChaturTechieRagVersion=$TR_VERSION" "-p:ChaturTrBlazeUIVersion=$TRB_VERSION")
  RESTORE_ARGS=(--configfile "$PKG/nuget.config")
fi

# 4. The settings the installed build embeds: appsettings.json with the App Manager values from user secrets.
SECRETS_ID="$(sed -n 's:.*<UserSecretsId>\(.*\)</UserSecretsId>.*:\1:p' "$PROJECT" | head -1)"
SECRETS_FILE="$HOME/.microsoft/usersecrets/$SECRETS_ID/secrets.json"
mkdir -p "$SETTINGS_DIR"
cleanup() { rm -f "$SETTINGS_FILE"; }
trap cleanup EXIT
KEY_STATE="$(python3 -I - "$REPO/src/Chatur/appsettings.json" "$SECRETS_FILE" "$SETTINGS_FILE" <<'PY'
import json, os, sys
base, secrets_path, out = sys.argv[1:4]
settings = json.load(open(base, encoding="utf-8-sig"))
am = settings.setdefault("AppManager", {})
if os.path.isfile(secrets_path):
    secrets = json.load(open(secrets_path, encoding="utf-8-sig"))
    for name in ("BaseUrl", "ApplicationId", "ApiKey"):
        value = secrets.get("AppManager:" + name) or (secrets.get("AppManager") or {}).get(name)
        if value:
            am[name] = value
key = str(am.get("ApiKey") or "")
if key and not key.startswith("pk_"):
    print("bad"); sys.exit(0)
json.dump(settings, open(out, "w", encoding="utf-8"), indent=2)
print("key" if key else "nokey")
PY
)"
case "$KEY_STATE" in
  key)   say "Settings for the install written (the App Manager key is not shown)." ;;
  nokey) say "Settings for the install written. No AppManager:ApiKey in this Mac's user secrets ($SECRETS_ID): Chatur signs in by its application id alone." ;;
  bad)   fail "AppManager:ApiKey must be an installed-app key (it starts pk_). A website key must not ship inside an app." ;;
  *)     fail "could not write the settings for the install." ;;
esac

# 5. Build: Release, this Mac's processor, signed.
case "$(uname -m)" in arm64) RID="maccatalyst-arm64" ;; *) RID="maccatalyst-x64" ;; esac
say "Building Chatur (Release, $RID, signed) — a few minutes…"
# Always from clean Mac Release folders: precompiled code left from an earlier build with other settings aborts the app
# at start (27 Nov 2026). Only this build's own output is removed.
rm -rf "$REPO/src/Chatur/obj/Release/$TFM" "$REPO/src/Chatur/bin/Release/$TFM"
dotnet restore "$PROJECT" ${RESTORE_ARGS[@]+"${RESTORE_ARGS[@]}"} -p:RuntimeIdentifier="$RID" \
  ${BUILD_PROPS[@]+"${BUILD_PROPS[@]}"} -nologo -v:q > "$LOG_DIR/restore.log" 2>&1 \
  || { tail -20 "$LOG_DIR/restore.log" >&2; fail "restore failed."; }
if ! dotnet build "$PROJECT" -f "$TFM" -c Release --no-restore -p:RuntimeIdentifier="$RID" \
      -p:EnableCodeSigning=true "-p:CodesignKey=$IDENTITY" "-p:CodesignProvision=$PROFILE_NAME" \
      "-p:CodesignEntitlements=Platforms/MacCatalyst/Entitlements.Signed.plist" \
      "-p:ChaturSettingsFile=$SETTINGS_FILE" ${BUILD_PROPS[@]+"${BUILD_PROPS[@]}"} \
      -nologo -v:minimal > "$LOG_DIR/build.log" 2>&1; then
  grep -E "error" "$LOG_DIR/build.log" | sort -u | head -20 >&2
  fail "the build failed; the whole log is $LOG_DIR/build.log"
fi
cleanup
BUILT="$REPO/src/Chatur/bin/Release/$TFM/$RID/Chatur.app"
[[ -d "$BUILT" ]] || fail "the build made no Chatur.app at $BUILT"
codesign --verify --deep --strict "$BUILT" || fail "the built app's signature does not verify."

# 6. Install, replacing an earlier install in place.
rm -rf "$INSTALL_APP"
ditto "$BUILT" "$INSTALL_APP"
codesign --verify --deep --strict "$INSTALL_APP" || fail "the installed app's signature does not verify."

# 7. The folder the owner's test works in: this repository's own bin folder. Three small files keep Chatur's own build
#    settings and package feed away from the test projects made under it, and a .git file that leads nowhere makes
#    those projects read as "not a repository", so nothing done in them can reach Chatur's own repository.
TEST_ROOT="$REPO/bin"
mkdir -p "$TEST_ROOT"
printf '<Project />\n' > "$TEST_ROOT/Directory.Build.props"
printf '<Project />\n' > "$TEST_ROOT/Directory.Build.targets"
printf '%s\n' '<?xml version="1.0" encoding="utf-8"?>' \
  '<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' \
  > "$TEST_ROOT/nuget.config"
printf 'gitdir: ./no-repository-here\n' > "$TEST_ROOT/.git"

VERSION="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$INSTALL_APP/Contents/Info.plist")"
say "Installed Chatur $VERSION to $INSTALL_APP"
say "Open it from Launchpad or Spotlight (Cmd+Space, type Chatur, Return)."
say "Folder for the owner's test: $TEST_ROOT"
if (( OPEN_AFTER )); then open "$INSTALL_APP"; fi
