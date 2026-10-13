#!/usr/bin/env sh
set -eu
cd "$(dirname "$0")"
section=${1-all}
if command -v pwsh >/dev/null 2>&1; then
    exec pwsh -NoLogo -NoProfile -File ./packaging/Invoke-Build.ps1 -Section "$section" -Configuration Debug
fi
case "$section" in
    all|clean|restore|build|test|pack|validate) ;;
    *) printf 'Unknown build section: %s\n' "$section" >&2; exit 2 ;;
esac
if [ "$section" = all ] || [ "$section" = clean ]; then dotnet clean Icod.Dedit.sln -c Debug; fi
if [ "$section" = all ] || [ "$section" = restore ]; then dotnet restore Icod.Dedit.sln; fi
if [ "$section" = all ] || [ "$section" = build ]; then dotnet build Icod.Dedit.sln -c Debug --no-restore; fi
if [ "$section" = all ] || [ "$section" = test ]; then dotnet test Icod.Dedit.sln -c Debug --no-build --no-restore; fi
if [ "$section" = all ] || [ "$section" = pack ]; then
    mkdir -p artifacts
    dotnet pack Icod.Dedit.sln -c Debug --no-build --no-restore -o artifacts
fi
if [ "$section" = all ] || [ "$section" = validate ]; then
    version=$(dotnet msbuild Icod.Dedit.csproj -nologo -getProperty:PackageVersion)
    package="artifacts/Icod.Dedit.$version.nupkg"
    test -f "$package" || { printf 'Missing tool package: %s\n' "$package" >&2; exit 1; }
    set -- artifacts/*.nupkg
    test "$#" -eq 1 && test "$1" = "$package" || { printf 'Expected exactly one dedit tool package\n' >&2; exit 1; }
    tool_dir=$(mktemp -d)
    trap 'rm -rf "$tool_dir"' EXIT HUP INT TERM
    package_source=$(cd artifacts && pwd)
    escaped_source=$(printf '%s' "$package_source" | sed -e 's/&/\&amp;/g' -e 's/</\&lt;/g' -e 's/"/\&quot;/g')
    printf '<configuration><packageSources><clear/><add key="local" value="%s"/></packageSources></configuration>\n' "$escaped_source" > "$tool_dir/NuGet.Config"
    dotnet tool install Icod.Dedit --tool-path "$tool_dir/tool" --version "$version" --configfile "$tool_dir/NuGet.Config"
    actual=$("$tool_dir/tool/dedit" --version)
    test "$actual" = "dedit $version" || { printf 'Unexpected tool version: %s\n' "$actual" >&2; exit 1; }
    "$tool_dir/tool/dedit" --help
fi
