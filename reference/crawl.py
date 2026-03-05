#!/usr/bin/env python3
"""
STB website crawler.
Mirrors https://www.stb.com.mk/ into reference/stb-crawl/
and generates inventory files.
"""

import re
import sys
import time
import random
import urllib.parse
from pathlib import Path
from collections import deque
from typing import Set

# Force UTF-8 output on Windows so Macedonian/Cyrillic chars don't crash the console
if sys.stdout.encoding != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if sys.stderr.encoding != "utf-8":
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import requests
from bs4 import BeautifulSoup

# ── Config ────────────────────────────────────────────────────────────────────
BASE_URL   = "https://www.stb.com.mk/"
OUTPUT_DIR = Path(__file__).parent / "stb-crawl"
HEADERS    = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/120.0.0.0 Safari/537.36"
    ),
    "Accept-Language": "mk,en;q=0.9",
}
WAIT_MIN   = 0.4   # seconds between requests
WAIT_MAX   = 1.2
TIMEOUT    = 30
RETRIES    = 3
MAX_PAGES  = 2000  # safety cap

# Extensions to download as assets (not crawl)
ASSET_EXTS = {
    ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp",
    ".ico", ".woff", ".woff2", ".ttf", ".eot", ".otf",
    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
}
SKIP_EXTS = {".exe", ".zip", ".tar", ".gz", ".rar", ".mp4", ".avi", ".mov"}

# ── Helpers ───────────────────────────────────────────────────────────────────

def url_to_local_path(url: str) -> Path:
    parsed = urllib.parse.urlparse(url)
    # strip query/fragment for file path
    path = parsed.path.lstrip("/")
    if not path or path.endswith("/"):
        path = path + "index.html"
    elif "." not in Path(path).name:
        path = path + "/index.html"
    # sanitize
    path = re.sub(r'[<>:"|?*]', "_", path)
    return OUTPUT_DIR / parsed.netloc / path


def normalise(url: str, base: str) -> str | None:
    """Resolve relative URL, return None if out-of-scope or should be skipped."""
    url = url.strip()
    if url.startswith(("mailto:", "tel:", "javascript:", "#", "data:")):
        return None
    full = urllib.parse.urljoin(base, url).split("#")[0]
    parsed = urllib.parse.urlparse(full)
    if parsed.scheme not in ("http", "https"):
        return None
    # only www.stb.com.mk (skip ebank, mbank, etc.)
    if parsed.netloc not in ("www.stb.com.mk", "stb.com.mk"):
        return None
    ext = Path(parsed.path).suffix.lower()
    if ext in SKIP_EXTS:
        return None
    return full


def download(session: requests.Session, url: str, local: Path,
             retries: int = RETRIES) -> bytes | None:
    local.parent.mkdir(parents=True, exist_ok=True)
    if local.exists():
        return local.read_bytes()
    for attempt in range(retries):
        try:
            r = session.get(url, headers=HEADERS, timeout=TIMEOUT,
                            allow_redirects=True, verify=False)
            r.raise_for_status()
            local.write_bytes(r.content)
            return r.content
        except Exception as exc:
            print(f"  [warn] attempt {attempt+1}/{retries} failed for {url}: {exc}")
            time.sleep(1)
    return None


# ── Main crawl ────────────────────────────────────────────────────────────────

def crawl():
    import urllib3
    urllib3.disable_warnings()

    session = requests.Session()
    session.verify = False

    visited: Set[str]        = set()
    asset_urls: Set[str]     = set()
    queue: deque             = deque([BASE_URL])
    page_count               = 0

    print(f"Starting crawl of {BASE_URL}")
    print(f"Output: {OUTPUT_DIR}\n")

    while queue and page_count < MAX_PAGES:
        url = queue.popleft()
        if url in visited:
            continue
        visited.add(url)

        ext = Path(urllib.parse.urlparse(url).path).suffix.lower()
        local = url_to_local_path(url)

        # ── Download asset (non-HTML) ─────────────────────────────────────
        if ext in ASSET_EXTS:
            if not local.exists():
                print(f"  [asset] {url}")
                download(session, url, local)
                time.sleep(random.uniform(WAIT_MIN * 0.5, WAIT_MAX * 0.5))
            asset_urls.add(url)
            continue

        # ── Download HTML page ────────────────────────────────────────────
        page_count += 1
        print(f"[{page_count:4d}] {url}")

        content = download(session, url, local)
        if content is None:
            continue

        # Try to detect encoding
        try:
            html_text = content.decode("utf-8", errors="replace")
        except Exception:
            html_text = content.decode("latin-1", errors="replace")

        soup = BeautifulSoup(html_text, "html.parser")

        # ── Collect linked pages ──────────────────────────────────────────
        for tag in soup.find_all("a", href=True):
            link = normalise(tag["href"], url)
            if link and link not in visited:
                queue.append(link)

        # ── Collect assets ────────────────────────────────────────────────
        for tag in soup.find_all(["link", "script", "img",
                                  "source", "video", "audio"]):
            for attr in ("href", "src", "data-src", "data-lazy-src"):
                val = tag.get(attr)
                if val:
                    norm = normalise(val, url)
                    if norm and norm not in visited:
                        queue.appendleft(norm)   # prioritise assets

        # Also grab CSS @import and url() references? — handled via asset download
        time.sleep(random.uniform(WAIT_MIN, WAIT_MAX))

    print(f"\nCrawl complete. Pages: {page_count}, Total URLs: {len(visited)}")
    return visited


# ── Inventory generation ──────────────────────────────────────────────────────

def generate_inventories(crawl_root: Path, output_dir: Path):
    html_files   = []
    css_files    = []
    js_files     = []
    image_files  = []
    doc_files    = []

    for p in crawl_root.rglob("*"):
        if not p.is_file():
            continue
        rel = str(p.relative_to(crawl_root)).replace("\\", "/")
        ext = p.suffix.lower()
        size = p.stat().st_size

        if ext in (".html", ".htm"):
            html_files.append((rel, size))
        elif ext == ".css":
            css_files.append((rel, size))
        elif ext == ".js":
            js_files.append((rel, size))
        elif ext in (".png", ".jpg", ".jpeg", ".gif", ".svg",
                     ".webp", ".ico", ".bmp"):
            image_files.append((rel, size))
        elif ext in (".pdf", ".doc", ".docx", ".xls",
                     ".xlsx", ".ppt", ".pptx"):
            doc_files.append((rel, size))

    def write_inv(filename: str, items: list, header: str):
        path = output_dir / filename
        with open(path, "w", encoding="utf-8") as f:
            f.write(f"# {header}\n")
            f.write(f"# Generated: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"# Count: {len(items)}\n\n")
            for rel, size in sorted(items):
                f.write(f"{size:>10,} bytes  {rel}\n")
        print(f"Wrote {path.name} ({len(items)} entries)")

    write_inv("_page-inventory.txt",     html_files,  "HTML Pages")
    write_inv("_css-inventory.txt",      css_files,   "CSS Files")
    write_inv("_js-inventory.txt",       js_files,    "JavaScript Files")
    write_inv("_image-inventory.txt",    image_files, "Images")
    write_inv("_document-inventory.txt", doc_files,   "Documents (PDF, Office)")


# ── Entry point ───────────────────────────────────────────────────────────────

if __name__ == "__main__":
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    visited = crawl()
    print("\nGenerating inventories…")
    generate_inventories(OUTPUT_DIR / "www.stb.com.mk", OUTPUT_DIR)
    print("Done.")
