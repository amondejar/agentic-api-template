#!/usr/bin/env bash
# End-to-end: packs the template, installs it, creates solutions with it, builds them with warnings as errors, runs
# their tests, starts the API and drives it like an AI agent (tests/mcp-agent.mjs).
#
#   tests/run.sh
#
# Environment:
#   AGENTIC_FEED      extra local NuGet feed for the generated solutions (packages not on nuget.org yet)
#   AGENTIC_OFFLINE   1: use only AGENTIC_FEED, build the API project only (no tests, no OpenAPI variant)
set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
work="$(mktemp -d)"
server=""
cleanup() { [ -n "$server" ] && kill "$server" 2>/dev/null || true; rm -rf "$work"; }
trap cleanup EXIT
export DOTNET_CLI_HOME="$work/home" DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
offline="${AGENTIC_OFFLINE:-0}"
pass=0
ok() { echo "  ✓ $1"; pass=$((pass + 1)); }
fail() { echo "  ✗ $1"; [ -f "$work/out.txt" ] && tail -40 "$work/out.txt"; exit 1; }

echo "Pack and install the template"
dotnet pack "$repo/src/AgenticApi.Templates" -c Release -o "$work/pkg" > "$work/out.txt" 2>&1 || fail "pack"
dotnet new install "$work/pkg"/AgenticApi.Templates.*.nupkg > "$work/out.txt" 2>&1 || fail "install"
ok "template installed"

nuget_config() {
  local dir="$1"
  {
    echo '<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear />'
    [ "$offline" = "1" ] || echo '<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />'
    [ -n "${AGENTIC_FEED:-}" ] && echo "<add key=\"local\" value=\"$AGENTIC_FEED\" />"
    echo '</packageSources></configuration>'
  } > "$dir/nuget.config"
}

create() {
  local name="$1"; shift
  (cd "$work" && dotnet new agentic-api -n "$name" --no-restore "$@" > "$work/out.txt" 2>&1) || fail "dotnet new agentic-api -n $name $*"
  nuget_config "$work/$name"
}

echo "Generated solution"
if [ "$offline" = "1" ]; then create Shop --openapi false --port 5180; else create Shop --port 5180; fi
sol="$work/Shop"
[ -f "$sol/Shop.slnx" ] && [ -f "$sol/src/Shop/Shop.csproj" ] && [ -f "$sol/tests/Shop.Tests/Shop.Tests.csproj" ] && ok "solution, API and tests renamed" || fail "renaming"
grep -q "namespace Shop.Features.Loans" "$sol/src/Shop/Features/Loans/Loans.cs" && ok "namespaces renamed" || fail "namespaces"
grep -q "localhost:5180/mcp" "$sol/.mcp.json" && grep -q "localhost:5180" "$sol/src/Shop/Properties/launchSettings.json" && grep -q "localhost:5180/mcp" "$sol/README.md" && ok "--port reaches .mcp.json, launchSettings and README" || fail "port"
for f in AGENTS.md .github/copilot-instructions.md .github/workflows/copilot-setup-steps.yml .github/workflows/ci.yml .vscode/mcp.json .config/dotnet-tools.json specs/loans.spec.md spectrace.json .globalconfig; do
  [ -f "$sol/$f" ] || fail "missing $f"
done
ok "agent files, CI, specs and tool manifest"
if grep -rq "AgenticApi" "$sol" --include=*.cs --include=*.csproj --include=*.razor --include=*.json --include=*.md --include=*.slnx; then fail "leftover AgenticApi"; fi
ok "no leftover template names"

echo "Build"
target="$sol"; [ "$offline" = "1" ] && target="$sol/src/Shop"
(cd "$sol" && dotnet build "$target" -warnaserror > "$work/out.txt" 2>&1) || fail "build with warnings as errors"
ok "builds with no warnings (SpecTrace traceability complete, SkillLock clean)"

if [ "$offline" != "1" ]; then
  (cd "$sol" && dotnet test --no-build > "$work/out.txt" 2>&1) || fail "tests"
  ok "tests pass: $(grep -oE 'Passed: *[0-9]+' "$work/out.txt" | head -1)"

  echo "Variant without OpenAPI"
  create Lean --openapi false
  (cd "$work/Lean" && dotnet build -warnaserror > "$work/out.txt" 2>&1) || fail "build --openapi false"
  grep -q "Scalar" "$work/Lean/src/Lean/Lean.csproj" "$work/Lean/src/Lean/Program.cs" && fail "OpenAPI left in --openapi false" || ok "--openapi false builds without Scalar"
fi

echo "Traceability: an approved requirement without code is reported"
sed -i 's/^Status: proposed$/Status: approved/' "$sol/specs/loans.spec.md"
if (cd "$sol" && dotnet build "$target" -warnaserror > "$work/out.txt" 2>&1); then fail "approving LN-004 should break the build"; fi
grep -q "ST0003" "$work/out.txt" && grep -q "LN-004" "$work/out.txt" && ok "approving LN-004 makes the build ask for its implementation" || fail "ST0003 for LN-004"
sed -i '/^## LN-004/,/^Status:/ s/^Status: approved$/Status: proposed/' "$sol/specs/loans.spec.md"

echo "Run it as an agent would"
if curl -s -o /dev/null http://localhost:5180; then fail "port 5180 is already in use"; fi
dll="$(ls "$sol"/src/Shop/bin/*/net10.0/Shop.dll | head -1)"
# exec: $server is the API itself, so the trap stops it (dotnet run would leave its child running).
(cd "$sol/src/Shop" && ASPNETCORE_ENVIRONMENT=Development exec dotnet "$dll" --urls http://localhost:5180 > "$work/server.txt" 2>&1) &
server=$!
for _ in $(seq 1 60); do curl -sf -o /dev/null http://localhost:5180/api/loans && break; sleep 1; done
curl -sf -o /dev/null http://localhost:5180/api/loans || { cat "$work/server.txt"; fail "the API did not start"; }
node "$repo/tests/mcp-agent.mjs" http://localhost:5180 || { tail -30 "$work/server.txt"; fail "agent checks"; }

echo
echo "All $pass template checks passed (plus the agent checks above)."
