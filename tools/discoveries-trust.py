#!/usr/bin/env python3
r"""X-805 (maintainer's decision of 2026-11-22): the laptop worker merges by itself the pull request (travailleur/discoveries-<issue>) of a
trusted account's discoveries, only when every check below passes. Any doubt leaves the pull request open, commented once with the reason,
for the maintainer's word (scripts of the maintainer); an account not on the list changes nothing.

Usage:
  python -I -X utf8 tools/discoveries-trust.py init [--list PATH]
      Creates the list of trusted accounts holding the repository owner's account, when it does not exist (never written over).
  python -I -X utf8 tools/discoveries-trust.py merge <pull request> [--list PATH] [--dry-run]
      Exit 0: merged (--dry-run: would be); 1: not a trusted account's share, or no list: nothing done; 2: usage, or a list that does not
      read; 3: a doubt: not merged, the pull request commented once with the reason (« TELL THE MAINTAINER » printed); 4: its CI still
      runs: nothing done, run again later.
  python -I -X utf8 tools/discoveries-trust.py --selftest
      The decision on made-up pull requests, tickets and shares (no network).

The list: a text file, one GitHub login per line (letter case ignored); « # » starts a comment. Default: ~/.tensho/trusted-accounts.txt (on
Windows %USERPROFILE%\.tensho\trusted-accounts.txt), never inside a checkout of the repository (refused there: nothing of the repository
can add a name). Kept by the maintainer alone; this tool writes it only at init, with the repository owner's account.

Checks, all needed for a merge:
  - the pull request: open, not a draft, base Devloppement, branch travailleur/discoveries-<issue>, no conflict; its files (every page of the
    list, counted against GitHub's count) are players/<issue>.jsonl.gz (and the <issue>.json of before, removed) and v3/ files only, under
    community-discoveries/; its « Build and test » succeeded and no finished check failed (still running: exit 4);
  - the ticket <issue>: open, a share (discoveries-merge.py is_share), one gist link, CC0-1.0 ticked, a title naming its author; its author
    (the ticket's, never the pull request's) on the list; the gist owned by that author; no newer open share of the same author;
  - the gist as it is now (read like sync reads it): readable, at most 5 % of its facts refused, known kinds only, at most PLAUSIBLE_FACTS facts;
  - the player file of the pull request: that issue and that login, the gist and revision just read noted as merged, a fact file the merge
    tool reads (fields of a player file only: nothing of the player), every fact of the gist and of the file on Devloppement kept, no fact
    from anywhere else, and something new.
A merge squashes on the checked head commit (--match-head-commit), its commit says « merged automatically » (the maintainer reads each one),
then the ticket is commented in English then French (discoveries-merge.py notice <issue> merged <version> <facts>).
"""
import base64,importlib.util,json,os,re,subprocess,sys,tempfile

ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPOSITORY="Shigu330/Tensho-Assistant";BASE="Devloppement";REQUIRED="Build and test";OWNER=REPOSITORY.split("/")[0]
DEFAULT_LIST=os.path.join(os.path.expanduser("~"),".tensho","trusted-accounts.txt")
PLAUSIBLE_FACTS=200_000   # a share past this many facts is looked at by the maintainer
TIMEOUT=60
LOGIN=re.compile(r"[A-Za-z0-9][A-Za-z0-9-]{0,38}")
BRANCH=re.compile(r"travailleur/discoveries-(\d+)(?:-[a-z0-9]+)*")   # the worker may add a suffix (travailleur/discoveries-43-v3)
AUTOMATIC="merged automatically"   # in the merge commit's title: what the maintainer looks for at each return
MARK="Not merged automatically"
REASONS={
    "pr-state":("the pull request is not open, or is a draft","la demande de fusion n'est pas ouverte, ou est un brouillon"),
    "pr-base":("the pull request is not based on Devloppement","la demande de fusion n'est pas basée sur Devloppement"),
    "pr-conflict":("GitHub sees a conflict, or has not computed it yet","GitHub voit un conflit, ou ne l'a pas encore calculé"),
    "pr-files":("files other than the ticket's player file and the common set, or a list of files not read whole","des fichiers autres que le fichier du joueur et l'ensemble commun, ou une liste de fichiers pas lue en entier"),
    "ticket":("the ticket is closed, not a share, or not readable as one (gist link, CC0-1.0, title)","le ticket est fermé, n'est pas un partage, ou ne se lit pas comme tel (lien du gist, CC0-1.0, titre)"),
    "owner":("the gist is not owned by the ticket's author","le gist n'appartient pas à l'auteur du ticket"),
    "newer":("the same author has a newer open share","le même auteur a un partage ouvert plus récent"),
    "gist":("the gist could not be read","le gist n'a pas pu être lu"),
    "refused":("over 5 % of the gist's facts are refused","plus de 5 % des faits du gist sont refusés"),
    "kinds":("the gist holds a kind of fact the merge tool does not know","le gist contient une sorte de fait que l'outil de fusion ne connaît pas"),
    "volume":("the gist holds no fact, or more than a share is expected to hold","le gist ne contient aucun fait, ou plus qu'un partage n'en contient d'habitude"),
    "player-file":("the pull request's player file does not read, or is not this ticket's, this login's, or this gist revision's","le fichier du joueur de la demande ne se lit pas, ou n'est pas celui de ce ticket, de cet identifiant ou de cette révision du gist"),
    "facts":("the pull request's player file loses facts, holds facts from elsewhere, or brings nothing new","le fichier du joueur de la demande perd des faits, en contient venus d'ailleurs, ou n'apporte rien de neuf"),
    "ci":("a check of the pull request failed","une vérification de la demande de fusion a échoué"),
}

