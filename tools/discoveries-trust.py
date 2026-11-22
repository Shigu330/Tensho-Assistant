#!/usr/bin/env python3
r"""X-805 (maintainer's decision of 2026-11-22): the laptop worker merges by itself the pull request of a batch of trusted accounts' discoveries,
only when every check below passes. Any doubt leaves the pull request open, commented once with the reason, for the maintainer's word
(scripts of the maintainer); a batch holding an account not on the list changes nothing.
X-830 (D-226 a and D-227 B+C, user's choices of 22/11): a pull request merges a batch (every open share a sync reads at once), names no issue
nor account (branch travailleur/discoveries-batch-<8 hex digits>, title, body, commits), and the player files are anonymous: the link issue ↔
file is the worker's private link file, which the replay reads.

Usage:
  python -I -X utf8 tools/discoveries-trust.py init [--list PATH]
      Creates the list of trusted accounts holding the repository owner's account, when it does not exist (never written over).
  python -I -X utf8 tools/discoveries-trust.py merge <pull request> [--list PATH] [--links PATH] [--dry-run]
      Exit 0: merged (--dry-run: would be); 1: not a batch of trusted accounts' shares, or no list: nothing done; 2: usage, or a list that
      does not read; 3: a doubt: not merged, the pull request commented once with the reason (« TELL THE MAINTAINER » printed); 4: its CI
      still runs, or GitHub has not computed yet whether it merges: nothing done, run again later.
  python -I -X utf8 tools/discoveries-trust.py --selftest
      The decision on made-up pull requests, tickets and shares (no network).

The list: a text file, one GitHub login per line (letter case ignored); « # » starts a comment. Default: ~/.tensho/trusted-accounts.txt (on
Windows %USERPROFILE%\.tensho\trusted-accounts.txt), never inside a checkout of the repository (refused there: nothing of the repository
can add a name). Kept by the maintainer alone; this tool writes it only at init, with the repository owner's account. The private link file
(discoveries-merge.py LINKS_PATH, ~/.tensho/discoveries-links.json) is read, never written.

Checks, all needed for a merge:
  - X-823 (review of 0.1823, I1): where the pull request comes from: a branch of this repository itself (never a fork: isCrossRepository
    false, head repository Shigu330/Tensho-Assistant), opened by the worker's account (WORKER: the laptop works with the repository owner's
    account). Only an account that may push to the repository can make such a branch. The commits' authors are not read: GitHub maps them
    from an e-mail address anyone can write (the worker's show none), so they prove nothing;
  - the pull request: open, not a draft, base Devloppement, branch travailleur/discoveries-batch-<8 hex digits>, no conflict; X-830: its
    branch, title, body and commit messages name no open share's issue nor account (discoveries-merge.py names_in); its files (every page of
    the list, counted against GitHub's count) are anonymous player files, v3/ files and merged.txt only, under community-discoveries/; its
    « Build and test » succeeded and no finished check failed (still running: exit 4);
  - the batch: the open shares a sync reads now (discoveries-merge.py plan: newest issue of each account, CC0-1.0 ticked, one gist link, a
    title naming its author, the gist owned by that author, a revision not listed as merged yet); at least one; every author on the list;
    each gist readable, at most 5 % of its facts refused, known kinds only, at most PLAUSIBLE_FACTS facts; the private link file readable;
  - X-823, X-830: the content, by value: a sync of this batch is replayed offline on Devloppement's community-discoveries (the commit read
    then) with the private link and the gists just read (discoveries-merge.py plan, rebuild); every file of the pull request's players/, v3/
    and merged.txt must hold what that replay writes (gzip files compared once unpacked, index.json without its byte counts and hashes,
    which must name the pull request's own files), no file more, none missing. A batch whose gists bring nothing new changes merged.txt only
    (so the plugin deletes those gists).
A merge squashes on the checked head commit (--match-head-commit), its commit says « merged automatically » and names no issue nor account
(the maintainer reads each one), then each ticket whose facts were merged is commented in English then French (discoveries-merge.py notice).
"""
import base64,hashlib,importlib.util,json,os,re,subprocess,sys,tempfile

ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPOSITORY="Shigu330/Tensho-Assistant";BASE="Devloppement";REQUIRED="Build and test";OWNER=REPOSITORY.split("/")[0]
WORKER=OWNER   # X-823: the account the laptop worker opens its pull requests with
FOLDERS=("community-discoveries/players","community-discoveries/v3")   # X-823: what a sync writes, compared whole
MERGED_FILE="community-discoveries/merged.txt"   # X-830: the gists merged (hashes), written by a sync too
DEFAULT_LIST=os.path.join(os.path.expanduser("~"),".tensho","trusted-accounts.txt")
PLAUSIBLE_FACTS=200_000   # a share past this many facts is looked at by the maintainer
TIMEOUT=60
LOGIN=re.compile(r"[A-Za-z0-9][A-Za-z0-9-]{0,38}")
AUTOMATIC="merged automatically"   # in the merge commit's title: what the maintainer looks for at each return
MARK="Not merged automatically"
REASONS={
    "pr-state":("the pull request is not open, or is a draft","la demande de fusion n'est pas ouverte, ou est un brouillon"),
    "pr-origin":("the pull request does not come from a branch of this repository opened by the worker's account (a fork, or another author)",
                 "la demande de fusion ne vient pas d'une branche de ce dépôt ouverte par le compte du travailleur (un fork, ou un autre auteur)"),
    "pr-base":("the pull request is not based on Devloppement","la demande de fusion n'est pas basée sur Devloppement"),
    "pr-conflict":("GitHub sees a conflict","GitHub voit un conflit"),
    "pr-files":("files other than anonymous player files, the common set and the list of merged gists, or a list of files not read whole",
                "des fichiers autres que des fichiers de joueurs anonymes, l'ensemble commun et la liste des gists fusionnés, ou une liste de fichiers pas lue en entier"),
    "pr-names":("the pull request names an issue or an account of a share (branch, title, body or commits)",
                "la demande de fusion nomme un ticket ou un compte d'un partage (branche, titre, corps ou commits)"),
    "ticket":("a ticket of the batch is closed, not a share, or not readable as one (gist link, CC0-1.0, title)","un ticket du lot est fermé, n'est pas un partage, ou ne se lit pas comme tel (lien du gist, CC0-1.0, titre)"),
    "owner":("a gist of the batch is not owned by its ticket's author","un gist du lot n'appartient pas à l'auteur de son ticket"),
    "gist":("a gist of the batch could not be read","un gist du lot n'a pas pu être lu"),
    "refused":("over 5 % of a gist's facts are refused","plus de 5 % des faits d'un gist sont refusés"),
    "kinds":("a gist holds a kind of fact the merge tool does not know","un gist contient une sorte de fait que l'outil de fusion ne connaît pas"),
    "volume":("a gist holds no fact, or more than a share is expected to hold","un gist ne contient aucun fait, ou plus qu'un partage n'en contient d'habitude"),
    "links":("the private link file does not read on the worker's PC","le fichier de lien privé ne se lit pas sur le PC du travailleur"),
    "facts":("no share of the batch is waiting (nothing a sync would merge)","aucun partage du lot n'est en attente (rien qu'un sync fusionnerait)"),
    "content":("the pull request's player files, common set or list of merged gists are not what a sync of this batch writes",
               "les fichiers des joueurs, l'ensemble commun ou la liste des gists fusionnés de la demande ne sont pas ce qu'écrit un sync de ce lot"),
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
def allowed(path):
    """X-805, X-830: a file the worker's pull request may change: an anonymous player file, the common set, the list of merged gists."""
    return bool(re.fullmatch(r"community-discoveries/players/[0-9a-f]{16}\.jsonl\.gz",path)) or path==MERGED_FILE \
        or bool(re.fullmatch(r"community-discoveries/v3/(index\.json|kind-\d{2}\.jsonl\.gz)",path))

def decide(trusted,pr,batch):
    """Pure. trusted: lower-case logins. pr: {"head","base","state","draft","mergeable","files":[(path, previous)],"changed","checks":[(name, status,
    conclusion)],"cross","head_repo","author","texts": [branch, title, body, commit messages]}. batch: {"issues": the open issues (gh), "links":
    the private link read (bool), "read": [{"number","login","owner","gist","facts","refused","kinds","flag"}] (the shares a sync reads now,
    discoveries-merge.py plan), "tickets": {number: {"state","issue"}}, "content": "same" or what differs}. → (verdict, reason): "merge",
    "skip" (not a batch of trusted accounts' shares: nothing done), "wait" (CI running), "doubt" (a check failed: the reason's code in REASONS)."""
    m=tool()
    if not m.BATCH_BRANCH.fullmatch(pr.get("head") or ""):return "skip","not a worker pull request of a batch of discoveries"
    read=batch.get("read") or []
    if any((r.get("login") or "").lower() not in trusted for r in read):return "skip","an account of the batch is not on the list of trusted accounts"
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
    if len(files)!=pr.get("changed") or not paths or not all(allowed(p) for p in paths):return "doubt","pr-files"
    # X-830 (D-227): the pull request names no share's issue nor account
    shares=[i for i in batch.get("issues") or [] if m.is_share(i)]
    if m.names_in(pr.get("texts") or [],shares):return "doubt","pr-names"
    if not batch.get("links"):return "doubt","links"
    if not read:return "doubt","facts"
    for r in read:
        ticket=(batch.get("tickets") or {}).get(r.get("number")) or {}
        issue=ticket.get("issue") or {}
        if ticket.get("state")!="OPEN" or not m.is_share(issue) or m.triage(issue)[1]:return "doubt","ticket"
        if m.triage(issue)[0]!=r.get("gist"):return "doubt","gist"
        if m.owner_doubt(r.get("owner") or "",issue):return "doubt","owner"
        facts,refused=r.get("facts",0),r.get("refused",0)
        if (facts+refused)==0 or refused*20>facts+refused or r.get("flag"):return "doubt","refused" if refused else "volume"
        if not set(r.get("kinds") or [])<=m.KINDS:return "doubt","kinds"
        if facts>PLAUSIBLE_FACTS:return "doubt","volume"
    if batch.get("content")!="same":return "doubt","content"   # X-823: by value, replayed (never read: a doubt)
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

def replay(base,issues,links,listing,reader):
    """X-830: what a sync of this batch writes. base: {repository path: bytes} of FOLDERS and MERGED_FILE on Devloppement; issues: the open
    issues; links: the private link (a copy is used); listing(gist) → (owner, files, revision) and reader(files) → the gist's data (the gists
    already read). → ({repository path: bytes} after the sync, the shares read: discoveries-merge.py plan's « read »). Raises what rebuild
    raises (Lossy, ValueError): never a merge."""
    m=tool()
    with tempfile.TemporaryDirectory() as folder:
        roots={sub:os.path.join(folder,sub.rsplit("/",1)[1]) for sub in FOLDERS}
        for root in roots.values():os.makedirs(root)
        merged=os.path.join(folder,os.path.basename(MERGED_FILE))
        for path,data in base.items():
            if path==MERGED_FILE:target=merged
            else:
                sub,name=path.rsplit("/",1)
                if sub not in roots or not re.fullmatch(r"[A-Za-z0-9._-]+",name):raise ValueError(f"not a file of {', '.join(FOLDERS)}: {path}")
                target=os.path.join(roots[sub],name)
            with open(target,"wb") as stream:stream.write(data)
        saved=(m.PLAYERS,m.V3_DIR,m.MERGED_PATH)
        m.PLAYERS,m.V3_DIR,m.MERGED_PATH=roots[FOLDERS[0]],roots[FOLDERS[1]],merged
        try:
            m.no_numbered()
            past={m.key(f) for f in m.from_v3_dir(m.V3_DIR)["Facts"]} if os.path.isfile(os.path.join(m.V3_DIR,m.V3_INDEX)) else None
            mine=json.loads(json.dumps(links));notes=m.read_merged()
            done=m.plan([json.loads(json.dumps(i)) for i in issues],mine,notes,listing=listing,reader=reader,say=lambda *_:None)
            m.rebuild(say=lambda *_:None,fresh=done["fresh"],pending=done["pending"],owners=m.owners_of(mine),notes=notes,history=lambda:past)
        finally:m.PLAYERS,m.V3_DIR,m.MERGED_PATH=saved
        out={f"{sub}/{name}":open(os.path.join(root,name),"rb").read() for sub,root in roots.items() for name in sorted(os.listdir(root))}
        if os.path.isfile(merged):out[MERGED_FILE]=open(merged,"rb").read()
        return out,done["read"]

def index_entries(data):
    """An index.json's text without what depends on the compressor (each entry's Bytes and Sha256), and its entries."""
    index=json.loads(data.decode("utf-8"))
    entries=index.get("Files") if isinstance(index,dict) else None
    if not isinstance(entries,list) or not all(isinstance(e,dict) for e in entries):raise ValueError("not an index")
    return {**index,"Files":[{k:v for k,v in e.items() if k not in ("Bytes","Sha256")} for e in entries]},entries

def content_doubt(expected,head):
    """Pure. None when the pull request's files of FOLDERS and MERGED_FILE (head: {path: bytes}) hold what the replay writes (expected); else
    what differs. A gzip file is compared unpacked, an index without its byte counts and hashes (another zlib may pack the same text
    otherwise); the head's index must then name its own kind files' exact sizes and hashes (the plugin checks them)."""
    m=tool()
    missing=sorted(set(expected)-set(head));extra=sorted(set(head)-set(expected))
    if missing:return f"missing from the pull request: {', '.join(missing[:3])}"
    if extra:return f"not written by a sync of this batch: {', '.join(extra[:3])}"
    for path in sorted(expected):
        a,b=expected[path],head[path]
        if a==b:continue
        try:
            if path.endswith(".gz"):same=m.gunzip_bounded(a,m.MAX_DECOMPRESSED)==m.gunzip_bounded(b,m.MAX_DECOMPRESSED)
            elif path.endswith("/"+m.V3_INDEX):same=index_entries(a)[0]==index_entries(b)[0]
            else:same=False
        except Exception as error:return f"{path}: does not read ({type(error).__name__})"   # zlib.error is no OSError: any failure is a doubt
        if not same:return f"{path}: differs from what a sync of this batch writes"
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
                       "isCrossRepository,headRepository,headRepositoryOwner,author,title,body,commits"))
    rows=gh("api","--paginate",f"repos/{REPOSITORY}/pulls/{n}/files?per_page=100","--jq",'.[] | [.filename, (.previous_filename // "")] | @tsv')
    files=[tuple(line.split("\t")) for line in rows.splitlines() if line.strip()]
    checks=[check_row(c) for c in info.get("statusCheckRollup") or []]
    owner=((info.get("headRepositoryOwner") or {}).get("login") or "");name=((info.get("headRepository") or {}).get("name") or "")
    return {"head":info["headRefName"],"base":info["baseRefName"],"state":info["state"],"draft":info["isDraft"],"mergeable":info["mergeable"],
            "sha":info["headRefOid"],"changed":info["changedFiles"],"files":files,"checks":checks,
            "cross":info.get("isCrossRepository"),"head_repo":f"{owner}/{name}" if owner and name else "","author":((info.get("author") or {}).get("login") or ""),
            # X-830 (D-227): what the pull request says, checked for an issue or an account named
            "texts":[info["headRefName"],info.get("title") or "",info.get("body") or "",
                     *[t for c in info.get("commits") or [] for t in (c.get("messageHeadline") or "",c.get("messageBody") or "")]]}

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
    """X-823: {repository path: git blob id} of the files of FOLDERS at a commit (a folder that does not exist: none).
    X-827 (review of 0.1826, M3): read through the git trees API, walked folder by folder from the commit's tree. The contents API lists at
    most 1 000 entries of a folder: past 1 000 player files, the base replayed and the pull request's head were read cut short, and every
    trusted merge became a public doubt. A tree GitHub says is truncated is refused (never a merge on a partial list)."""
    trees={}
    def entries(sha):
        if sha not in trees:
            data=json.loads(gh("api",f"repos/{REPOSITORY}/git/trees/{sha}"))
            if not isinstance(data,dict) or data.get("truncated") or not isinstance(data.get("tree"),list):raise RuntimeError(f"tree {str(sha)[:60]}: truncated or not a tree")
            trees[sha]={e.get("path"):e for e in data["tree"] if isinstance(e,dict)}
        return trees[sha]
    found={}
    for folder in FOLDERS:
        sha=ref
        for part in folder.split("/"):
            entry=entries(sha).get(part)
            if entry is None:sha=None;break
            if entry.get("type")!="tree":raise RuntimeError(f"{folder}: not a folder")
            sha=entry.get("sha")
        if sha is None:continue
        for name,entry in sorted(entries(sha).items()):
            path=f"{folder}/{name}"
            # a folder, a link or a submodule where a sync writes plain files: refused
            if entry.get("type")!="blob" or entry.get("mode") not in ("100644","100755") or not re.fullmatch(r"[0-9a-f]{40}",str(entry.get("sha"))):
                raise RuntimeError(f"{path}: not a plain file")
            found[path]=entry["sha"]
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

