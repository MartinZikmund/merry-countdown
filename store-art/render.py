"""Render store graphics from the HTML templates with headless Chrome.

Usage: python store-art/render.py play|msstore [--lang en,cs]
Captures are read from store-art/raw*/ and output goes to store-art/out-<target>/ (both git-ignored).
Set CHROME to the browser binary if it isn't in the usual place.
"""
import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path
from urllib.parse import quote

HERE = Path(__file__).resolve().parent

CHROME_CANDIDATES = [
    os.environ.get("CHROME", ""),
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    shutil.which("google-chrome") or "",
    shutil.which("chromium") or "",
]

SUBTITLES = {"en": "Advent calendar & wish list", "cs": "Adventní kalendář a přání"}


def chrome() -> str:
    for c in CHROME_CANDIDATES:
        if c and Path(c).exists():
            return c
    sys.exit("Chrome not found; set CHROME to its path.")


def shot(page: str, query: str, size: tuple[int, int], out: Path) -> None:
    out.parent.mkdir(parents=True, exist_ok=True)
    url = (HERE / page).as_uri() + "?" + query
    subprocess.run(
        [chrome(), "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
         "--virtual-time-budget=4000", f"--window-size={size[0]},{size[1]}", f"--screenshot={out}", url],
        check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    print(out.relative_to(HERE))


def play(langs: list[str]) -> None:
    out = HERE / "out-play"
    shot("tile.html", "s=512", (512, 512), out / "icon-512.png")
    for lang in langs:
        shot("art.html", f"kind=feature&zoom=0.33334&lang={lang}&sub={quote(SUBTITLES[lang])}", (1024, 500), out / f"feature-{lang}.png")
        for f in range(1, 7):
            shot("compose-phone.html", f"f={f}&lang={lang}", (2160, 3840), out / f"phone-{lang}-{f}.png")
        # Frame 4 (the Windows mini window) is left out; tags keep the numbering 1..7.
        for tag, f in enumerate([1, 2, 3, 5, 6, 7, 8], start=1):
            shot("compose.html", f"f={f}&lang={lang}&dir=raw-tablet&tag={tag}&crop=765,418,1030,843&rw=2560&zoom=1.15",
                 (3840, 2160), out / f"tablet-{lang}-{tag}.png")


def msstore(langs: list[str]) -> None:
    out = HERE / "out-msstore"
    for s in (300, 150, 71):
        shot("tile.html", f"s={s}", (s, s), out / f"tile-{s}.png")
    shot("art.html", "kind=hero", (3840, 2160), out / "hero-16x9.png")
    shot("art.html", "kind=poster", (1440, 2160), out / "poster-2x3.png")
    shot("art.html", "kind=box", (2160, 2160), out / "box-1x1.png")
    shot("art.html", "kind=titled&zoom=0.5", (1920, 1080), out / "titled-hero-1920x1080.png")
    shot("art.html", "kind=square&zoom=0.5", (1080, 1080), out / "promo-square-1080.png")
    for lang in langs:
        for f in range(1, 9):
            shot("compose.html", f"f={f}&lang={lang}", (3840, 2160), out / f"{lang}-{f}.png")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("target", choices=["play", "msstore"])
    ap.add_argument("--lang", default="en,cs")
    a = ap.parse_args()
    {"play": play, "msstore": msstore}[a.target](a.lang.split(","))


if __name__ == "__main__":
    main()