_tool=None
def tool():
    """discoveries-merge.py, loaded once (its rules: is_share, triage, owner_doubt, KINDS, the gist and player file readers)."""
    global _tool
    if _tool is None:
        spec=importlib.util.spec_from_file_location("discoveries_merge",os.path.join(ROOT,"tools","discoveries-merge.py"))
        _tool=importlib.util.module_from_spec(spec);spec.loader.exec_module(_tool)
    return _tool

# ---------- the list ----------
def inside(path,root=ROOT):
    real=os.path.normcase(os.path.realpath(path));top=os.path.normcase(os.path.realpath(root))
    return real==top or real.startswith(top.rstrip(os.sep)+os.sep)

def parse_list(text):
    """The logins of a list's text, lower case; ValueError on a line that is not a GitHub login."""
    names=set()
    for line in text.splitlines():
        line=line.split("#",1)[0].strip()
        if not line:continue
        if not LOGIN.fullmatch(line):raise ValueError(f"not a GitHub login: {line[:40]!r}")
        names.add(line.lower())
    return names

def read_list(path):
    """The trusted logins; None when there is no list; ValueError when it is inside the repository or holds a line that is not a login."""
    if inside(path):raise ValueError(f"{path} is inside the repository: the list stays outside it")
    if not os.path.isfile(path):return None
    with open(path,encoding="utf-8") as stream:return parse_list(stream.read(64*1024))

# ---------- the decision (pure) ----------
def allowed(path,issue):
    """X-805: a file the worker's pull request may change: this ticket's player file (and its file of before) and the common set."""
    return path in (f"community-discoveries/players/{issue}.jsonl.gz",f"community-discoveries/players/{issue}.json") \
        or bool(re.fullmatch(r"community-discoveries/v3/(index\.json|kind-\d{2}\.jsonl\.gz)",path))