def tree_files(ref,known=None):
    """X-830: {repository path: bytes} of FOLDERS and MERGED_FILE at a commit (known: bytes already read, taken when their blob id matches)."""
    found={}
    for path,sha in folder_blobs(ref).items():
        data=(known or {}).get(path)
        found[path]=data if data is not None and blob_sha(data)==sha else blob(sha)
    merged=file_bytes(MERGED_FILE,ref)
    if merged is not None:found[MERGED_FILE]=merged
    return found

def gather(n,links_path=None):
    """The pull request, and the batch as GitHub has it now (only the pull request when it is not a worker's batch)."""
    m=tool();pr=pull_request(n)
    if not m.BATCH_BRANCH.fullmatch(pr["head"]):return pr,{}
    issues=json.loads(gh("issue","list","-R",REPOSITORY,"--state","open","--limit",str(m.ISSUE_LIMIT),"--json","number,title,body,author,labels"))
    batch={"issues":issues,"links":False,"read":[],"tickets":{}}
    try:links=m.read_links(links_path);batch["links"]=links is not None
    except (OSError,ValueError) as error:print(f"link file: not read ({type(error).__name__}: {str(error)[:120]})");links=None
    if links is None:return pr,batch
    # the gists as GitHub has them now, each read once (any failure is no share read: plan says it, the replay does the same)
    listed={};data={}
    def listing(gist):
        if gist not in listed:
            try:listed[gist]=m.gist_listing(gist)
            except Exception as error:listed[gist]=error
        if isinstance(listed[gist],Exception):raise listed[gist]
        return listed[gist]
    def reader(files):
        key=json.dumps(files,sort_keys=True)
        if key not in data:
            try:data[key]=m.from_gist_files(files,m.fetch_gist_raw)
            except Exception as error:data[key]=error
        if isinstance(data[key],Exception):raise data[key]
        return data[key]
    try:
        # X-823: Devloppement pinned to one commit, read whole for the replay
        base_sha=gh("api",f"repos/{REPOSITORY}/branches/{BASE}","--jq",".commit.sha").strip()
        if not re.fullmatch(r"[0-9a-f]{40}",base_sha):raise RuntimeError(f"{BASE}: no commit")
        base=tree_files(base_sha)
        expected,read=replay(base,issues,links,listing,reader)
        batch["read"]=read
        for r in read:
            issue=json.loads(gh("issue","view",str(r["number"]),"--repo",REPOSITORY,"--json","number,title,body,author,labels,state"))
            batch["tickets"][r["number"]]={"state":issue.get("state"),"issue":issue}
        found=content_doubt(expected,tree_files(pr["sha"],expected))
    except Exception as error:   # a replay that stops (Lossy, a file that does not read) is a doubt, never a merge
        found=f"not replayed ({type(error).__name__}: {str(error)[:120]})"
    if found:print(f"content: {found}")
    batch["content"]=found or "same"
    return pr,batch

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

