#!/usr/bin/env bash
# Syntax-only C# check (no Unity/Xamarin assemblies needed): builds a throw-away project that
# compiles the given directories and fails only on SYNTAX diagnostics (CS1xxx / CS8xxx parse errors).
# Semantic errors (missing UnityEngine/Android types, CS0xxx) are expected and ignored by design.
# Usage: scripts/check_csharp_syntax.sh DIR [DIR...]
set -euo pipefail
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null 2>&1 || { echo "check_csharp_syntax: dotnet SDK required" >&2; exit 2; }
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
root="$PWD"
tmp="$(mktemp -d)"; trap 'rm -rf "$tmp"' EXIT
{
  echo '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net9.0</TargetFramework><LangVersion>latest</LangVersion>'
  echo '<EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputType>Library</OutputType><TreatWarningsAsErrors>false</TreatWarningsAsErrors><NoWarn>*</NoWarn></PropertyGroup><ItemGroup>'
  for d in "$@"; do echo "<Compile Include=\"$root/$d/**/*.cs\" />"; done
  echo '</ItemGroup></Project>'
} > "$tmp/Syntax.csproj"
n="$(find "$@" -name '*.cs' | wc -l)"
out="$(dotnet build "$tmp/Syntax.csproj" -nologo -v q -p:UseSharedCompilation=false 2>&1 || true)"
# CS1xxx = syntax/lexical errors, CS8xxx = newer-syntax errors; CS0xxx (semantic) are ignored.
bad="$(printf '%s\n' "$out" | grep -E 'error CS(1[0-9]{3}|8[0-9]{3})' | grep -vE 'error CS(1[0-9]{3}|8[0-9]{3}).*(UnityEngine|Android|Xamarin)' | sort -u || true)"
# drop semantic-ish CS1xxx/CS8xxx codes that only appear because assemblies are missing
bad="$(printf '%s\n' "$bad" | grep -vE 'error CS(1061|1503|1929|1579|1955|1501|1503|1594|1612|1620|1729|1739|1744|8120|8130|8129|8139|8124|8200|8400|8401)' || true)"
if [[ -n "$bad" ]]; then echo "csharp-syntax: FAILED"; printf '%s\n' "$bad" | head -40; exit 1; fi
echo "csharp-syntax: OK ($n .cs files parsed; semantic errors from missing engine assemblies ignored)"