def decide(trusted,pr,ticket,share):
    """Pure. trusted: lower-case logins. pr: {"head","base","state","draft","mergeable","files":[(path, previous)],"changed","checks":[(name, status,
    conclusion)]}. ticket: {"number","state","issue": the gh issue (title, body, author, labels), "newer": a newer open share of the same author}.
    share: {"read": bool, "owner", "gist", "revision", "facts", "refused", "kinds": [kind], "gist_keys": set, "file": {"Issue","Login","Noted",
    "keys"} or None, "base_keys": set}. → (verdict, reason): "merge", "skip" (not a trusted account's share: nothing done), "wait" (CI running),
    "doubt" (a check failed: the reason's code in REASONS)."""
    m=tool()
    head=BRANCH.fullmatch(pr.get("head") or "")
    if not head:return "skip","not a worker pull request of discoveries"
    number=int(head.group(1))
    issue=ticket.get("issue") or {}
    author=m.login(issue)
    if ticket.get("number")!=number or not author or author.lower() not in trusted:return "skip","the ticket's author is not on the list of trusted accounts"
    if pr.get("state")!="OPEN" or pr.get("draft"):return "doubt","pr-state"
    if pr.get("base")!=BASE:return "doubt","pr-base"
    if pr.get("mergeable")!="MERGEABLE":return "doubt","pr-conflict"
    files=pr.get("files") or []
    paths=[p for row in files for p in row if p]
    if len(files)!=pr.get("changed") or not paths or not all(allowed(p,number) for p in paths) \
        or f"community-discoveries/players/{number}.jsonl.gz" not in paths:return "doubt","pr-files"
    if ticket.get("state")!="OPEN" or not m.is_share(issue):return "doubt","ticket"
    gist,doubt=m.triage(issue)
    if doubt:return "doubt","ticket"
    if ticket.get("newer"):return "doubt","newer"
    if not share.get("read") or share.get("gist")!=gist:return "doubt","gist"
    if m.owner_doubt(share.get("owner") or "",issue):return "doubt","owner"
    facts,refused=share.get("facts",0),share.get("refused",0)
    if (facts+refused)==0 or refused*20>facts+refused:return "doubt","refused" if refused else "volume"
    if not set(share.get("kinds") or [])<=m.KINDS:return "doubt","kinds"
    if facts>PLAUSIBLE_FACTS:return "doubt","volume"
    held=share.get("file")
    if not held or held.get("Issue")!=number or (held.get("Login") or "").lower()!=author.lower() \
        or held.get("Noted")!={"Gist":gist,"Revision":share.get("revision")} or not share.get("revision"):return "doubt","player-file"
    keys,given,before=held.get("keys") or set(),share.get("gist_keys") or set(),share.get("base_keys") or set()
    if not given<=keys or not before<=keys or not keys<=given|before or held.get("same"):return "doubt","facts"
    checks=pr.get("checks") or []
    failed=[c for c in checks if c[1]=="COMPLETED" and c[2] not in ("SUCCESS","SKIPPED","NEUTRAL") and c[0]]
    if failed:return "doubt","ci"
    build=[c for c in checks if c[0]==REQUIRED]
    if not build or build[0][1]!="COMPLETED":return "wait",f"'{REQUIRED}' has not run yet, or still runs"
    if build[0][2]!="SUCCESS":return "doubt","ci"
    return "merge","every check passed"

# ---------- what GitHub says (network) ----------
def gh(*args,raw=False):
    out=subprocess.run(["gh",*args],capture_output=True,timeout=TIMEOUT)
    if out.returncode!=0:raise RuntimeError(f"gh {' '.join(args[:2])}: {out.stderr.decode('utf-8','replace').strip()[:200]}")
    return out.stdout if raw else out.stdout.decode("utf-8")

def pull_request(n):
    info=json.loads(gh("pr","view",str(n),"--repo",REPOSITORY,"--json","headRefName,baseRefName,isDraft,mergeable,headRefOid,state,changedFiles,statusCheckRollup"))
    rows=gh("api","--paginate",f"repos/{REPOSITORY}/pulls/{n}/files?per_page=100","--jq",'.[] | [.filename, (.previous_filename // "")] | @tsv')
    files=[tuple(line.split("\t")) for line in rows.splitlines() if line.strip()]
    checks=[((c.get("workflowName") or c.get("name") or c.get("context") or ""),(c.get("status") or "COMPLETED"),(c.get("conclusion") or c.get("state") or ""))
            for c in info.get("statusCheckRollup") or []]
    return {"head":info["headRefName"],"base":info["baseRefName"],"state":info["state"],"draft":info["isDraft"],"mergeable":info["mergeable"],
            "sha":info["headRefOid"],"changed":info["changedFiles"],"files":files,"checks":checks}