def merge(n,path,dry,links_path=None):
    try:trusted=read_list(path)
    except (OSError,ValueError,UnicodeDecodeError) as error:print(f"the list of trusted accounts does not read: {error}");return 2
    if trusted is None:print(f"no list of trusted accounts ({path}): nothing done (init creates it)");return 1
    pr,batch=gather(n,links_path)
    verdict,reason=decide(trusted,pr,batch)
    if verdict=="skip":print(f"#{n}: {reason}: nothing done (the maintainer's word, as before)");return 1
    if verdict=="wait":print(f"#{n}: {reason}: run again later");return 4
    if verdict=="doubt":
        comment_doubt(n,reason,dry)
        print(f"TELL THE MAINTAINER: pull request #{n} not merged automatically: {REASONS[reason][0]}")
        return 3
    read=batch["read"];merged=[r for r in read if r.get("new")]
    # X-830 (D-227): the merge commit names no issue nor account; the tickets are commented one by one
    subject=f"Discoveries: a batch {AUTOMATIC} (trusted accounts)"
    body=compose(f"Every check of tools/discoveries-trust.py passed: {sum(r['facts'] for r in merged)} facts merged; {len(read)-len(merged)} gist(s) with nothing new listed as merged.",
                 f"Toutes les vérifications de tools/discoveries-trust.py sont passées : {sum(r['facts'] for r in merged)} faits fusionnés ; {len(read)-len(merged)} gist(s) sans rien de neuf notés comme fusionnés.")
    if dry:print(f"#{n} would be merged (squash, head {pr['sha'][:7]}): {subject}");return 0
    gh("pr","merge",str(n),"--repo",REPOSITORY,"--squash","--match-head-commit",pr["sha"],"--subject",subject,"--body",body)
    print(f"#{n} merged (head {pr['sha'][:7]}): {subject}")
    version=version_on_base() if merged else ""
    for r in merged:
        out=subprocess.run([sys.executable,"-I","-X","utf8",os.path.join(ROOT,"tools","discoveries-merge.py"),"notice",str(r["number"]),"merged",version,str(r["facts"])],
                           capture_output=True,text=True,timeout=TIMEOUT)
        print((out.stdout+out.stderr).strip() or f"#{r['number']}: notice gave nothing")
    for r in read:
        if not r.get("new"):print(f"#{r['number']}: nothing new merged: no « Merged » comment on the ticket (as sync does)")
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
    gist="0123456789abcdef0123";friend_gist="f"*20
    body=f"Shared from Tensho Assistant 0.1832.0\n\nGist: https://gist.github.com/{gist}\n\n- [x] I share these discoveries under CC0-1.0 (public domain).\n"
    issue={"number":43,"title":"Discoveries: Shigu330","body":body,"author":{"login":"Shigu330"},"labels":[{"name":"discoveries"}]}
    friend={"number":50,"title":"Discoveries: Friend","body":body.replace(gist,friend_gist),"author":{"login":"Friend"},"labels":[]}
    files=[("community-discoveries/players/0123456789abcdef.jsonl.gz",""),("community-discoveries/v3/index.json",""),("community-discoveries/v3/kind-01.jsonl.gz",""),
           (MERGED_FILE,"")]
    texts=list(m.batch_text("0a1b2c3d").values())
    def pr(**k):return {"head":"travailleur/discoveries-batch-0a1b2c3d","base":BASE,"state":"OPEN","draft":False,"mergeable":"MERGEABLE","files":files,"changed":4,
                        "checks":[("Build and test","COMPLETED","SUCCESS"),("CodeQL","COMPLETED","SUCCESS")],
                        "cross":False,"head_repo":REPOSITORY,"author":WORKER,"texts":texts,**k}
    def share(**k):return {"number":43,"login":"Shigu330","owner":"shigu330","gist":gist,"facts":100,"refused":5,"kinds":[1,3],"flag":False,"new":True,**k}
    theirs=share(number=50,login="Friend",owner="Friend",gist=friend_gist)
    def batch(read=None,**k):
        return {"issues":[issue,friend],"links":True,"read":[share()] if read is None else read,
                "tickets":{43:{"state":"OPEN","issue":issue},50:{"state":"OPEN","issue":friend}},"content":"same",**k}
    trusted={"shigu330"}
    check(decide(trusted,pr(),batch())==("merge","every check passed"),"a batch of a trusted account, every check green: merged")
    check(decide(trusted,pr(),batch([share(),theirs]))[0]=="skip" and decide(trusted|{"friend"},pr(),batch([share(),theirs]))==("merge","every check passed"),
          "X-830: a batch of two shares: merged only when every author is trusted")
    check(decide(set(),pr(),batch())[0]=="skip" and decide({"someone"},pr(),batch())[0]=="skip","an account not on the list: nothing done")
    check(decide(trusted,pr(),batch([share(login="Mallory",owner="Mallory")]))[0]=="skip","the ticket's author decides, never the pull request's")
    check(decide(trusted,pr(head="travailleur/discoveries-43"),batch())[0]=="skip" and decide(trusted,pr(head="feature/x"),batch())[0]=="skip"
          and decide(trusted,pr(head="travailleur/discoveries-batch-0a1b2c3d-43"),batch())[0]=="skip","X-830: only a batch's branch, naming no issue")
    check(decide(trusted,pr(checks=[("Build and test","IN_PROGRESS","")]),batch())[0]=="wait","its CI still runs: wait")
    check(decide(trusted,pr(checks=[]),batch())[0]=="wait","no CI yet: wait")
    # X-824 M2: passing states wait, never a public doubt
    check(decide(trusted,pr(mergeable="UNKNOWN"),batch())[0]=="wait","X-824 M2: mergeable not computed yet: wait")
    rows=[check_row(c) for c in ({"__typename":"CheckRun","workflowName":"Build and test","status":"COMPLETED","conclusion":"SUCCESS"},
                                 {"__typename":"StatusContext","context":"ci/other","state":"PENDING"},{"context":"ci/expected","state":"EXPECTED"})]
    check(rows[1][1]=="PENDING" and rows[2][1]=="PENDING" and decide(trusted,pr(checks=rows),batch())[0]=="merge",
          f"X-824 M2: a commit status still pending is no failure: {rows}")
    check(check_row({"workflowName":"Build and test"})[1]=="PENDING" and decide(trusted,pr(checks=[check_row({"workflowName":"Build and test"})]),batch())[0]=="wait",
          "X-824 M2: a check run without its status is not finished: wait")
    failed=[check_row(c) for c in ({"workflowName":"Build and test","status":"COMPLETED","conclusion":"SUCCESS"},{"context":"ci/other","state":"ERROR"})]
    check(decide(trusted,pr(checks=failed),batch())==("doubt","ci"),"X-824 M2: a commit status in error is still a failure")
    closed={**issue,"state":"CLOSED"}
    reds={
        "pr-origin (a fork)":(pr(cross=True,head_repo="Mallory/Tensho-Assistant"),batch()),
        "pr-origin (a fork that names the repository)":(pr(cross=True),batch()),
        "pr-origin (not said)":(pr(cross=None),batch()),
        "pr-origin (another repository)":(pr(head_repo="Mallory/Tensho-Assistant"),batch()),
        "pr-origin (deleted head repository)":(pr(head_repo=""),batch()),
        "pr-origin (another author)":(pr(author="Mallory"),batch()),
        "pr-state":(pr(draft=True),batch()),
        "pr-base":(pr(base="main"),batch()),
        "pr-conflict":(pr(mergeable="CONFLICTING"),batch()),
        "pr-files":(pr(files=files+[("src/TenshoAssistant/Plugin.cs","")],changed=5),batch()),
        "pr-files (cut list)":(pr(changed=5),batch()),
        "pr-files (a player file named after its issue)":(pr(files=[("community-discoveries/players/43.jsonl.gz","")]+files,changed=5),batch()),
        "pr-files (renamed from outside)":(pr(files=[(files[0][0],"docs/x.md")]+files[1:]),batch()),
        "pr-names (its title)":(pr(texts=[*texts[:1],"Discoveries: merge issue #43",*texts[2:]]),batch()),
        "pr-names (its body)":(pr(texts=[*texts,"Shared by shigu330"]),batch()),
        "pr-names (another open share's account)":(pr(texts=[*texts,"with Friend"]),batch()),
        "pr-names (a bare issue number in a commit)":(pr(texts=[*texts,"merge 43"]),batch()),
        "links":(pr(),batch(links=False)),
        "facts":(pr(),batch([])),
        "ticket":(pr(),batch(tickets={43:{"state":"CLOSED","issue":closed}})),
        "ticket (cc0)":(pr(),batch(tickets={43:{"state":"OPEN","issue":{**issue,"body":body.replace("[x]","[ ]")}}})),
        "ticket (title)":(pr(),batch(tickets={43:{"state":"OPEN","issue":{**issue,"title":"Discoveries: Bob"}}})),
        "ticket (missing)":(pr(),batch(tickets={})),
        "gist":(pr(),batch([share(gist=friend_gist)])),
        "owner":(pr(),batch([share(owner="Bob")])),
        "refused":(pr(),batch([share(facts=90,refused=10)])),
        "volume":(pr(),batch([share(facts=PLAUSIBLE_FACTS+1,refused=0)])),
        "volume (empty)":(pr(),batch([share(facts=0,refused=0)])),
        "kinds":(pr(),batch([share(kinds=[1,26])])),
        "content":(pr(),batch(content="community-discoveries/v3/kind-01.jsonl.gz: differs")),
        "content (never replayed)":(pr(),{k:v for k,v in batch().items() if k!="content"}),
        "ci":(pr(checks=[("Build and test","COMPLETED","FAILURE")]),batch()),
        "ci (another check)":(pr(checks=[("Build and test","COMPLETED","SUCCESS"),("CodeQL","COMPLETED","FAILURE")]),batch()),
    }
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
    check(allowed("community-discoveries/v3/kind-07.jsonl.gz") and allowed(MERGED_FILE) and allowed("community-discoveries/players/0123456789abcdef.jsonl.gz")
          and not allowed("community-discoveries/v3/../x") and not allowed("community-discoveries/players/43.jsonl.gz")
          and not allowed("community-discoveries/players/withdrawn-0123456789abcdef.jsonl.gz"),"allowed paths: anonymous player files only")
    end_to_end(check)
    print(f"discoveries-trust selftest: {ok}/{total} ok");return 0 if ok==total else 1

