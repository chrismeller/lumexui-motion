#!/usr/bin/env bash
# Checks a packed LumexUI.Motion .nupkg before it's published:
#   - staticwebassets/motion.js is there, isn't empty and exports the interop functions. The bundle is generated
#     by the build (pnpm build) and isn't committed, so a broken bundle step would otherwise ship a package whose
#     components fail to import their JS module.
#   - lib/<tfm>/LumexUI.Motion.dll is there for every target framework in src/LumexUI.Motion.csproj.
#
# Usage: scripts/check-package.sh path/to/LumexUI.Motion.<version>.nupkg
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "usage: $0 <path to LumexUI.Motion .nupkg>" >&2
    exit 2
fi

package="$1"
if [[ ! -f "$package" ]]; then
    echo "error: $package doesn't exist" >&2
    exit 2
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0
fail() {
    echo "error: $package: $1" >&2
    failed=1
}

entries="$(unzip -Z1 "$package")"

bundle="staticwebassets/motion.js"
if ! grep -qxF "$bundle" <<<"$entries"; then
    fail "$bundle is missing"
else
    bundle_js="$(unzip -p "$package" "$bundle")"
    if [[ -z "$bundle_js" ]]; then
        fail "$bundle is empty"
    else
        # Minification keeps property names, so the functions MotionJsInterop calls appear by name.
        for fn in animateEnter animateExit animateLayoutId; do
            grep -qF "$fn" <<<"$bundle_js" || fail "$bundle doesn't contain $fn"
        done
    fi
fi

frameworks="$(dotnet msbuild "$repo_root/src/LumexUI.Motion.csproj" -getProperty:TargetFrameworks)"
IFS=';' read -ra tfms <<<"$frameworks"
if [[ ${#tfms[@]} -eq 0 ]]; then
    fail "couldn't read TargetFrameworks from src/LumexUI.Motion.csproj"
fi
for tfm in "${tfms[@]}"; do
    grep -qxF "lib/$tfm/LumexUI.Motion.dll" <<<"$entries" || fail "lib/$tfm/LumexUI.Motion.dll is missing"
done

if [[ $failed -ne 0 ]]; then
    exit 1
fi
echo "$package: OK ($bundle and lib/ for ${frameworks//;/, })"