def file_bytes(path,ref):
    """A file of the repository at a commit or branch (its git blob, base64: any size up to GitHub's 100 MB), or None when it does not exist."""
    try:blob=gh("api",f"repos/{REPOSITORY}/contents/{path}?ref={ref}","--jq",".sha").strip()
    except RuntimeError as error:
        if "404" in str(error) or "Not Found" in str(error):return None
        raise
    if not re.fullmatch(r"[0-9a-f]{40}",blob):raise RuntimeError(f"{path}: no blob")
    return base64.b64decode(gh("api",f"repos/{REPOSITORY}/git/blobs/{blob}","--jq",".content"))

def player_file(ref,number):
    """The player file of a ticket at a commit or branch, read by the merge tool (None: no such file)."""
    data=file_bytes(f"community-discoveries/players/{number}.jsonl.gz",ref)
    if data is None:return None
    with tempfile.TemporaryDirectory() as folder:
        path=os.path.join(folder,f"{number}.jsonl.gz")
        with open(path,"wb") as stream:stream.write(data)
        return tool().read_player(path)

def gather(n):
    """The pull request, its ticket and the share as GitHub has them now (only the pull request when it is not a worker's)."""
    m=tool();pr=pull_request(n)
    head=BRANCH.fullmatch(pr["head"])
    if not head:return pr,{},{}
    number=int(head.group(1))
    issue=json.loads(gh("issue","view",str(number),"--repo",REPOSITORY,"--json","number,title,body,author,labels,state"))
    shares=json.loads(gh("issue","list","-R",REPOSITORY,"--state","open","--limit",str(m.ISSUE_LIMIT),"--json","number,title,body,author,labels"))
    newer=[i["number"] for i in shares if i["number"]>number and m.is_share(i) and m.login(i).lower()==m.login(issue).lower()]
    ticket={"number":issue.get("number"),"state":issue.get("state"),"issue":issue,"newer":newer[0] if newer else None}
    share={"read":False}
    gist,doubt=m.triage(issue)
    if doubt:return pr,ticket,share
    try:
        owner,files,revision=m.gist_listing(gist)
        facts,report=m.clean(m.from_gist_files(files,m.fetch_gist_raw),player=True)
    except Exception as error:   # any failure to read the gist is a doubt, never a merge
        print(f"gist {gist}: not read ({type(error).__name__})");return pr,ticket,share
    share={"read":True,"owner":owner,"gist":gist,"revision":revision,"facts":report["facts"],"refused":report["refused"],"kinds":list(report["kinds"]),
           "gist_keys":{m.key(f) for f in facts}}
    try:
        held=player_file(pr["sha"],number);before=player_file(BASE,number)
    except (RuntimeError,OSError,ValueError) as error:
        print(f"player file: not read ({type(error).__name__}: {str(error)[:120]})");return pr,ticket,share
    if held is not None:
        keys={m.key(f) for f in held["Facts"]}
        same=before is not None and sorted(map(json.dumps,held["Facts"]))==sorted(map(json.dumps,before["Facts"]))
        share["file"]={"Issue":held.get("Issue"),"Login":held.get("Login"),"Noted":held.get("Noted"),"keys":keys,"same":same}
    share["base_keys"]={m.key(f) for f in before["Facts"]} if before else set()
    return pr,ticket,share

def version_on_base():
    text=(file_bytes("src/TenshoAssistant/TenshoAssistant.csproj",BASE) or b"").decode("utf-8","replace")
    found=re.search(r"<Version>(\d+\.\d+\.\d+)",text)
    return found.group(1) if found else "Devloppement"

