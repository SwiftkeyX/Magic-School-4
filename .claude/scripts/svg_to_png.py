"""Render SVG files to PNG at 2x with headless Chrome, for pages that cannot show SVG (Notion).

Usage: python .claude/scripts/svg_to_png.py <out_dir> file1.svg [file2.svg ...]
Each SVG needs width="..." height="..." on its root element; the PNG is cut to exactly that size.
"""
import os
import re
import subprocess
import sys

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
out_dir = sys.argv[1]
os.makedirs(out_dir, exist_ok=True)
for svg in sys.argv[2:]:
    head = open(svg, encoding="utf-8").read(2000)
    w, h = re.search(r'width="(\d+)" height="(\d+)"', head).groups()
    png = os.path.join(out_dir, os.path.splitext(os.path.basename(svg))[0] + ".png")
    subprocess.run([CHROME, "--headless", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=2",
                    f"--screenshot={png}", f"--window-size={w},{h}", "file:///" + os.path.abspath(svg).replace("\\", "/")],
                   capture_output=True)
    print(png, "ok" if os.path.exists(png) else "FAILED")