def fake_tree(trees,tid,limit=None):
    """end_to_end, X-827: what GitHub's git trees API answers for a made-up tree id « commit|folder » (a commit: its root tree), from
    {commit: {path: bytes}}; limit: past that many entries, « truncated » as GitHub says it."""
    ref,_,prefix=tid.partition("|");found={}
    for path,data in trees[ref].items():
        if prefix and not path.startswith(prefix+"/"):continue
        rest=path[len(prefix)+1:] if prefix else path
        name,_,deeper=rest.partition("/")
        found[name]={"path":name,"mode":"040000","type":"tree","sha":f"{ref}|{prefix+'/' if prefix else ''}{name}"} if deeper else \
                    {"path":name,"mode":"100644","type":"blob","sha":blob_sha(data)}
    listed=list(found.values())
    return json.dumps({"sha":tid,"tree":listed[:limit] if limit else listed,"truncated":bool(limit and len(listed)>limit)})

class Merged(Exception):
    """end_to_end: the made-up GitHub was asked to merge."""

def end_to_end(check):
    """X-823, X-830: merge() against a made-up GitHub (gh and the gist reader replaced, nothing sent): a batch replayed by the real merge tool
    with the private link; a fork, another author, a file out of scope, a common set, player file or list of merged gists changed by value, a
    file more, a pull request naming the issue, no link file: never merged."""
    import contextlib,gzip,io
    m=tool()
    gist="0123456789abcdef0123";login="Shigu330";base_sha="2"*40;head_sha="1"*40;fid="0123456789abcdef"
    body=f"Shared from Tensho Assistant 0.1832.0\n\nGist: https://gist.github.com/{gist}\n\n- [x] I share these discoveries under CC0-1.0 (public domain).\n"
    issue={"number":43,"title":f"Discoveries: {login}","body":body,"author":{"login":login},"labels":[{"name":"discoveries"}],"state":"OPEN"}
    links={"43":{"File":fid,"Login":login}}
    def raw(subject,level=1):return {"Kind":1,"Subject":subject,"Detail":0,"Territory":128,"Map":1,"CellX":1,"CellZ":2,"X":60.0,"Z":140.0,
                                     "LevelMin":1,"LevelMax":level,"Seen":2,"Imported":0,"First":"2026-11-10T00:00:00+00:00","Last":"2026-11-12T00:00:00+00:00"}
    def at(revision,*facts):return (lambda g:(login,{"id":g},revision)),(lambda files:{"Version":2,"Facts":[dict(f) for f in facts]})
    base,_=replay({},[issue],links,*at("r1",raw(1)))
    good,read=replay(base,[issue],links,*at("r2",raw(2)))
    kind=f"{FOLDERS[1]}/kind-01.jsonl.gz";index=f"{FOLDERS[1]}/{m.V3_INDEX}";mine=f"{FOLDERS[0]}/{fid}{m.PLAYER_SUFFIX}"
    check(content_doubt(good,dict(good)) is None and set(good)=={kind,index,mine,MERGED_FILE} and [r["number"] for r in read]==[43]
          and m.note_hash(gist,"r2") in good[MERGED_FILE].decode("utf-8"),f"the replay writes the anonymous player file, the common set and the merged list ({sorted(good)})")
    check(login.encode() not in gzip.decompress(good[mine]) and b"43" not in gzip.decompress(good[mine]).split(b"\n")[0],"X-830: the replayed player file names no account nor issue")
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
    text=m.batch_text("0a1b2c3d")
    def pr_info(**k):return {"headRefName":text["branch"],"baseRefName":BASE,"isDraft":False,"mergeable":"MERGEABLE","headRefOid":head_sha,
                             "state":"OPEN","isCrossRepository":False,"headRepository":{"name":"Tensho-Assistant"},"headRepositoryOwner":{"login":"Shigu330"},
                             "author":{"login":WORKER},"statusCheckRollup":[{"workflowName":REQUIRED,"status":"COMPLETED","conclusion":"SUCCESS"}],
                             "title":text["title"],"body":text["body"],"commits":[{"messageHeadline":text["commit"],"messageBody":""}],**k}
    def run(head,info=None,extra=(),dry=True,start=None,linked=True):
        """merge() on this head ({path: bytes}), Devloppement holding start (base by default); → (exit code, printed text, the gh calls made)."""
        base_=start or base
        trees={base_sha:base_,head_sha:head};store={blob_sha(d):d for t in trees.values() for d in t.values()}
        changed=sorted(p for p in set(base_)|set(head) if base_.get(p)!=head.get(p))+list(extra)
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
            if "/git/trees/" in url:return fake_tree(trees,url.rsplit("/git/trees/",1)[1])
            if "/contents/" in url:
                path,ref=url.split("/contents/",1)[1].split("?ref=");tree=trees[ref]
                if path in tree:return blob_sha(tree[path])+"\n"
                raise RuntimeError("gh api: HTTP 404: Not Found")
            raise RuntimeError(f"unexpected gh call: {a[:3]}")
        global gh
        saved=(gh,m.gist_listing,m.from_gist_files)
        gh=fake;m.gist_listing=lambda g:(login,{"id":g},"r2");m.from_gist_files=lambda files,fetch:{"Version":2,"Facts":[raw(2)]}
        out=io.StringIO()
        try:
            with tempfile.TemporaryDirectory() as folder, contextlib.redirect_stdout(out):
                path=os.path.join(folder,"trusted-accounts.txt");linked_path=os.path.join(folder,"links.json")
                with open(path,"w",encoding="utf-8") as stream:stream.write(login+"\n")
                if linked:m.write_links(links,linked_path)
                try:code=merge(7,path,dry,linked_path)
                except Merged:code="merged"
        finally:gh,m.gist_listing,m.from_gist_files=saved
        return code,out.getvalue(),calls
    def merged_call(calls):return any(c[:2]==("pr","merge") for c in calls)
    code,said,_=run(good)
    check(code==0 and "would be merged" in said,f"the worker's own batch, replayed whole: merged ({code}: {said[-200:]})")
    code,said,calls=run(good,dry=False)
    merge_call=[c for c in calls if c[:2]==("pr","merge")]
    check(code=="merged" and merge_call and not m.names_in([merge_call[0][merge_call[0].index("--subject")+1],merge_call[0][merge_call[0].index("--body")+1]],[issue]),f"X-830: the merge commit names no issue nor account ({merge_call[:1]})")
    code,said,_=run(reindexed({**good,kind:repacked(good[kind],lambda n,line:line)}))
    check(code==0,f"the same text packed by another compressor, its index naming it: merged ({code}: {said[-200:]})")
    cases={
        "a fork":(good,{"isCrossRepository":True,"headRepositoryOwner":{"login":"Mallory"}},(),"pr-origin"),
        "another author":(good,{"author":{"login":"Mallory"}},(),"pr-origin"),
        "a file out of scope":(good,None,("src/TenshoAssistant/Plugin.cs",),"pr-files"),
        "a title naming the issue":(good,{"title":"Discoveries: merge issue #43"},(),"pr-names"),
        "a commit naming the account":(good,{"commits":[{"messageHeadline":"Discoveries of Shigu330","messageBody":""}]},(),"pr-names"),
        "the common set changed by value":(reindexed({**good,kind:repacked(good[kind],level99)}),None,(),"content"),
        "the player file changed by value":({**good,mine:repacked(good[mine],level99)},None,(),"content"),
        "the merged list changed":({**good,MERGED_FILE:good[MERGED_FILE]+("0"*64+"\n").encode()},None,(),"content"),
        "a kind file more":(reindexed({**good,f"{FOLDERS[1]}/kind-05.jsonl.gz":good[kind]}),None,(),"content"),
        "a kind file missing":({p:d for p,d in good.items() if p!=kind},None,(),"content"),
        "an index naming other bytes":({**good,kind:repacked(good[kind],lambda n,line:line)},None,(),"content"),
    }
    for name,(head,info,extra,reason) in cases.items():
        code,said,calls=run(head,info,extra,dry=False)
        check(code==3 and REASONS[reason][0] in said and not merged_call(calls) and any(c[:2]==("pr","comment") for c in calls),
              f"X-823, X-830: {name}: a doubt, never merged ({code}: {said[-240:]})")
    code,said,calls=run(good,dry=False,linked=False)
    check(code==3 and REASONS["links"][0] in said and not merged_call(calls),f"X-830: no private link file: a doubt ({code}: {said[-200:]})")
    # X-825, X-830: a share whose gist brings nothing new (Devloppement holds its facts, its revision not listed as merged): the batch changes the
    # merged list only (so the plugin deletes that gist), merged without a « Merged » comment on the ticket.
    start={**good,MERGED_FILE:base[MERGED_FILE]}
    noted,read=replay(start,[issue],links,*at("r2",raw(2)))
    check({p for p in noted if noted[p]!=start.get(p)}=={MERGED_FILE} and read and read[0]["new"] is False,
          f"X-825: the replay of a gist with nothing new changes the merged list only ({sorted(p for p in noted if noted[p]!=start.get(p))})")
    code,said,calls=run(noted,dry=False,start=start)
    check(code=="merged" and not any(c[:2]==("pr","comment") for c in calls),f"X-825: nothing new, the merged list alone: merged, nothing commented ({code}: {said[-240:]})")
    code,said,calls=run({**noted,mine:repacked(noted[mine],level99)},dry=False,start=start)
    check(code==3 and REASONS["content"][0] in said and not merged_call(calls),f"X-825: the merged list and a fact changed: a doubt ({code}: {said[-240:]})")
    # X-827: past 1 000 player files (the contents API's bound) every file listed; a tree GitHub cuts short, a link or a folder: refused.
    many={**good,**{f"{FOLDERS[0]}/{n}{m.PLAYER_SUFFIX}":str(n).encode() for n in range(1000,2200)},"docs/x.md":b"x"}
    global gh
    saved=gh
    def listing(tree,limit=None,mode=None):
        global gh
        def fake(*args,raw=False):
            tid=args[1].rsplit("/git/trees/",1)[1];answer=json.loads(fake_tree({"c":tree},tid,limit))
            if mode:answer["tree"]=[{**e,"mode":mode} if e["type"]=="blob" else e for e in answer["tree"]]
            return json.dumps(answer)
        gh=fake
        try:return folder_blobs("c")
        except RuntimeError as error:return str(error)
        finally:gh=saved
    listed=listing(many)
    check(isinstance(listed,dict) and len([p for p in listed if p.startswith(FOLDERS[0]+"/")])==1201 and kind in listed and "docs/x.md" not in listed
          and all(listed[p]==blob_sha(d) for p,d in many.items() if p in listed),f"X-827: past 1 000 player files, every one listed with its blob ({len(listed)})")
    check(listing(many,limit=1000)=="tree c|community-discoveries/players: truncated or not a tree",f"X-827: a tree GitHub says is truncated: refused, never a partial list ({listing(many,limit=1000)})")
    check(str(listing(good,mode="120000")).endswith("not a plain file") and str(listing({**good,f"{FOLDERS[0]}/deeper/x":b"x"})).endswith("not a plain file"),
          "X-827: a link or a folder where a sync writes plain files: refused")
    check(listing({"docs/x.md":b"x"})=={},"X-827: no community-discoveries folder: nothing listed")

