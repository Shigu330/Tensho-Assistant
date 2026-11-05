# new-version.py <file.json>: the next x.Y.0 in the csproj, CHANGELOG (with VERIFYCOUNT for publish-version.sh), TESTING,
# STATUS (version and resume point) and, for a proposal, its state line and its mark in the plan. Run it from the repository's root.
# {"id":"P2","doc":"MAP","fr":"...","en":"...","test":"...","tests":"Name","next":"phase 3, C5"}; without "doc": a request outside the proposals.
import re,sys,json
def load(path):
    text=open(path,encoding='utf-8',newline='').read();crlf='\r\n' in text;return text.replace('\r\n','\n'),crlf
def store(path,text,crlf):open(path,'w',encoding='utf-8',newline='').write(text.replace('\n','\r\n') if crlf else text)
entry=json.load(open(sys.argv[1],encoding='utf-8'))
path='src/TenshoAssistant/TenshoAssistant.csproj';text,crlf=load(path)
found=re.search(r'<Version>0\.(\d+)\.0</Version>',text);old=int(found.group(1));new=old+1
store(path,text.replace(found.group(0),f'<Version>0.{new}.0</Version>',1),crlf)
path='docs/ai/STATUS.md';text,crlf=load(path)
assert f'VERSION: 0.{old}.0.0' in text
text=text.replace(f'VERSION: 0.{old}.0.0',f'VERSION: 0.{new}.0.0',1)
text,count=re.subn(r'Reprise : [^.\n]*\.',f"Reprise : {entry['next']}.",text,count=1);assert count==1
store(path,text,crlf)
path='CHANGELOG.md';text,crlf=load(path);head=f'## 0.{old}.0\n';assert head in text
store(path,text.replace(head,f"## 0.{new}.0\n- Plan global, {entry['id']} : {entry['fr']}\n- Tests : {entry['tests']}. Verify.ps1 (agent) : VERIFYCOUNT, 0 avertissement, 0 erreur ; DLL 0.{new}.0.0. Non validé en jeu.\n### English\n- {entry['en']}\n\n"+head,1),crlf)
path='docs/project/TESTING.md';text,crlf=load(path);head='**0.535 : interfaces** — à voir en jouant\n';assert head in text
store(path,text.replace(head,f"- [ ] (0.{new}) {entry['test']}\n\n"+head,1),crlf)
if entry.get('doc'):
    path=f"docs/project/{entry['doc']}-PROPOSALS.md";text,crlf=load(path)
    text,count=re.subn(rf"(^## {entry['id']} [^\n]*\n\n)",lambda match:match.group(1)+f"- **État (relu le {entry.get('date','2026-11-05')})** : FAIT 0.{new}.\n",text,count=1,flags=re.M);assert count==1
    store(path,text,crlf)
    path='docs/project/GLOBAL-PLAN.md';text,crlf=load(path)
    text,count=re.subn(rf"^\| {entry['id']} \| ([^|]*) \|",lambda match:f"| {entry['id']} | ~~{match.group(1)}~~ fait en 0.{new} |",text,count=1,flags=re.M);assert count==1
    store(path,text,crlf)
print(f'0.{new}.0')
