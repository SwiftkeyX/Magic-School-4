---
name: notion-docs
description: Read, write and back up this project's Notion workspace (the "Magic School 4" page, its task board and design docs). Use whenever a task, design note, report or picture should go into Notion, when finishing work that changes a task's status, or when the user asks for a Notion snapshot/backup.
---

# Notion docs for Magic School 4

Since 2026-10-27 the owner keeps tasks and design knowledge in Notion, not in `.claude/docs/*.md`. The owner edits
these pages by hand while you work, so **always fetch a page right before changing it** and change only what the task
needs. Never rewrite the owner's wording, and never delete a block or sub-page you did not create without asking.

## Where things are

Home page **Magic School 4**: https://app.notion.com/p/4068c134506e8113970dfc02c1e23294 (private to the owner).
It splits pages into *working docs* (written in daily) and *knowledge docs* (stable; a "(testing)" suffix means it
is changing fast). Fetch the home page to find pages added since this list was written.

| Page | ID | Holds |
|---|---|---|
| Magic School 4 Tasks (database) | `2f5923c877e5428f8f131e4efe278ee4`, data source `collection://05ae747f-ef51-43bb-9f5d-feb3df21dc8a` | every task |
| Working space | `4068c134506e8103bed1f1a12f7062fc` | the owner's thinking notes (theirs: read, rarely write) |
| Planning | `4068c134506e81ff818beb1916bf6f43` | short goals, deferred design decisions |
| Power level (testing) | `4068c134506e81758555c5a00a2af84f` | prog, criteria, per-team balance log |
| Mark layout | `4068c134506e8198bc7ed1d8bb28c85b` | mark-shape rules, with pictures |
| Team comp design | `4068c134506e81a99cd0ce62f11fe969` | how a team is made; links to sub-pages (team/hero distinct, 10 AI comps, real team usage 1-0-0) |
| Benchmark | `4068c134506e8151b337e95bcf6ae018` | what the benchmarks taught us |
| Code notes (old flagging) | `4068c134506e8183a51ce2e33ea807ed` | the owner's old `// FLAGGING` code notes; one Low task points here |

**One topic lives on one page.** When something new is learned, update the page that owns the topic and link to it
from elsewhere; never copy the same rule onto two pages. If no page owns it, ask before making a new one.

## Tasks database

Columns: `Task` (title), `Status` (To do / Doing / Done), `Priority` (High / Medium / Low), `Size` (Major = a
system, a redesign or a balance pass; Minor = a small fix or a number tweak), `Area` (Balance = numbers and tuning;
Organize = cleanup, renames, docs; Quality of life = tools and readability for the owner; Feature = new mechanics,
heroes, items), `Details` (rich text: a link named `Page → Heading`, e.g.
`[Power level → Skeleton team](https://www.notion.so/4068c134506e81758555c5a00a2af84f)`).

- Priority: High = what the owner is working on now; Medium = small fixes; Low = everything else (backlog, ideas).
  Ask before raising a task's priority.
- New task: `create-pages` with parent `{"data_source_id": "05ae747f-ef51-43bb-9f5d-feb3df21dc8a"}`, one short line
  as the title; the detail goes on the doc page, linked from `Details`.
- When you finish or start a task, set its `Status` with `update-page` → `update_properties`.
- Give every new task a Size.
- The "Board" view groups by Status and shows Task, Priority, Size, Area, Details.
- Read the whole list with `query-data-sources` (SQL over the data source URL).

## Writing on a page

- Use `update-page` with `update_content` (small search-and-replace on the fetched text), not `replace_content`.
- Heading style the owner chose (H2 and H3 look too alike in Notion):
  - a section = divider + blue H1:
    `---\n\n<h1 color="blue">\nSection name\n</h1>`
  - a sub-section = H3; on long knowledge pages make it a collapsible toggle:
    `<toggle heading="h3">\nTitle\n\nbody...\n</toggle>`
  - never use H2. Leave the owner's own H4s alone.
  - To wrap an existing section in a toggle, replace `\n\n### Title` with `\n</toggle>\n\n<toggle heading="h3">\nTitle`
    (and close the last one before the next H1). Fetch afterwards to check.
- Link to another page with `<mention url="https://app.notion.com/p/<id>" />`. Heading-level links need a block ID,
  which the API does not return; name the heading in the link text instead.
- Write like the rest of the project: short, plain, the outcome first.

## Pictures

Notion cannot show SVG reliably, and local paths do not work. To put a picture on a page:

1. `python .claude/scripts/svg_to_png.py <out_dir> a.svg b.svg` (PNG at 2x through headless Chrome).
2. One `create-file-upload` call per file (they can run in parallel).
3. Put each result's `upload_url` and `upload_headers.authorization` with its file path into a JSON list, then
   `python .claude/scripts/notion_upload.py uploads.json`; it prints a `file-upload://...` source per file.
4. Within the hour, place them: `![caption](file-upload://...)`, each image its own block with blank lines around.
   A fetch then shows them as `notion-file-block://...`; keep those sources untouched when editing.

## Snapshot (backup)

When asked for a snapshot, or before a large change to several pages:

1. Fetch the home page, every page it lists, and every sub-page they mention (`<mention>` / `<page>` URLs).
2. Read the task list with `query-data-sources`.
3. Overwrite `.claude/docs/notion-snapshot/` (one folder, no dated sub-folders; git history keeps the old ones).
   Delete files for pages that no longer exist, then write: one `.md` per page (`# <icon> <title>` then the fetched
   content), `tasks.md` (a table), `README.md` (file → page URL, and the "Taken:" time), and `images/` (download each
   `notion-file-block://` image with `get-file-download-urls` + curl, or copy the local PNG it was made from;
   point the page's image links at `images/<name>.png`).
4. Tell the owner the folder; do not commit it (the owner commits).

The most complete backup is Notion's own: on the Magic School 4 page, `•••` → Export → Markdown & CSV, include
sub-pages. Suggest it for a full copy; Notion also keeps page history (`•••` → Page history) and a Trash.
