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
      runs, or GitHub has not computed yet whether it merges: nothing done, run again later.
  python -I -X utf8 tools/discoveries-trust.py --selftest
      The decision on made-up pull requests, tickets and shares (no network).

The list: a text file, one GitHub login per line (letter case ignored); « # » starts a comment. Default: ~/.tensho/trusted-accounts.txt (on
Windows %USERPROFILE%\.tensho\trusted-accounts.txt), never inside a checkout of the repository (refused there: nothing of the repository
can add a name). Kept by the maintainer alone; this tool writes it only at init, with the repository owner's account.

Checks, all needed for a merge:
  - X-823 (review of 0.1823, I1): where the pull request comes from: a branch of this repository itself (never a fork: isCrossRepository
    false, head repository Shigu330/Tensho-Assistant), opened by the worker's account (WORKER: the laptop works with the repository owner's
    account). Only an account that may push to the repository can make such a branch. The commits' authors are not read: GitHub maps them
    from an e-mail address anyone can write (the worker's show none), so they prove nothing;
  - the pull request: open, not a draft, base Devloppement, branch travailleur/discoveries-<issue>, no conflict; its files (every page of the
    list, counted against GitHub's count) are players/<issue>.jsonl.gz (and the <issue>.json of before, removed) and v3/ files only, under
    community-discoveries/; its « Build and test » succeeded and no finished check failed (still running: exit 4);
  - the ticket <issue>: open, a share (discoveries-merge.py is_share), one gist link, CC0-1.0 ticked, a title naming its author; its author
    (the ticket's, never the pull request's) on the list; the gist owned by that author; no newer open share of the same author;
  - the gist as it is now (read like sync reads it): readable, at most 5 % of its facts refused, known kinds only, at most PLAUSIBLE_FACTS facts;
  - the player file of the pull request: that issue and that login, the gist and revision just read noted as merged, a fact file the merge
    tool reads (fields of a player file only: nothing of the player), every fact of the gist and of the file on Devloppement kept, no fact
    from anywhere else, and something new;
  - X-823: the content, by value: a sync of this share alone is replayed offline on Devloppement's community-discoveries (the commit read
    then), with the gist just read (discoveries-merge.py: union, rebuild); every file of the pull request's players/ and v3/ must hold what
    that replay writes (gzip files compared once unpacked, index.json without its byte counts and hashes, which must name the pull
    request's own files), no file more, none missing.
A merge squashes on the checked head commit (--match-head-commit), its commit says « merged automatically » (the maintainer reads each one),
then the ticket is commented in English then French (discoveries-merge.py notice <issue> merged <version> <facts>).
"""
import base64,hashlib,importlib.util,json,os,re,subprocess,sys,tempfile

ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPOSITORY="Shigu330/Tensho-Assistant";BASE="Devloppement";REQUIRED="Build and test";OWNER=REPOSITORY.split("/")[0]
WORKER=OWNER   # X-823: the account the laptop worker opens its pull requests with
FOLDERS=("community-discoveries/players","community-discoveries/v3")   # X-823: what a sync writes, compared whole
DEFAULT_LIST=os.path.join(os.path.expanduser("~"),".tensho","trusted-accounts.txt")
PLAUSIBLE_FACTS=200_000   # a share past this many facts is looked at by the maintainer
TIMEOUT=60
LOGIN=re.compile(r"[A-Za-z0-9][A-Za-z0-9-]{0,38}")
BRANCH=re.compile(r"travailleur/discoveries-(\d+)(?:-[a-z0-9]+)*")   # the worker may add a suffix (travailleur/discoveries-43-v3)
AUTOMATIC="merged automatically"   # in the merge commit's title: what the maintainer looks for at each return
MARK="Not merged automatically"
REASONS={
    "pr-state":("the pull request is not open, or is a draft","la demande de fusion n'est pas ouverte, ou est un brouillon"),
    "pr-origin":("the pull request does not come from a branch of this repository opened by the worker's account (a fork, or another author)",
                 "la demande de fusion ne vient pas d'une branche de ce dépôt ouverte par le compte du travailleur (un fork, ou un autre auteur)"),
    "pr-base":("the pull request is not based on Devloppement","la demande de fusion n'est pas basée sur Devloppement"),
    "pr-conflict":("GitHub sees a conflict","GitHub voit un conflit"),
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
    "content":("the pull request's player file or common set is not what a sync of this share alone writes",
               "le fichier du joueur ou l'ensemble commun de la demande n'est pas ce qu'écrit un sync de ce seul partage"),
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
    # X-823: a branch of this repository itself, opened by the worker's account (a fork can name its branch anything)
    if pr.get("cross") is not False or (pr.get("head_repo") or "").lower()!=REPOSITORY.lower() \
        or (pr.get("author") or "").lower()!=WORKER.lower():return "doubt","pr-origin"
    if pr.get("state")!="OPEN" or pr.get("draft"):return "doubt","pr-state"
    if pr.get("base")!=BASE:return "doubt","pr-base"
    # X-824 M2 (review of 0.1823): « UNKNOWN » is GitHub not having computed it yet (just after a push): wait, never a public doubt
    if pr.get("mergeable")=="UNKNOWN":return "wait","GitHub has not computed whether it merges yet"
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
    if share.get("content")!="same":return "doubt","content"   # X-823: by value, replayed (never read: a doubt)
    checks=pr.get("checks") or []
    failed=[c for c in checks if c[1]=="COMPLETED" and c[2] not in ("SUCCESS","SKIPPED","NEUTRAL") and c[0]]
    if failed:return "doubt","ci"
    build=[c for c in checks if c[0]==REQUIRED]
    if not build or build[0][1]!="COMPLETED":return "wait",f"'{REQUIRED}' has not run yet, or still runs"
    if build[0][2]!="SUCCESS":return "doubt","ci"
    return "merge","every check passed"

# ---------- X-823: the content, replayed (pure: no network) ----------
def blob_sha(data):
    """The git blob id of these bytes (what GitHub lists for a file)."""
    return hashlib.sha1(b"blob %d\0"%len(data)+data).hexdigest()

def replay(base,number,author,gist_facts,noted):
    """What a sync of this share alone writes. base: {repository path: bytes} of FOLDERS on Devloppement; gist_facts: the gist's facts as
    discoveries-merge.py clean(player=True) gives them; noted: {"Gist","Revision"}. → {repository path: bytes} of FOLDERS after the sync
    (sync's own steps: the player file's facts united with the gist's, the gist's places and days given to rebuild, which writes the common
    set then the player files). Raises what rebuild raises (Lossy, ValueError): never a merge."""
    m=tool()
    with tempfile.TemporaryDirectory() as folder:
        roots={sub:os.path.join(folder,sub.rsplit("/",1)[1]) for sub in FOLDERS}
        for root in roots.values():os.makedirs(root)
        for path,data in base.items():
            sub,name=path.rsplit("/",1)
            if sub not in roots or not re.fullmatch(r"[A-Za-z0-9._-]+",name):raise ValueError(f"not a file of {', '.join(FOLDERS)}: {path}")
            with open(os.path.join(roots[sub],name),"wb") as stream:stream.write(data)
        saved=(m.PLAYERS,m.V3_DIR)
        m.PLAYERS,m.V3_DIR=roots[FOLDERS[0]],roots[FOLDERS[1]]
        try:
            past={m.key(f) for f in m.from_v3_dir(m.V3_DIR)["Facts"]} if os.path.isfile(os.path.join(m.V3_DIR,m.V3_INDEX)) else None
            name=f"{number}{m.PLAYER_SUFFIX}"
            held=m.player_of(number)
            facts=m.union((held or {}).get("Facts") or [],gist_facts)
            m.rebuild(say=lambda *_:None,fresh=[(name,f) for f in gist_facts],pending={name:(number,author,facts,m.merged_note(noted))},history=lambda:past)
        finally:m.PLAYERS,m.V3_DIR=saved
        return {f"{sub}/{name}":open(os.path.join(root,name),"rb").read() for sub,root in roots.items() for name in sorted(os.listdir(root))}

def index_entries(data):
    """An index.json's text without what depends on the compressor (each entry's Bytes and Sha256), and its entries."""
    index=json.loads(data.decode("utf-8"))
    entries=index.get("Files") if isinstance(index,dict) else None
    if not isinstance(entries,list) or not all(isinstance(e,dict) for e in entries):raise ValueError("not an index")
    return {**index,"Files":[{k:v for k,v in e.items() if k not in ("Bytes","Sha256")} for e in entries]},entries

def content_doubt(expected,head):
    """Pure. None when the pull request's files of FOLDERS (head: {path: bytes}) hold what the replay writes (expected); else what differs.
    A gzip file is compared unpacked, an index without its byte counts and hashes (another zlib may pack the same text otherwise); the head's
    index must then name its own kind files' exact sizes and hashes (the plugin checks them)."""
    m=tool()
    missing=sorted(set(expected)-set(head));extra=sorted(set(head)-set(expected))
    if missing:return f"missing from the pull request: {', '.join(missing[:3])}"
    if extra:return f"not written by a sync of this share: {', '.join(extra[:3])}"
    for path in sorted(expected):
        a,b=expected[path],head[path]
        if a==b:continue
        try:
            if path.endswith(".gz"):same=m.gunzip_bounded(a,m.MAX_DECOMPRESSED)==m.gunzip_bounded(b,m.MAX_DECOMPRESSED)
            elif path.endswith("/"+m.V3_INDEX):same=index_entries(a)[0]==index_entries(b)[0]
            else:same=False
        except Exception as error:return f"{path}: does not read ({type(error).__name__})"   # zlib.error is no OSError: any failure is a doubt
        if not same:return f"{path}: differs from what a sync of this share writes"
    index=f"{FOLDERS[1]}/{m.V3_INDEX}"
    if index in head:
        for e in index_entries(head[index])[1]:
            data=head.get(f"{FOLDERS[1]}/{e.get('Name')}")
            if data is None or e.get("Bytes")!=len(data) or str(e.get("Sha256") or "").upper()!=hashlib.sha256(data).hexdigest().upper():
                return f"{index}: its entry {str(e.get('Name'))[:40]} does not name the pull request's file"
    return None

# ---------- what GitHub says (network) ----------
def gh(*args,raw=False):
    out=subprocess.run(["gh",*args],capture_output=True,timeout=TIMEOUT)
    if out.returncode!=0:raise RuntimeError(f"gh {' '.join(args[:2])}: {out.stderr.decode('utf-8','replace').strip()[:200]}")
    return out.stdout if raw else out.stdout.decode("utf-8")

def pull_request(n):
    info=json.loads(gh("pr","view",str(n),"--repo",REPOSITORY,"--json","headRefName,baseRefName,isDraft,mergeable,headRefOid,state,changedFiles,statusCheckRollup,"
                       "isCrossRepository,headRepository,headRepositoryOwner,author"))
    rows=gh("api","--paginate",f"repos/{REPOSITORY}/pulls/{n}/files?per_page=100","--jq",'.[] | [.filename, (.previous_filename // "")] | @tsv')
    files=[tuple(line.split("\t")) for line in rows.splitlines() if line.strip()]
    checks=[check_row(c) for c in info.get("statusCheckRollup") or []]
    owner=((info.get("headRepositoryOwner") or {}).get("login") or "");name=((info.get("headRepository") or {}).get("name") or "")
    return {"head":info["headRefName"],"base":info["baseRefName"],"state":info["state"],"draft":info["isDraft"],"mergeable":info["mergeable"],
            "sha":info["headRefOid"],"changed":info["changedFiles"],"files":files,"checks":checks,
            "cross":info.get("isCrossRepository"),"head_repo":f"{owner}/{name}" if owner and name else "","author":((info.get("author") or {}).get("login") or "")}

def check_row(c):
    """X-824 M2 (review of 0.1823): one entry of statusCheckRollup as (name, status, conclusion). A check run says its status (COMPLETED once
    done) and conclusion; a commit status (StatusContext: no status, a state) is done only at SUCCESS, FAILURE or ERROR, its PENDING or
    EXPECTED being still to come. Before, a missing status read as COMPLETED, so a pending status was taken for a failure (« ci »), and the
    pull request was told in public that a check had failed while it ran."""
    name=c.get("workflowName") or c.get("name") or c.get("context") or ""
    if "status" not in c and "state" in c or c.get("__typename")=="StatusContext":
        state=(c.get("state") or "").upper()
        return (name,"COMPLETED" if state in ("SUCCESS","FAILURE","ERROR") else "PENDING",state)
    return (name,(c.get("status") or "PENDING").upper(),(c.get("conclusion") or "").upper())

def folder_blobs(ref):
    """X-823: {repository path: git blob id} of the files of FOLDERS at a commit (a folder that does not exist: none)."""
    found={}
    for folder in FOLDERS:
        try:rows=gh("api",f"repos/{REPOSITORY}/contents/{folder}?ref={ref}","--jq",'.[] | [.type, .path, .sha] | @tsv')
        except RuntimeError as error:
            if "404" in str(error) or "Not Found" in str(error):continue
            raise
        for line in rows.splitlines():
            if not line.strip():continue
            kind,path,sha=line.split("\t")
            if kind!="file" or not re.fullmatch(r"[0-9a-f]{40}",sha):raise RuntimeError(f"{path}: not a plain file")   # a folder or a link where a sync writes files
            found[path]=sha
    return found

def blob(sha):
    data=base64.b64decode(gh("api",f"repos/{REPOSITORY}/git/blobs/{sha}","--jq",".content"))
    if blob_sha(data)!=sha:raise RuntimeError(f"blob {sha[:7]}: its bytes do not match its id")
    return data

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

def player_bytes(files,number):
    """The player file of a ticket among files read ({path: bytes}; the new file, else the one of before), read by the merge tool; None without one."""
    m=tool()
    for name in (f"{number}{m.PLAYER_SUFFIX}",f"{number}.json"):
        data=files.get(f"{FOLDERS[0]}/{name}")
        if data is None:continue
        with tempfile.TemporaryDirectory() as folder:
            path=os.path.join(folder,name)
            with open(path,"wb") as stream:stream.write(data)
            return m.read_player(path)
    return None

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
        # X-823: Devloppement pinned to one commit, read whole for the replay (and the player file of before from it)
        base_sha=gh("api",f"repos/{REPOSITORY}/branches/{BASE}","--jq",".commit.sha").strip()
        if not re.fullmatch(r"[0-9a-f]{40}",base_sha):raise RuntimeError(f"{BASE}: no commit")
        base={path:blob(sha) for path,sha in folder_blobs(base_sha).items()}
        held=player_file(pr["sha"],number);before=player_bytes(base,number)
    except (RuntimeError,OSError,ValueError) as error:
        print(f"player file: not read ({type(error).__name__}: {str(error)[:120]})");return pr,ticket,share
    if held is not None:
        keys={m.key(f) for f in held["Facts"]}
        same=before is not None and sorted(map(json.dumps,held["Facts"]))==sorted(map(json.dumps,before["Facts"]))
        share["file"]={"Issue":held.get("Issue"),"Login":held.get("Login"),"Noted":held.get("Noted"),"keys":keys,"same":same}
    share["base_keys"]={m.key(f) for f in before["Facts"]} if before else set()
    try:
        expected=replay(base,number,m.login(issue),facts,{"Gist":gist,"Revision":revision})
        head={}
        for path,sha in folder_blobs(pr["sha"]).items():
            data=expected.get(path)
            head[path]=data if data is not None and blob_sha(data)==sha else blob(sha)
        found=content_doubt(expected,head)
    except Exception as error:   # a replay that stops (Lossy, a file that does not read) is a doubt, never a merge
        found=f"not replayed ({type(error).__name__}: {str(error)[:120]})"
    if found:print(f"content: {found}")
    share["content"]=found or "same"
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
                        "checks":[("Build and test","COMPLETED","SUCCESS"),("CodeQL","COMPLETED","SUCCESS")],
                        "cross":False,"head_repo":REPOSITORY,"author":WORKER,**k}
    def ticket(**k):return {"number":43,"state":"OPEN","issue":issue,"newer":None,**k}
    def held(**k):return {"Issue":43,"Login":"Shigu330","Noted":{"Gist":gist,"Revision":"r2"},"keys":{"a","b","c"},"same":False,**k}
    def share(**k):return {"read":True,"owner":"shigu330","gist":gist,"revision":"r2","facts":100,"refused":5,"kinds":[1,3],"gist_keys":{"a","c"},
                           "file":held(),"base_keys":{"a","b"},"content":"same",**k}
    trusted={"shigu330"}
    check(decide(trusted,pr(),ticket(),share())==("merge","every check passed"),"a trusted account, every check green: merged")
    check(decide(set(),pr(),ticket(),share())[0]=="skip" and decide({"someone"},pr(),ticket(),share())[0]=="skip","an account not on the list: nothing done")
    other={**issue,"author":{"login":"Mallory"},"title":"Discoveries: Mallory"}
    check(decide(trusted,pr(),ticket(issue=other),share(owner="Mallory"))[0]=="skip","the ticket's author decides, never the pull request's")
    check(decide(trusted,pr(head="feature/x"),ticket(),share())[0]=="skip","not a worker branch: nothing done")
    check(decide(trusted,pr(head="travailleur/discoveries-43-v3"),ticket(),share())[0]=="merge" and decide(trusted,pr(head="travailleur/discoveries-430"),ticket(),share())[0]=="skip","a suffix after the ticket accepted, another ticket not")
    check(decide(trusted,pr(checks=[("Build and test","IN_PROGRESS","")]),ticket(),share())[0]=="wait","its CI still runs: wait")
    check(decide(trusted,pr(checks=[]),ticket(),share())[0]=="wait","no CI yet: wait")
    # X-824 M2: passing states wait, never a public doubt
    check(decide(trusted,pr(mergeable="UNKNOWN"),ticket(),share())[0]=="wait","X-824 M2: mergeable not computed yet: wait")
    rows=[check_row(c) for c in ({"__typename":"CheckRun","workflowName":"Build and test","status":"COMPLETED","conclusion":"SUCCESS"},
                                 {"__typename":"StatusContext","context":"ci/other","state":"PENDING"},{"context":"ci/expected","state":"EXPECTED"})]
    check(rows[1][1]=="PENDING" and rows[2][1]=="PENDING" and decide(trusted,pr(checks=rows),ticket(),share())[0]=="merge",
          f"X-824 M2: a commit status still pending is no failure: {rows}")
    check(check_row({"workflowName":"Build and test"})[1]=="PENDING" and decide(trusted,pr(checks=[check_row({"workflowName":"Build and test"})]),ticket(),share())[0]=="wait",
          "X-824 M2: a check run without its status is not finished: wait")
    failed=[check_row(c) for c in ({"workflowName":"Build and test","status":"COMPLETED","conclusion":"SUCCESS"},{"context":"ci/other","state":"ERROR"})]
    check(decide(trusted,pr(checks=failed),ticket(),share())==("doubt","ci"),"X-824 M2: a commit status in error is still a failure")
    reds={
        "pr-origin (a fork)":(pr(cross=True,head_repo="Mallory/Tensho-Assistant"),ticket(),share()),
        "pr-origin (a fork that names the repository)":(pr(cross=True),ticket(),share()),
        "pr-origin (not said)":(pr(cross=None),ticket(),share()),
        "pr-origin (another repository)":(pr(head_repo="Mallory/Tensho-Assistant"),ticket(),share()),
        "pr-origin (deleted head repository)":(pr(head_repo=""),ticket(),share()),
        "pr-origin (another author)":(pr(author="Mallory"),ticket(),share()),
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
    reds["content"]=(pr(),ticket(),share(content="community-discoveries/v3/kind-01.jsonl.gz: differs"))
    reds["content (never replayed)"]=(pr(),ticket(),{k:v for k,v in share().items() if k!="content"})
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
    end_to_end(check)
    print(f"discoveries-trust selftest: {ok}/{total} ok");return 0 if ok==total else 1

class Merged(Exception):
    """end_to_end: the made-up GitHub was asked to merge."""

def end_to_end(check):
    """X-823: merge() against a made-up GitHub (gh and the gist reader replaced, nothing sent): a share replayed by the real merge tool; a fork,
    another author, a file out of scope, a common set or player file changed by value, a file more: never merged."""
    import contextlib,gzip,io
    m=tool()
    gist="0123456789abcdef0123";login="Shigu330";base_sha="2"*40;head_sha="1"*40
    body=f"Shared from Tensho Assistant 0.1823.0\n\nGist: https://gist.github.com/{gist}\n\n- [x] I share these discoveries under CC0-1.0 (public domain).\n"
    issue={"number":43,"title":f"Discoveries: {login}","body":body,"author":{"login":login},"labels":[{"name":"discoveries"}],"state":"OPEN"}
    def raw(subject,level=1):return {"Kind":1,"Subject":subject,"Detail":0,"Territory":128,"Map":1,"CellX":1,"CellZ":2,"X":60.0,"Z":140.0,
                                     "LevelMin":1,"LevelMax":level,"Seen":2,"Imported":0,"First":"2026-11-10T00:00:00+00:00","Last":"2026-11-12T00:00:00+00:00"}
    def cleaned(*facts):return m.clean({"Version":2,"Facts":[dict(f) for f in facts]},player=True)[0]
    base=replay({},43,login,cleaned(raw(1)),{"Gist":"f"*20,"Revision":"r1"})
    good=replay(base,43,login,cleaned(raw(2)),{"Gist":gist,"Revision":"r2"})
    kind=f"{FOLDERS[1]}/kind-01.jsonl.gz";index=f"{FOLDERS[1]}/{m.V3_INDEX}";mine=f"{FOLDERS[0]}/43{m.PLAYER_SUFFIX}"
    check(content_doubt(good,dict(good)) is None and set(good)=={kind,index,mine},f"the replay writes the player file and the common set ({sorted(good)})")
    def reindexed(files):
        """A head whose index names its kind files exactly (what a careful forger would also do)."""
        entries=index_entries(files[index])[1]
        for e in entries:
            data=files[f"{FOLDERS[1]}/{e['Name']}"];e["Bytes"]=len(data);e["Sha256"]=hashlib.sha256(data).hexdigest().upper()
        text=json.loads(files[index].decode("utf-8"));text["Files"]=entries
        return {**files,index:(json.dumps(text,ensure_ascii=False,indent=1)+"\n").encode("utf-8")}
    def repacked(data,change):
        lines=gzip.decompress(data).decode("utf-8").splitlines()
        packed=gzip.compress(("".join(change(n,line)+"\n" for n,line in enumerate(lines))).encode("utf-8"),compresslevel=1,mtime=12345)
        assert packed!=data   # other bytes (the header's time at least), as another compressor would give
        return packed
    level99=lambda n,line:line.replace('"LevelMax":1','"LevelMax":99') if n>0 and '"Subject":2,' in line else line
    def pr_info(**k):return {"headRefName":"travailleur/discoveries-43","baseRefName":BASE,"isDraft":False,"mergeable":"MERGEABLE","headRefOid":head_sha,
                             "state":"OPEN","isCrossRepository":False,"headRepository":{"name":"Tensho-Assistant"},"headRepositoryOwner":{"login":"Shigu330"},
                             "author":{"login":WORKER},"statusCheckRollup":[{"workflowName":REQUIRED,"status":"COMPLETED","conclusion":"SUCCESS"}],**k}
    def run(head,info=None,extra=(),dry=True):
        """merge() on this head ({path: bytes}); → (exit code, printed text, the gh calls made)."""
        trees={base_sha:base,head_sha:head};store={blob_sha(d):d for t in trees.values() for d in t.values()}
        changed=sorted(p for p in set(base)|set(head) if base.get(p)!=head.get(p))+list(extra)
        calls=[]
        def fake(*args,raw=False):
            calls.append(args);a=list(args)
            if a[:2]==["pr","view"]:info_=pr_info(**(info or {}));info_["changedFiles"]=len(changed);return json.dumps(info_)
            if a[:2]==["issue","view"]:return json.dumps(issue)
            if a[:2]==["issue","list"]:return json.dumps([issue])
            if a[:2]==["pr","comment"]:return ""
            if a[:2]==["pr","merge"]:raise Merged()   # stops merge() here: the notice (a real gh call) never runs from a test
            url=next((x for x in a if x.startswith("repos/")),"") if a[0]=="api" else ""
            if url.endswith("/files?per_page=100"):return "".join(f"{p}\t\n" for p in changed)
            if url.endswith(f"/branches/{BASE}"):return base_sha+"\n"
            if "/comments?" in url:return ""
            if "/git/blobs/" in url:return base64.b64encode(store[url.rsplit("/",1)[1]]).decode("ascii")
            if "/contents/" in url:
                path,ref=url.split("/contents/",1)[1].split("?ref=");tree=trees[ref]
                if path in FOLDERS:return "".join(f"file\t{p}\t{blob_sha(d)}\n" for p,d in tree.items() if p.rsplit("/",1)[0]==path)
                if path in tree:return blob_sha(tree[path])+"\n"
                raise RuntimeError("gh api: HTTP 404: Not Found")
            raise RuntimeError(f"unexpected gh call: {a[:3]}")
        global gh
        saved=(gh,m.gist_listing,m.from_gist_files)
        gh=fake;m.gist_listing=lambda g:(login,{},"r2");m.from_gist_files=lambda files,fetch:{"Version":2,"Facts":[raw(2)]}
        out=io.StringIO()
        try:
            with tempfile.TemporaryDirectory() as folder, contextlib.redirect_stdout(out):
                path=os.path.join(folder,"trusted-accounts.txt")
                with open(path,"w",encoding="utf-8") as stream:stream.write(login+"\n")
                try:code=merge(43,path,dry)
                except Merged:code="merged"
        finally:gh,m.gist_listing,m.from_gist_files=saved
        return code,out.getvalue(),calls
    def merged_call(calls):return any(c[:2]==("pr","merge") for c in calls)
    code,said,_=run(good)
    check(code==0 and "would be merged" in said,f"the worker's own pull request, a share replayed whole: merged ({code}: {said[-200:]})")
    code,said,_=run(reindexed({**good,kind:repacked(good[kind],lambda n,line:line)}))
    check(code==0,f"the same text packed by another compressor, its index naming it: merged ({code}: {said[-200:]})")
    cases={
        "a fork":(good,{"isCrossRepository":True,"headRepositoryOwner":{"login":"Mallory"}},(),"pr-origin"),
        "another author":(good,{"author":{"login":"Mallory"}},(),"pr-origin"),
        "a file out of scope":(good,None,("src/TenshoAssistant/Plugin.cs",),"pr-files"),
        "the common set changed by value":(reindexed({**good,kind:repacked(good[kind],level99)}),None,(),"content"),
        "the player file changed by value":({**good,mine:repacked(good[mine],level99)},None,(),"content"),
        "a kind file more":(reindexed({**good,f"{FOLDERS[1]}/kind-05.jsonl.gz":good[kind]}),None,(),"content"),
        "a kind file missing":({p:d for p,d in good.items() if p!=kind},None,(),"content"),
        "an index naming other bytes":({**good,kind:repacked(good[kind],lambda n,line:line)},None,(),"content"),
    }
    for name,(head,info,extra,reason) in cases.items():
        code,said,calls=run(head,info,extra,dry=False)
        check(code==3 and REASONS[reason][0] in said and not merged_call(calls) and any(c[:2]==("pr","comment") for c in calls),
              f"X-823: {name}: a doubt, never merged ({code}: {said[-240:]})")

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
