"""Send local files to Notion upload slots made by the Notion `create-file-upload` tool.

Usage: python .claude/scripts/notion_upload.py uploads.json

uploads.json is a list of {"file": <local path>, "upload_url": ..., "authorization": "Bearer ..."}, copied from each
create-file-upload result. Prints one line per file: its path and the `file-upload://...` source to put in a page
(`![caption](file-upload://...)`). A slot expires about an hour after it was made, so place the sources promptly.
"""
import json
import mimetypes
import subprocess
import sys

for item in json.load(open(sys.argv[1], encoding="utf-8")):
    kind = mimetypes.guess_type(item["file"])[0] or "application/octet-stream"
    out = subprocess.run(["curl", "-s", "-X", "POST", item["upload_url"],
                          "-H", "authorization: " + item["authorization"],
                          "-F", f"file=@{item['file']};type={kind}"],
                         capture_output=True, text=True).stdout
    try:
        print(item["file"], json.loads(out).get("markdown_source") or out[:200])
    except ValueError:
        print(item["file"], "FAILED", out[:200])
