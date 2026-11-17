#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
#
# Build WineOverlayHide.dll with the .NET Framework csc.exe that already lives
# in the Wine prefix where HDT is installed, compiling against that HDT.
#
#   WINEPREFIX=~/Games/battlenet ./build.sh [--install]
#
# Proton/umu prefixes: also set PROTONPATH (and optionally GAMEID) and the
# build runs through umu-run, joining the running wineserver if there is one.
# --install copies the DLL into HDT's Plugins folder.
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PREFIX="${WINEPREFIX:?set WINEPREFIX to the Wine prefix that contains HDT}"
PREFIX="${PREFIX%/}"

# Proton keeps drive_c under pfx/ in some layouts.
[ -d "$PREFIX/drive_c" ] || [ ! -d "$PREFIX/pfx/drive_c" ] || PREFIX="$PREFIX/pfx"
[ -d "$PREFIX/drive_c" ] || { echo "no drive_c in $PREFIX" >&2; exit 1; }

# Find the Windows user that has HDT installed.
USERNAME_WIN=""
for d in "$PREFIX"/drive_c/users/*/; do
    if [ -f "$d/AppData/Local/HearthstoneDeckTracker/current/HearthstoneDeckTracker.exe" ]; then
        USERNAME_WIN="$(basename "$d")"
        break
    fi
done
[ -n "$USERNAME_WIN" ] || { echo "HDT not found under $PREFIX/drive_c/users/*/AppData/Local" >&2; exit 1; }

USERDIR="$PREFIX/drive_c/users/$USERNAME_WIN"
BUILD="$USERDIR/wineoverlayhide-build"
PLUGINS="$USERDIR/AppData/Roaming/HearthstoneDeckTracker/Plugins"
WUSER="C:\\users\\$USERNAME_WIN"
WBUILD="$WUSER\\wineoverlayhide-build"
FW='C:\windows\Microsoft.NET\Framework64\v4.0.30319'
HDT="$WUSER\\AppData\\Local\\HearthstoneDeckTracker\\current\\HearthstoneDeckTracker.exe"

[ -f "$PREFIX/drive_c/windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe" ] \
    || { echo "no .NET Framework 4 csc.exe in the prefix" >&2; exit 1; }

if [ -n "${PROTONPATH:-}" ]; then
    run() { env PROTON_VERB=run UMU_CONTAINER_NSENTER=1 GAMEID="${GAMEID:-umu-default}" \
                WINEPREFIX="$WINEPREFIX" PROTONPATH="$PROTONPATH" umu-run "$@"; }
else
    run() { env WINEPREFIX="$PREFIX" WINEDEBUG=-all "${WINE:-wine}" "$@"; }
fi

mkdir -p "$BUILD"
cp "$HERE/WineOverlayHide.cs" "$BUILD/"
rm -f "$BUILD/WineOverlayHide.dll"

run "$FW\\csc.exe" /nologo /target:library /optimize+ \
    "/out:$WBUILD\\WineOverlayHide.dll" \
    "/r:$HDT" \
    "/r:$FW\\WPF\\PresentationFramework.dll" \
    "/r:$FW\\WPF\\PresentationCore.dll" \
    "/r:$FW\\WPF\\WindowsBase.dll" \
    "/r:$FW\\System.Xaml.dll" \
    "/r:$FW\\netstandard.dll" \
    "$WBUILD\\WineOverlayHide.cs" 2>&1 \
    | grep -vE 'ProtonFixes|^(INFO|DEBUG|WARN)' || true

[ -f "$BUILD/WineOverlayHide.dll" ] || { echo "build failed" >&2; exit 1; }
cp "$BUILD/WineOverlayHide.dll" "$HERE/"
rm -rf "$BUILD"
echo "built $HERE/WineOverlayHide.dll"

if [ "${1:-}" = "--install" ]; then
    mkdir -p "$PLUGINS"
    cp "$HERE/WineOverlayHide.dll" "$PLUGINS/"
    echo "installed to $PLUGINS"
fi