def compose(english,french):
    sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
    from bilingual import compose as joined
    return joined(english,french)

def comment_doubt(n,code,dry):
    """The reason said once on the pull request (English then French), never twice the same."""
    en,fr=REASONS[code]
    body=compose(f"{MARK}: {en}. It waits for the maintainer's word.",f"Pas fusionné automatiquement : {fr}. Elle attend le mot du mainteneur.")
    if dry:print(body);return
    said=gh("api",f"repos/{REPOSITORY}/issues/{n}/comments?per_page=100","--jq",".[].body")
    if f"{MARK}: {en}." in said:return
    gh("pr","comment",str(n),"--repo",REPOSITORY,"--body",body)

def merge(n,path,dry):
    try:trusted=read_list(path)
    except (OSError,ValueError,UnicodeDecodeError) as error:print(f"the list of trusted accounts does not read: {error}");return 2
    if trusted is None:print(f"no list of trusted accounts ({path}): nothing done (init creates it)");return 1
    pr,ticket,share=gather(n)
    verdict,reason=decide(trusted,pr,ticket,share)
    if verdict=="skip":print(f"#{n}: {reason}: nothing done (the maintainer's word, as before)");return 1
    if verdict=="wait":print(f"#{n}: {reason}: run again later");return 4
    if verdict=="doubt":
        comment_doubt(n,reason,dry)
        print(f"TELL THE MAINTAINER: pull request #{n} (ticket #{ticket.get('number')}) not merged automatically: {REASONS[reason][0]}")
        return 3
    number,author=ticket["number"],tool().login(ticket["issue"])
    subject=f"Discoveries of #{number} {AUTOMATIC} (trusted account {author})"
    body=compose(f"Every check of tools/discoveries-trust.py passed: {share['facts']} facts from gist revision {share['revision'][:12]}.",
                 f"Toutes les vérifications de tools/discoveries-trust.py sont passées : {share['facts']} faits de la révision {share['revision'][:12]} du gist.")
    if dry:print(f"#{n} would be merged (squash, head {pr['sha'][:7]}): {subject}");return 0
    gh("pr","merge",str(n),"--repo",REPOSITORY,"--squash","--match-head-commit",pr["sha"],"--subject",subject,"--body",body)
    print(f"#{n} merged (head {pr['sha'][:7]}): {subject}")
    out=subprocess.run([sys.executable,"-I","-X","utf8",os.path.join(ROOT,"tools","discoveries-merge.py"),"notice",str(number),"merged",version_on_base(),str(share["facts"])],
                       capture_output=True,text=True,timeout=TIMEOUT)
    print((out.stdout+out.stderr).strip() or f"#{number}: notice gave nothing")
    return 0

def init(path):
    if inside(path):print(f"{path} is inside the repository: the list stays outside it");return 2
    if os.path.exists(path):print(f"{path} exists: left as it is");return 0
    os.makedirs(os.path.dirname(path) or ".",exist_ok=True)
    with open(path,"x",encoding="utf-8") as stream:
        stream.write("# Trusted accounts (tools/discoveries-trust.py): one GitHub login per line; kept by the maintainer alone.\n"
                     "# Their discoveries are merged by the worker without waiting, when every check passes.\n"+OWNER+"\n")
    print(f"{path} written with {OWNER}");return 0

