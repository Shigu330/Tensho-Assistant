#!/bin/bash
# Full build (the game reloads the DLL if it runs), tests, counts filled in CHANGELOG and STATUS, secret check, commit and push.
# Stops at the first problem: nothing is committed then. Run check.sh first.
root="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"; cd "$root" || exit 1
source "$root/scripts/agent/env.sh"
"$DOTNET" build TenshoAssistant.sln -c Release --nologo 2>&1 | grep -E " error |Avertissement|Warning\(s\)|échou" | sed -E 's/\[C:.*//' | cut -c1-200 | sort -u | head -5
"$DOTNET" run --project tests/TenshoAssistant.Tests -c Release --no-build > "$AGENT_TEMP/tests.txt" 2>&1
code=$?; echo "tests exit $code"
grep -E "^FAIL" "$AGENT_TEMP/tests.txt" | cut -c1-260 | head -5
count=$(tail -1 "$AGENT_TEMP/tests.txt" | cut -d' ' -f1); echo "$count"
if [ "$code" != "0" ]; then echo "STOP: the tests failed, nothing is committed."; exit 1; fi
python -I -X utf8 - "$count" <<'PY' || { echo "STOP: counts not filled, nothing is committed."; exit 1; }
import sys,re
count=sys.argv[1];passed,total=count.split('/');assert passed==total,count
def edit(path,change):
    text=open(path,encoding='utf-8',newline='').read();open(path,'w',encoding='utf-8',newline='').write(change(text))
def changelog(text):
    return text.replace('VERIFYCOUNT',count,1)
edit('CHANGELOG.md',changelog)
def status(text):
    result,changed=re.subn(r'Verify\.ps1 \(agent\) \d+/\d+','Verify.ps1 (agent) '+count,text,count=1);assert changed==1
    return result
edit('docs/ai/STATUS.md',status)
PY
secrets=$(git grep -n -I -E "discord(app)?\.com/api/webhooks/[0-9]+/" -- . ":!tests" | wc -l)
if [ "$secrets" != "0" ]; then echo "STOP: a Discord address is in the tracked files, nothing is committed."; exit 1; fi
log="$APPDATA/XIVLauncher/dalamud.log"
[ -f "$log" ] && grep -a -E "^$(date +%Y-%m-%d).*\[(ERR|FTL)\].*Tensho" "$log" | tail -2 | cut -c1-180
pwsh.exe -NoProfile -File "$root/scripts/agent/commit.ps1" 2>&1 | tail -3 | cut -c1-120
