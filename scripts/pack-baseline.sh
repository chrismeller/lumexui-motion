#!/usr/bin/env bash
# Packs the package-validation baseline: LumexUI.Motion as released at the tag v<version>, built from that tag's
# source. src/LumexUI.Motion.csproj compares every pack against it (PackageValidationBaselinePath), so a change
# that breaks the public API of the last release fails `dotnet pack`.
#
# The baseline is packed from the tag instead of downloaded, because GitHub Packages needs a token even to read.
# The API surface comes from the source, so a rebuild of the tag has the same API as the published package.
#
# Usage: scripts/pack-baseline.sh [version]
#   The version defaults to MotionBaselineVersion in src/LumexUI.Motion.csproj, the one the pack compares against.
# Writes artifacts/baseline/LumexUI.Motion.<version>.nupkg (artifacts/ is gitignored).
set -euo pipefail

if [[ $# -gt 1 ]]; then
    echo "usage: $0 [baseline version, e.g. 0.2.0]" >&2
    exit 2
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
version="${1:-$(dotnet msbuild "$repo_root/src/LumexUI.Motion.csproj" -getProperty:MotionBaselineVersion)}"
if [[ -z "$version" ]]; then
    echo "error: src/LumexUI.Motion.csproj doesn't set MotionBaselineVersion" >&2
    exit 2
fi
tag="v$version"
out="$repo_root/artifacts/baseline"
package="$out/LumexUI.Motion.$version.nupkg"

if [[ -f "$package" ]]; then
    echo "$package already exists"
    exit 0
fi

# CI checks out a single commit with no tags, so fetch the tag if it isn't here.
if ! git -C "$repo_root" rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
    git -C "$repo_root" fetch --depth 1 origin "refs/tags/$tag:refs/tags/$tag"
fi

work="$(mktemp -d)"
trap 'git -C "$repo_root" worktree remove --force "$work/src" >/dev/null 2>&1 || true; rm -rf "$work"' EXIT

git -C "$repo_root" worktree add --detach "$work/src" "$tag" >/dev/null

# The tag's own csproj bundles motion.js with pnpm, as the release did. -p:EnablePackageValidation=false keeps a
# later tag from trying to validate against a baseline of its own.
(
    cd "$work/src"
    pnpm install --frozen-lockfile
    dotnet pack src/LumexUI.Motion.csproj -c Release --output "$work/out" -p:EnablePackageValidation=false
)

mkdir -p "$out"
cp "$work/out/LumexUI.Motion.$version.nupkg" "$package"
echo "Packed the $tag baseline: $package"
