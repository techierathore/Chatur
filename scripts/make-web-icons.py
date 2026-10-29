"""Renders the web harness icons from the approved Chatur tile (REQ-NFR-009, BRD-191).

Reads  scripts/brand/Chatur.png            (the tile, 1024 px, copied byte for byte from the brand folder)
Writes src/Chatur.WebHarness/wwwroot/favicon.ico   (32 x 32)
       src/Chatur.WebHarness/wwwroot/icon-192.png  (192 x 192)
       src/Chatur.WebHarness/wwwroot/icon-512.png  (512 x 512)

Run with Windows Python that has Pillow, from anywhere:  python.exe scripts/make-web-icons.py
The output is deterministic, so running it again when nothing changed rewrites identical files.
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "scripts" / "brand" / "Chatur.png"
OUT = ROOT / "src" / "Chatur.WebHarness" / "wwwroot"


def render(size):
    with Image.open(SOURCE) as tile:
        return tile.convert("RGBA").resize((size, size), Image.LANCZOS)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    render(32).save(OUT / "favicon.ico", format="ICO", sizes=[(32, 32)])
    render(192).save(OUT / "icon-192.png", format="PNG", optimize=True)
    render(512).save(OUT / "icon-512.png", format="PNG", optimize=True)
    print("wrote favicon.ico, icon-192.png, icon-512.png to", OUT)


if __name__ == "__main__":
    main()