if __name__=="__main__":
    for stream in (sys.stdout,sys.stderr):
        if hasattr(stream,"reconfigure"):stream.reconfigure(encoding="utf-8",errors="replace")
    argv=sys.argv[1:];dry="--dry-run" in argv;argv=[a for a in argv if a!="--dry-run"]
    path=DEFAULT_LIST;links_path=None
    if "--list" in argv:
        at=argv.index("--list")
        if at+1>=len(argv):print(__doc__);sys.exit(2)
        path=argv[at+1];del argv[at:at+2]
    if "--links" in argv:   # X-830: the private link file (default: the merge tool's LINKS_PATH)
        at=argv.index("--links")
        if at+1>=len(argv):print(__doc__);sys.exit(2)
        links_path=argv[at+1];del argv[at:at+2]
    if argv==["--selftest"]:sys.exit(selftest())
    if argv==["init"]:sys.exit(init(path))
    if len(argv)==2 and argv[0]=="merge" and argv[1].isdigit():
        try:sys.exit(merge(int(argv[1]),path,dry,links_path))
        except (RuntimeError,subprocess.SubprocessError,OSError,ValueError) as error:
            print(f"TELL THE MAINTAINER: pull request #{argv[1]} not merged automatically: {type(error).__name__}: {str(error)[:200]}");sys.exit(3)
    print(__doc__);sys.exit(2)
