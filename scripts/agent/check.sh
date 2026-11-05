#!/bin/bash
# Compiles the tests, compiles the plugin to a temporary folder and runs the tests. The game does not reload the DLL.
# Prints only errors and failures. A test project that does not compile would run its old binary: its errors come first, read them.
root="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"; cd "$root" || exit 1
source "$root/scripts/agent/env.sh"
"$DOTNET" build tests/TenshoAssistant.Tests -c Release --nologo 2>&1 | grep -E " error |échou|failed" | sed -E 's/\[C:.*//;s/.*(tests|src).TenshoAssistant[^\]*.//' | cut -c1-210 | sort -u | head -10
"$DOTNET" build src/TenshoAssistant -c Release --nologo -o "$AGENT_TEMP/chk" 2>&1 | grep -E " error |échou|failed" | sed -E 's/\[C:.*//;s/.*TenshoAssistant.//' | cut -c1-210 | sort -u | head -10
rm -rf "$AGENT_TEMP/chk"
"$DOTNET" run --project tests/TenshoAssistant.Tests -c Release --no-build > "$AGENT_TEMP/tests.txt" 2>&1
echo "tests exit $?"
grep -E "^FAIL" "$AGENT_TEMP/tests.txt" | cut -c1-280 | head -8
tail -1 "$AGENT_TEMP/tests.txt" | cut -c1-40