# ---------- selftest ----------
def selftest():
    ok=total=0
    def check(cond,what):
        nonlocal ok,total;total+=1;ok+=bool(cond)
        if not cond:print("FAIL",what)
    m=tool()
    gist="0123456789abcdef0123"
    body=f"Shared from Tensho Assistant 0.1822.0\n\nGist: https://gist.github.com/{gist}\n\n- [x] I share these discoveries under CC0-1.0 (public domain).\n"
    issue={"number":43,"title":"Discoveries: Shigu330","body":body,"author":{"login":"Shigu330"},"labels":[{"name":"discoveries"}]}
    files=[("community-discoveries/players/43.jsonl.gz",""),("community-discoveries/v3/index.json",""),("community-discoveries/v3/kind-01.jsonl.gz","")]
    def pr(**k):return {"head":"travailleur/discoveries-43","base":"Devloppement","state":"OPEN","draft":False,"mergeable":"MERGEABLE","files":files,"changed":3,
                        "checks":[("Build and test","COMPLETED","SUCCESS"),("CodeQL","COMPLETED","SUCCESS")],**k}
    def ticket(**k):return {"number":43,"state":"OPEN","issue":issue,"newer":None,**k}
    def held(**k):return {"Issue":43,"Login":"Shigu330","Noted":{"Gist":gist,"Revision":"r2"},"keys":{"a","b","c"},"same":False,**k}
    def share(**k):return {"read":True,"owner":"shigu330","gist":gist,"revision":"r2","facts":100,"refused":5,"kinds":[1,3],"gist_keys":{"a","c"},
                           "file":held(),"base_keys":{"a","b"},**k}
    trusted={"shigu330"}
    check(decide(trusted,pr(),ticket(),share())==("merge","every check passed"),"a trusted account, every check green: merged")
    check(decide(set(),pr(),ticket(),share())[0]=="skip" and decide({"someone"},pr(),ticket(),share())[0]=="skip","an account not on the list: nothing done")
    other={**issue,"author":{"login":"Mallory"},"title":"Discoveries: Mallory"}
    check(decide(trusted,pr(),ticket(issue=other),share(owner="Mallory"))[0]=="skip","the ticket's author decides, never the pull request's")
    check(decide(trusted,pr(head="feature/x"),ticket(),share())[0]=="skip","not a worker branch: nothing done")
    check(decide(trusted,pr(head="travailleur/discoveries-43-v3"),ticket(),share())[0]=="merge" and decide(trusted,pr(head="travailleur/discoveries-430"),ticket(),share())[0]=="skip","a suffix after the ticket accepted, another ticket not")
    check(decide(trusted,pr(checks=[("Build and test","IN_PROGRESS","")]),ticket(),share())[0]=="wait","its CI still runs: wait")
    check(decide(trusted,pr(checks=[]),ticket(),share())[0]=="wait","no CI yet: wait")
    reds={
        "pr-state":(pr(draft=True),ticket(),share()),
        "pr-base":(pr(base="main"),ticket(),share()),
        "pr-conflict":(pr(mergeable="CONFLICTING"),ticket(),share()),
        "pr-files":(pr(files=files+[("src/TenshoAssistant/Plugin.cs","")],changed=4),ticket(),share()),
    }
    reds["pr-files (cut list)"]=(pr(changed=4),ticket(),share())
    reds["pr-files (another ticket)"]=(pr(files=[("community-discoveries/players/44.jsonl.gz","")]+files,changed=4),ticket(),share())
    reds["pr-files (renamed from outside)"]=(pr(files=[("community-discoveries/players/43.jsonl.gz","docs/x.md")]+files[1:]),ticket(),share())
    reds["ticket"]=(pr(),ticket(state="CLOSED"),share())
    reds["ticket (cc0)"]=(pr(),ticket(issue={**issue,"body":body.replace("[x]","[ ]")}),share())
    reds["ticket (title)"]=(pr(),ticket(issue={**issue,"title":"Discoveries: Bob"}),share())
    reds["newer"]=(pr(),ticket(newer=50),share())
    reds["gist"]=(pr(),ticket(),{"read":False})
    reds["owner"]=(pr(),ticket(),share(owner="Bob"))
    reds["refused"]=(pr(),ticket(),share(facts=90,refused=10))
    reds["volume"]=(pr(),ticket(),share(facts=PLAUSIBLE_FACTS+1,refused=0))
    reds["volume (empty)"]=(pr(),ticket(),share(facts=0,refused=0))
    reds["kinds"]=(pr(),ticket(),share(kinds=[1,26]))
    reds["player-file"]=(pr(),ticket(),share(file=held(Noted={"Gist":gist,"Revision":"r1"})))
    reds["player-file (login)"]=(pr(),ticket(),share(file=held(Login="Bob")))
    reds["player-file (none)"]=(pr(),ticket(),share(file=None))
    reds["facts (lost)"]=(pr(),ticket(),share(file=held(keys={"a","c"})))
    reds["facts (from elsewhere)"]=(pr(),ticket(),share(file=held(keys={"a","b","c","z"})))
    reds["facts (nothing new)"]=(pr(),ticket(),share(file=held(same=True)))
    reds["ci"]=(pr(checks=[("Build and test","COMPLETED","FAILURE")]),ticket(),share())
    reds["ci (another check)"]=(pr(checks=[("Build and test","COMPLETED","SUCCESS"),("CodeQL","COMPLETED","FAILURE")]),ticket(),share())
    for name,args in reds.items():
        got=decide(trusted,*args)
        check(got[0]=="doubt" and got[1]==name.split(" ")[0] and got[1] in REASONS,f"a red check gives a doubt, never a merge: {name} → {got}")
    check(all(m.KINDS) and 26 not in m.KINDS,"the merge tool's kinds")
    check(parse_list("# trusted\nShigu330\n  friend-1  # a friend\n\n")=={"shigu330","friend-1"},"the list: comments and letter case")
    for bad in ("two words","-dash","a"*40,"name@x"):
        try:parse_list(bad);check(False,f"a line that is not a login refused: {bad}")
        except ValueError:check(True,"refused")
    check(inside(os.path.join(ROOT,"community-discoveries","trusted.txt")) and not inside(os.path.join(os.path.dirname(ROOT),"elsewhere.txt")),"the list never inside the repository")
    try:read_list(os.path.join(ROOT,"trusted-accounts.txt"));check(False,"a list inside the repository refused")
    except ValueError:check(True,"refused")
    with tempfile.TemporaryDirectory() as folder:
        path=os.path.join(folder,"t","trusted-accounts.txt")
        check(read_list(path) is None,"no list: none")
        import contextlib,io
        with contextlib.redirect_stdout(io.StringIO()):made=init(path);again=init(path)
        check(made==0 and again==0 and read_list(path)=={OWNER.lower()},"init writes the owner's account once, never over")
        with open(path,"w",encoding="utf-8") as stream:stream.write("friend\n")
        with contextlib.redirect_stdout(io.StringIO()):init(path)
        check(read_list(path)=={"friend"},"init leaves a list as the maintainer wrote it")
    check(allowed("community-discoveries/v3/kind-07.jsonl.gz",43) and not allowed("community-discoveries/v3/../x",43) and not allowed("community-discoveries/players/430.jsonl.gz",43),"allowed paths")
    print(f"discoveries-trust selftest: {ok}/{total} ok");return 0 if ok==total else 1

if __name__=="__main__":
    for stream in (sys.stdout,sys.stderr):
        if hasattr(stream,"reconfigure"):stream.reconfigure(encoding="utf-8",errors="replace")
    argv=sys.argv[1:];dry="--dry-run" in argv;argv=[a for a in argv if a!="--dry-run"]
    path=DEFAULT_LIST
    if "--list" in argv:
        at=argv.index("--list")
        if at+1>=len(argv):print(__doc__);sys.exit(2)
        path=argv[at+1];del argv[at:at+2]
    if argv==["--selftest"]:sys.exit(selftest())
    if argv==["init"]:sys.exit(init(path))
    if len(argv)==2 and argv[0]=="merge" and argv[1].isdigit():
        try:sys.exit(merge(int(argv[1]),path,dry))
        except (RuntimeError,subprocess.SubprocessError,OSError,ValueError) as error:
            print(f"TELL THE MAINTAINER: pull request #{argv[1]} not merged automatically: {type(error).__name__}: {str(error)[:200]}");sys.exit(3)
    print(__doc__);sys.exit(2)
