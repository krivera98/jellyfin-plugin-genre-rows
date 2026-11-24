#!/usr/bin/env bash
# Builds the plugin, zips it into releases/, and adds the version to manifest.json
# so Jellyfin can install it from Dashboard → Plugins → Repositories.
#
# Usage: ./build-release.sh 1.0.0.0 "What changed in this version"
#
# REPO_BASE_URL is where this repository's files can be downloaded from on the internet.
# It must be a public URL: Jellyfin downloads the manifest and zip without logging in.
set -euo pipefail

VERSION="${1:?Usage: $0 <version, e.g. 1.0.0.0> [changelog]}"
CHANGELOG="${2:-}"
TARGET_ABI="12.4.0.0" # oldest Jellyfin version this build runs on
REPO_BASE_URL="${REPO_BASE_URL:-https://raw.githubusercontent.com/krivera98/jellyfin-plugin-genre-rows/main}"

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$HERE/Jellyfin.Plugin.GenreRows"
RELEASES_DIR="$HERE/releases"
MANIFEST="$HERE/manifest.json"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT

dotnet publish "$PROJECT" -c Release -o "$OUT/publish" -p:AssemblyVersion="$VERSION" -p:FileVersion="$VERSION" >/dev/null

mkdir -p "$RELEASES_DIR"
ZIP_NAME="genre-rows_${VERSION}.zip"
rm -f "$RELEASES_DIR/$ZIP_NAME"
(cd "$OUT/publish" && zip -q -X "$RELEASES_DIR/$ZIP_NAME" Jellyfin.Plugin.GenreRows.dll)
CHECKSUM="$(md5sum "$RELEASES_DIR/$ZIP_NAME" | cut -d' ' -f1)"

python3 - "$MANIFEST" "$VERSION" "$CHANGELOG" "$TARGET_ABI" "$REPO_BASE_URL/releases/$ZIP_NAME" "$CHECKSUM" <<'EOF'
import json, os, sys
from datetime import datetime, timezone

path, version, changelog, abi, url, checksum = sys.argv[1:]
manifest = json.load(open(path)) if os.path.exists(path) else []
plugin = next((p for p in manifest if p["guid"] == "620eedb4-ad26-47d7-a878-f42a41da0db2"), None)
if plugin is None:
    plugin = {"guid": "620eedb4-ad26-47d7-a878-f42a41da0db2", "versions": []}
    manifest.append(plugin)

plugin.update({
    "name": "Genre Rows",
    "description": "Adds a Home Screen Sections row for each genre of movies and shows.",
    "overview": "Rows like \"Comedy Movies\" and \"Drama Shows\" on the home screen. Requires the Home Screen Sections plugin.",
    "owner": "krivera98",
    "category": "General",
})
plugin["versions"] = [v for v in plugin["versions"] if v["version"] != version]
plugin["versions"].insert(0, {
    "version": version,
    "changelog": changelog,
    "targetAbi": abi,
    "sourceUrl": url,
    "checksum": checksum,
    "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
})
plugin["versions"].sort(key=lambda v: tuple(int(x) for x in v["version"].split(".")), reverse=True)
json.dump(manifest, open(path, "w"), indent=2)
open(path, "a").write("\n")
EOF

echo "Built $ZIP_NAME (md5 $CHECKSUM) and updated manifest.json"
