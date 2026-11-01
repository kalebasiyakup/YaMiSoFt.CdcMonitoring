#!/usr/bin/env python3
"""vault-lint — run the kit's LINT protocol for real.

`skills/llm-wiki/lint.md` specifies the checks and the health-score formula.
This script implements that spec: no dependencies, no network, read-only by
default. Point it at a vault and it prints the report format the skill asks for.

    python3 vault-lint.py path/to/vault
    python3 vault-lint.py . --json
    python3 vault-lint.py . --fields tur,durum,guncelleme --conflict-callout conflict

Read-only unless you pass --write-report.
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import os
import re
import subprocess
import sys
from collections import Counter, defaultdict
from datetime import date, datetime, timedelta

# --- link + frontmatter parsing -------------------------------------------------

FENCE = re.compile(r"^(\s*)(```|~~~)")
INLINE_CODE = re.compile(r"`[^`\n]*`")
WIKILINK = re.compile(r"\[\[([^\[\]]+?)\]\]")
FM_DELIM = "---"

# v3 eklentisi: tazelik. Kural OKM (Open Knowledge Metabolism, Eugeniu Ghelbur,
# MIT) şartnamesinden alındı: her saklanan olgu zamansız, tarihli ya da işaretçi
# olmak zorunda. Oradaki düzyazı tarayıcısı İngilizce oynak isim listelerine
# dayanıyor ve kendi şartnamesi de yanlış pozitif vereceğini kabul ediyor, bu
# yüzden buraya alınmadı. Buradaki üç kontrol sezgisel değil: frontmatter'da
# beyan, işaretçi hedefi, damga yaşı.
STAMP = re.compile(
    r"\(\s*(?:as\s+of\s+(?P<a>\d{4}-\d{2}(?:-\d{2})?)"
    r"|(?P<b>\d{4}-\d{2}(?:-\d{2})?)\s+itibar[iı]yla)\s*[^)]*\)", re.I)
POINTER_LINE = re.compile(
    r"^\s*(?:[-*]\s*)?\*\*\s*(?:Lives\s+(?:at|in)|Nerede\s+ya[şs][iı]yor)\s*:?\s*\*\*(?P<rest>.*)$",
    re.I | re.M)
URL = re.compile(r"https?://\S+")
FRESHNESS_VALUES = ("wiring", "snapshot", "pointer")

# Obsidian ignores links inside code. A linter that does not is a linter that
# cries wolf: `[[link]]` written as an example is not a broken link.
def strip_code(text: str) -> str:
    out, in_fence, fence_tok = [], False, ""
    for line in text.splitlines():
        m = FENCE.match(line)
        if m:
            tok = m.group(2)
            if not in_fence:
                in_fence, fence_tok = True, tok
            elif tok == fence_tok:
                in_fence = False
            out.append("")
            continue
        out.append("" if in_fence else INLINE_CODE.sub("", line))
    return "\n".join(out)


def parse_frontmatter(text: str) -> dict:
    lines = text.splitlines()
    if not lines or lines[0].strip() != FM_DELIM:
        return {}
    body: list[str] = []
    for line in lines[1:]:
        if line.strip() == FM_DELIM:
            break
        body.append(line)
    fm: dict[str, object] = {}
    key = None
    for line in body:
        if re.match(r"^\s*-\s+", line) and key:
            fm.setdefault(key, [])
            if isinstance(fm[key], list):
                fm[key].append(re.sub(r"^\s*-\s+", "", line).strip())
            continue
        m = re.match(r"^([A-Za-z_][\w-]*)\s*:\s*(.*)$", line)
        if m:
            key, val = m.group(1), m.group(2).strip()
            fm[key] = val if val else []
    return fm


def link_targets(text: str):
    """Yield (target, raw) for each wikilink, normalised the way Obsidian resolves."""
    for m in WIKILINK.finditer(strip_code(text)):
        raw = m.group(1)
        # alias: [[target|Alias]]. Inside a markdown table the pipe is written
        # \| to escape the cell separator, but it is still the alias pipe.
        target = raw.replace("\\|", "|").split("|")[0].strip()
        target = target.split("#")[0].split("^")[0].strip()
        if target:
            yield target, raw


# --- vault model ----------------------------------------------------------------

class Vault:
    """Two distinct sets, and the difference is the whole point when you retrofit:

    - `notes`     — what gets audited (default: every .md; narrow it with --notes)
    - `all_files` — what links resolve against (always the whole vault)

    Point a linter at a folder holding six months of raw material and it will
    scold you for every `[[example]]` in a tutorial you saved. Audit the wiki
    layer, resolve against everything.
    """

    def __init__(self, root: str, excludes: list[str], note_globs: list[str] | None = None):
        self.root = os.path.abspath(root)
        self.excludes = excludes + [".obsidian/*", ".git/*", "node_modules/*"]
        self.note_globs = note_globs or []
        self.notes: dict[str, str] = {}          # audited
        self.all_files: set[str] = set()         # resolvable
        self._load()

    def _audited(self, rel: str) -> bool:
        if not self.note_globs:
            return True
        r = rel.replace(os.sep, "/")
        return any(fnmatch.fnmatch(r, g) or r.startswith(g.rstrip("*").rstrip("/") + "/")
                   for g in self.note_globs)

    def _excluded(self, rel: str) -> bool:
        return any(fnmatch.fnmatch(rel, p) or rel.startswith(p.rstrip("*"))
                   for p in self.excludes)

    def _load(self):
        for dirpath, dirnames, filenames in os.walk(self.root):
            dirnames[:] = [d for d in dirnames if d not in (".git", ".obsidian", "node_modules")]
            for fn in filenames:
                full = os.path.join(dirpath, fn)
                rel = os.path.relpath(full, self.root)
                if self._excluded(rel):
                    continue
                self.all_files.add(rel)
                if fn.endswith(".md") and self._audited(rel):
                    try:
                        self.notes[rel] = open(full, encoding="utf-8", errors="replace").read()
                    except OSError:
                        pass

    def resolve(self, target: str, from_rel: str = "") -> tuple:
        """Mimic Obsidian's resolution. Returns (path_or_None, ambiguous).

        Obsidian does not pick at random when several files share a basename --
        it prefers the closest one. Neither should a linter: choosing arbitrarily
        reports phantom orphans for the page it did not choose.

        Order: exact relative path -> same folder as the linking note -> shallowest
        path -> lexicographic. `ambiguous` is True when the basename was not unique.
        """
        norm = target.replace("\\", "/").lower()
        for cand in (norm, norm + ".md"):
            for rel in sorted(self.all_files):
                if rel.replace(os.sep, "/").lower() == cand:
                    return rel, False

        base = os.path.basename(norm)
        matches = []
        for rel in self.all_files:
            name = os.path.basename(rel).lower()
            stem = name[:-3] if name.endswith(".md") else name
            if stem == base or name == base:
                matches.append(rel)
        if not matches:
            return None, False
        if len(matches) == 1:
            return matches[0], False

        here = os.path.dirname(from_rel)
        matches.sort(key=lambda r: (os.path.dirname(r) != here, r.count(os.sep), r.lower()))
        return matches[0], True


# --- checks ---------------------------------------------------------------------

def run(v: Vault, args) -> dict:
    required = [f.strip() for f in args.fields.split(",") if f.strip()]
    src_fields = [f.strip() for f in args.source_fields.split(",") if f.strip()]

    inbound: dict[str, set[str]] = defaultdict(set)
    broken: list[tuple[str, str]] = []
    ambiguous: Counter = Counter()
    unresolved_counts: Counter = Counter()
    total_links = 0

    for rel, text in v.notes.items():
        for target, _raw in link_targets(text):
            total_links += 1
            hit, amb = v.resolve(target, rel)
            if amb:
                ambiguous[target] += 1
            if hit:
                if hit != rel:
                    inbound[hit].add(rel)
            else:
                broken.append((rel, target))
                unresolved_counts[target] += 1

    orphans = sorted(r for r in v.notes if not inbound.get(r) and not _is_entry(r))

    fm_violations = []
    stale = []
    bad_sources = []
    cutoff = date.today() - timedelta(days=args.stale_days)
    for rel, text in v.notes.items():
        fm = parse_frontmatter(text)
        missing = [] if _is_entry(rel) else [f for f in required if f not in fm]
        if missing:
            fm_violations.append((rel, missing))
        d = _as_date(fm.get(args.date_field) if args.date_field in fm else fm.get("date"))
        status = str(fm.get("status") or fm.get("durum") or "").strip().lower()
        if d and d < cutoff and status in args.stale_status.split(","):
            stale.append((rel, d.isoformat(), status))
        for sf in src_fields:
            val = fm.get(sf)
            for p in (val if isinstance(val, list) else [val] if val else []):
                p = str(p).strip().strip("'\"")
                if not p or p.startswith(("http://", "https://")):
                    continue
                # "user | agent | doc" or "<where this came from>" are placeholders,
                # not paths. Do not report a template as a fabricated citation.
                if "|" in p or p.startswith("<") or " or " in p:
                    continue
                # External references (absolute/home paths, "Label: /some/where")
                # are outside the vault by definition. Only vault-relative paths
                # are claims this tool can check.
                if p.startswith(("~", "/")) or ": " in p:
                    continue
                # `source:` may hold provenance ("user", "planner") rather than a
                # location. Only verify values that look like a path: a separator,
                # or a file extension.
                if "/" not in p and not re.search(r"\.[A-Za-z0-9]{1,5}$", p):
                    continue
                if not os.path.exists(os.path.join(v.root, p)):
                    bad_sources.append((rel, p))

    missing_concepts = [(t, c) for t, c in unresolved_counts.most_common()
                        if c >= args.concept_threshold]

    conflicts = []
    pat = re.compile(r">\s*\[!" + re.escape(args.conflict_callout) + r"\]", re.I)
    for rel, text in v.notes.items():
        n = len(pat.findall(text))
        if n:
            conflicts.append((rel, n))

    hubs = sorted(((len(s), r) for r, s in inbound.items()), reverse=True)[:10]

    fresh = check_freshness(v, args)

    score = max(0, min(100, 100
                       - len(broken) * 10
                       - len(orphans) * 3
                       - len(stale) * 1
                       - len(missing_concepts) * 5
                       - (sum(n for _, n in conflicts) * 20 if args.score_conflicts else 0)
                       - len(fm_violations) * 5
                       - len(bad_sources) * 10
                       - len(fresh["dangling_pointers"]) * 10
                       - len(fresh["freshness_undeclared"]) * 5
                       - len(fresh["stale_stamps"]) * 1))

    return {
        "vault": v.root,
        "generated": date.today().isoformat(),
        "notes": len(v.notes),
        "files": len(v.all_files),
        "links": total_links,
        "links_per_note": round(total_links / len(v.notes), 1) if v.notes else 0,
        "score": score,
        "broken": broken,
        "orphans": orphans,
        "stale": stale,
        "missing_concepts": missing_concepts,
        "frontmatter_violations": fm_violations,
        "conflicts": conflicts,
        "bad_sources": bad_sources,
        "freshness_undeclared": fresh["freshness_undeclared"],
        "dangling_pointers": fresh["dangling_pointers"],
        "stale_stamps": fresh["stale_stamps"],
        "hubs": hubs,
        "ambiguous": ambiguous.most_common(),
    }


def _is_entry(rel: str) -> bool:
    return os.path.basename(rel).lower() in ("index.md", "readme.md", "log.md", "moc.md")


def _as_date(val):
    if not val or isinstance(val, list):
        return None
    m = re.search(r"(\d{4})-(\d{2})-(\d{2})", str(val))
    if not m:
        return None
    try:
        return datetime(int(m.group(1)), int(m.group(2)), int(m.group(3))).date()
    except ValueError:
        return None


def check_freshness(v: Vault, args) -> dict:
    """Tazelik: her olgu zamansız, tarihli ya da işaretçi.

    Üç kontrol, hiçbiri düzyazı sezgisi değil:

    - `freshness` beyan edilmemiş. Hızlı olgudan yapılan sayfa türleri (varsayılan:
      entity) alanı taşımak zorunda. Yazar sayfa başına bir kez karar verir.
    - İşaretçi hedefi çözülmüyor. `freshness: pointer` sayfası, hedefi URL ya da
      var olan bir vault sayfası olan bir "Lives at" satırı taşımak zorunda.
    - Damga eskimiş. Gövdedeki `(as of 2026-11-01)` damgası eşiği geçmişse
      ya yeniden gözle ya işaretçiye çevir.

    Arşiv ve `status: deprecated` sayfalar muaftır: onlar dondurulmuş tarihtir.
    """
    undeclared: list = []
    dangling: list = []
    stale_stamps: set = set()
    if args.no_freshness:
        return {"freshness_undeclared": [], "dangling_pointers": [], "stale_stamps": []}

    required_types = [t.strip().lower() for t in args.freshness_required.split(",") if t.strip()]
    cutoff = date.today() - timedelta(days=args.stamp_days)

    for rel, text in v.notes.items():
        if _is_entry(rel):
            continue
        fm = parse_frontmatter(text)
        status = str(fm.get("status") or "").strip().lower()
        if status == "deprecated" or rel.replace(os.sep, "/").startswith("wiki/archive/"):
            continue

        declared = str(fm.get(args.freshness_field) or "").strip().lower()
        body = strip_code(text)

        if declared not in FRESHNESS_VALUES:
            if str(fm.get("type") or "").strip().lower() in required_types:
                undeclared.append((rel, declared or "none"))
        elif declared == "pointer":
            if not _pointer_resolves(v, rel, body):
                dangling.append(rel)

        if declared != "snapshot":
            for m in STAMP.finditer(body):
                raw = m.group("a") or m.group("b")
                d = _as_date(raw if len(raw) > 7 else raw + "-01")
                if d and d < cutoff:
                    stale_stamps.add((rel, raw))

    return {"freshness_undeclared": undeclared,
            "dangling_pointers": dangling,
            "stale_stamps": sorted(stale_stamps)}


def _pointer_resolves(v: Vault, rel: str, body: str) -> bool:
    for m in POINTER_LINE.finditer(body):
        rest = m.group("rest")
        if URL.search(rest):
            return True
        for target, _raw in link_targets(rest):
            hit, _amb = v.resolve(target, rel)
            if hit:
                return True
    return False


# --- report ---------------------------------------------------------------------

BANDS = [(90, "✅ Healthy"), (70, "⚠️ Minor debt"), (50, "⚠️ Needs attention"), (0, "🚨 Cleanup overdue")]


def band(score: int) -> str:
    return next(label for threshold, label in BANDS if score >= threshold)


def render(r: dict) -> str:
    o = []
    a = o.append
    a(f"# Vault Health Report — {r['generated']}\n")
    a(f"**Vault:** `{r['vault']}`  ")
    a(f"**Total notes:** {r['notes']}  ")
    a(f"**Total links:** {r['links']} ({r['links_per_note']} per note)  ")
    a(f"**Health score:** {r['score']} / 100 — {band(r['score'])}\n")

    if r["conflicts"]:
        total = sum(n for _, n in r["conflicts"])
        a(f"## CRITICAL\n\n### Flagged contradictions ({total} across {len(r['conflicts'])} notes)\n")
        a("These are *marked*, not silently resolved — that is the protocol working. "
          "They still need a human decision.\n")
        for rel, n in sorted(r["conflicts"], key=lambda x: -x[1])[:15]:
            a(f"- `{rel}` — {n}")
        a("")

    if r["broken"] or r["bad_sources"] or r["dangling_pointers"]:
        a("## HIGH\n")
    if r["broken"]:
        a(f"### Broken links ({len(r['broken'])})\n")
        for rel, t in r["broken"][:25]:
            a(f"- `{rel}` → `[[{t}]]`\n  - *Suggestion: create the stub, or fix the link text.*")
        if len(r["broken"]) > 25:
            a(f"- …and {len(r['broken']) - 25} more")
        a("")
    if r["bad_sources"]:
        a(f"### Unverifiable sources ({len(r['bad_sources'])})\n")
        a("A note cites a source path that does not exist on disk. This is how you catch "
          "a citation the agent invented.\n")
        for rel, p in r["bad_sources"][:25]:
            a(f"- `{rel}` cites `{p}` — not found")
        a("")
    if r["dangling_pointers"]:
        a(f"### Unresolved pointer ({len(r['dangling_pointers'])})\n")
        a("A `freshness: pointer` page has no \"Lives at\" line whose target is a URL or an "
          "existing page. A pointer without a target does not say where the fact lives, "
          "it only claims to.\n")
        for rel in r["dangling_pointers"][:25]:
            a(f"- `{rel}` — target missing or does not resolve")
        a("")

    if r["orphans"] or r["missing_concepts"] or r["frontmatter_violations"] \
            or r["freshness_undeclared"]:
        a("## MEDIUM\n")
    if r["orphans"]:
        a(f"### Orphan notes ({len(r['orphans'])})\n")
        for rel in r["orphans"][:25]:
            a(f"- `{rel}` — no incoming links\n  - *Suggestion: link from a parent concept, or archive.*")
        a("")
    if r["missing_concepts"]:
        a(f"### Missing concept pages ({len(r['missing_concepts'])})\n")
        for t, c in r["missing_concepts"][:25]:
            a(f"- `[[{t}]]` — referenced {c}×, no page\n  - *Suggestion: INGEST a page synthesising existing references.*")
        a("")
    if r["frontmatter_violations"]:
        a(f"### Frontmatter violations ({len(r['frontmatter_violations'])})\n")
        for rel, missing in r["frontmatter_violations"][:25]:
            a(f"- `{rel}` — missing: {', '.join(missing)}")
        a("")
    if r["freshness_undeclared"]:
        a(f"### Freshness not declared ({len(r['freshness_undeclared'])})\n")
        a("Page types built from fast facts must carry a `freshness` field: "
          "`wiring` (timeless), `snapshot` (dated observation) or `pointer` "
          "(the fact lives in its own system).\n")
        for rel, val in r["freshness_undeclared"][:25]:
            a(f"- `{rel}` — `freshness: {val}`")
        a("")

    if r["ambiguous"]:
        a("### Ambiguous link targets (%d)\n" % len(r["ambiguous"]))
        a("Several files share this basename, so `[[name]]` resolves by proximity - "
          "which may not be the file you meant. Disambiguate with a path link.\n")
        for t, c in r["ambiguous"][:15]:
            a("- `[[%s]]` - %d x ambiguous" % (t, c))
        a("")

    if r["stale"] or r["stale_stamps"]:
        a("## LOW\n")
    if r["stale"]:
        a(f"### Stale notes ({len(r['stale'])})\n")
        for rel, d, st in r["stale"][:25]:
            a(f"- `{rel}` — {d}, status `{st}`\n  - *Suggestion: accept, deprecate, or update.*")
        a("")
    if r["stale_stamps"]:
        a(f"### Stale stamp ({len(r['stale_stamps'])})\n")
        a("The stamp is past the threshold. Three answers: re-observe and update the stamp, "
          "turn it into a pointer, or rewrite the sentence as a dated observation. "
          "Deleting is not an answer.\n")
        for rel, raw in r["stale_stamps"][:25]:
            a(f"- `{rel}` — `{raw}`")
        a("")

    if r["hubs"]:
        a("## Hubs — most linked-to notes\n")
        a("| Note | Inbound |")
        a("|---|---|")
        for n, rel in r["hubs"]:
            a(f"| `{rel}` | {n} |")
        a("")

    a("---\n")
    a("Generated by [`vault-lint.py`](scripts/vault-lint.py) — "
      "[claude-obsidian-kit](https://github.com/selmakcby/claude-obsidian-kit)")
    return "\n".join(o)


def _git(cwd: str, cmd: list) -> str | None:
    try:
        res = subprocess.run(["git"] + cmd, cwd=cwd, capture_output=True, text=True)
    except OSError:
        return None
    return res.stdout if res.returncode == 0 else None


def run_staged(args) -> int:
    """pre-commit kapısı: yalnız staged markdown dosyalarını denetler.

    Deterministik ve modelsiz. Commit'i yapısal bulgu bloklar: kırık bağlantı,
    diskte bulunmayan kaynak atfı, çözülemeyen işaretçi, eksik zorunlu alan.

    İşaretlenmiş çelişki varsayılan olarak bloklamaz: `> [!conflict]` protokolün
    çalıştığı anlamına gelir ve bloklasaydık çelişkiyi kaydeden sayfanın kendisi
    commit edilemezdi. Bloklamasını istersen --gate-conflicts ver.

    Dosyalar çalışma kopyasından okunur, git index'inden değil. Bir dosyayı
    stage ettikten sonra değiştirdiysen kapı yeni halini görür.
    """
    out = _git(args.vault, ["diff", "--cached", "--name-only", "--diff-filter=ACM"])
    if out is None:
        print("vault-lint: not a git repository or git is missing, --staged skipped", file=sys.stderr)
        return 0

    staged = [p.strip().replace("\\", "/") for p in out.splitlines()
              if p.strip().lower().endswith(".md")]
    if args.notes:
        staged = [p for p in staged
                  if any(fnmatch.fnmatch(p, g)
                         or p.startswith(g.rstrip("*").rstrip("/") + "/") for g in args.notes)]
    staged = [p for p in staged if os.path.isfile(os.path.join(args.vault, p))]
    if not staged:
        return 0

    v = Vault(args.vault, args.exclude, staged)
    if not v.notes:
        return 0
    r = run(v, args)

    hard: list = []
    for rel, t in r["broken"]:
        hard.append(f"{rel}: broken link [[{t}]]")
    for rel, p in r["bad_sources"]:
        hard.append(f"{rel}: source not on disk: {p}")
    for rel in r["dangling_pointers"]:
        hard.append(f"{rel}: pointer target does not resolve")
    for rel, missing in r["frontmatter_violations"]:
        hard.append(f"{rel}: missing field: {', '.join(missing)}")
    for rel, val in r["freshness_undeclared"]:
        hard.append(f"{rel}: freshness not declared")
    if args.gate_conflicts:
        for rel, n in r["conflicts"]:
            hard.append(f"{rel}: unresolved conflict ({n})")

    if hard:
        print("vault-lint: commit stopped, %d findings (%d staged files)"
              % (len(hard), len(staged)), file=sys.stderr)
        for line in hard:
            print("  - " + line, file=sys.stderr)
        print("\nFix and stage again. To skip: git commit --no-verify",
              file=sys.stderr)
        return 1

    flagged = sum(n for _, n in r["conflicts"]) if not args.gate_conflicts else 0
    suffix = ", %d flagged conflicts (non-blocking)" % flagged if flagged else ""
    print("vault-lint: %d staged files clean%s" % (len(staged), suffix))
    return 0


def main() -> int:
    # Yerel düzeltme: Windows konsolu cp1254 ile açıldığında rapordaki işaretler
    # UnicodeEncodeError veriyor. Çıktıyı UTF-8'e sabitliyoruz.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    p = argparse.ArgumentParser(description="Run the llm-wiki LINT protocol against a vault.")
    p.add_argument("vault", nargs="?", default=".", help="path to the vault root")
    p.add_argument("--fields", default="type,date,status",
                   help="required frontmatter fields (default: type,date,status)")
    p.add_argument("--date-field", default="date", help="frontmatter field holding the date")
    p.add_argument("--source-fields", default="source,kaynak",
                   help="frontmatter fields holding source paths to verify on disk")
    p.add_argument("--conflict-callout", default="conflict",
                   help="callout name used to flag contradictions")
    p.add_argument("--score-conflicts", action="store_true",
                   help="penalise flagged contradictions (-20 each), per the strict lint.md "
                        "formula. Off by default: a flagged conflict is the protocol working, "
                        "not a defect. An *unflagged* one is the defect, and no linter sees those.")
    p.add_argument("--concept-threshold", type=int, default=3,
                   help="unresolved link seen N+ times becomes a missing-concept finding")
    p.add_argument("--stale-days", type=int, default=90)
    p.add_argument("--stale-status", default="draft,wip,taslak")
    p.add_argument("--notes", action="append", default=[], metavar="GLOB",
                   help="restrict the audit to these paths (repeatable, e.g. --notes 'wiki/*'). "
                        "Links still resolve against the whole vault.")
    p.add_argument("--exclude", action="append", default=[], metavar="GLOB")
    p.add_argument("--stamp-days", type=int, default=30,
                   help="a stamp in the body older than this many days counts as stale (default: 30)")
    p.add_argument("--freshness-field", default="freshness",
                   help="frontmatter field that carries the freshness declaration")
    p.add_argument("--freshness-required", default="entity",
                   help="type values that must declare freshness, comma-separated")
    p.add_argument("--no-freshness", action="store_true",
                   help="turn the freshness checks off")
    p.add_argument("--staged", action="store_true",
                   help="check only markdown files staged in git; exit 1 on a structural "
                        "finding. For the pre-commit gate.")
    p.add_argument("--gate-conflicts", action="store_true",
                   help="with --staged: a flagged conflict also blocks the commit. Off by default, "
                        "because the page that records a conflict would itself become "
                        "uncommittable.")
    p.add_argument("--json", action="store_true", help="emit raw findings as JSON")
    p.add_argument("--write-report", metavar="FILE",
                   help="also write the markdown report to FILE (the only write this tool makes)")
    p.add_argument("--fail-under", type=int, default=None,
                   help="exit 1 if the health score is below this (for CI)")
    args = p.parse_args()

    if not os.path.isdir(args.vault):
        print(f"vault-lint: not a directory: {args.vault}", file=sys.stderr)
        return 2

    if args.staged:
        return run_staged(args)

    v = Vault(args.vault, args.exclude, args.notes)
    if not v.notes:
        print(f"vault-lint: no markdown notes found under {v.root}", file=sys.stderr)
        return 2

    r = run(v, args)
    print(json.dumps(r, indent=2, ensure_ascii=False, default=list) if args.json else render(r))

    if args.write_report:
        with open(args.write_report, "w", encoding="utf-8") as fh:
            fh.write(render(r) + "\n")
        print(f"\nreport written to {args.write_report}", file=sys.stderr)

    if args.fail_under is not None and r["score"] < args.fail_under:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
