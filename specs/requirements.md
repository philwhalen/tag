# Tag — Requirements Document

**Status:** Draft v1.0
**Date:** 2026-06-27
**Source:** Derived from `specs/spec.txt` plus clarifying decisions.

---

## 1. Overview

**Tag** is a cross-platform **desktop GUI application** that reorders pieces of
text within a file according to embedded ordering markers ("tags"). Each tag
carries a whole number indicating where its content belongs in the final
ordering. Tag reads a file, sorts the tagged pieces by their numbers, writes the
result to a new file, and can optionally strip the tag markers from the output.

The defining principle of the reordering is **"sort tags within their slots"**:
untagged text is fixed scaffolding that never moves; only the tagged pieces are
collected, sorted, and dropped back into the positions ("slots") that tags
originally occupied.

---

## 2. Definitions

| Term | Definition |
|------|------------|
| **Tag** | A marked-up piece of text of the form `~N%content%~`. |
| **Opening delimiter** | The two characters `~` … `%` surrounding the number, i.e. `~N%`. |
| **Closing delimiter** | The two characters `%~`. |
| **Number (`N`)** | A whole integer ≥ 1 between `~` and `%`. Lower numbers sort earlier (toward the top); higher numbers sort later (toward the bottom). |
| **Content** | Everything between the opening `%` and the closing `%~`. May span multiple lines and may contain other characters (see §4.4 for limits). |
| **Slot** | A position in the document where a valid tag occurs. Slots are enumerated in reading order (left-to-right, then top-to-bottom). |
| **Untagged text** | Any character of the file that is not part of a valid tag. |
| **Switch** | The user-facing toggle deciding whether tag delimiters are kept or removed in the output. |

---

## 3. Goals & Non-Goals

### 3.1 Goals
- Let a user open a single file, see how it would be reordered, and save the result.
- Reorder tagged pieces deterministically by their numbers without disturbing untagged text.
- Optionally remove tag markers in the output.
- Handle arbitrary text files robustly, including non-ASCII content.

### 3.2 Non-Goals (Out of Scope for v1)
- Batch / multi-file processing (single file per run).
- Editing tags inside the application (Tag is a transform tool, not a text editor).
- Nested tags (see §4.4 / §9).
- An escape mechanism for embedding delimiter sequences in content (see §4.4).
- Command-line interface, library/API, or web version.
- Undo/version history beyond the never-overwrite output policy.

---

## 4. Tag Grammar

### 4.1 Form
A **valid tag** matches the pattern:

```
~  <digits>  %  <content>  %~
```

- `~` immediately followed by one or more decimal digits,
- immediately followed by `%`,
- followed by content (any characters, including newlines),
- terminated by the first subsequent `%~`.

Regex reference (non-greedy, dot-matches-newline):

```
~(\d+)%(.*?)%~
```

### 4.2 Number rules
- The number must be an integer **≥ 1**. `~0%…%~` and negative/non-integer values are **malformed** (§4.3).
- Multi-digit numbers are supported (e.g. `~10%…%~`, `~250%…%~`).
- **Leading zeros are accepted** and parsed as the integer value (`~01%` → 1, `~007%` → 7).
- **Gaps are allowed.** `1, 3` simply sort as the 1st and 2nd pieces — there is no requirement to use consecutive numbers.
- **Duplicate numbers are allowed.** Tags sharing the same number retain their original document (reading) order relative to one another (**stable sort**).

### 4.3 Malformed tags
A construct that looks tag-like but does not satisfy §4.1–§4.2 is **malformed**. Examples: unterminated tag (no closing `%~`), empty/absent number (`~%…%~`), zero/negative/non-numeric number, reversed delimiters.

Handling: **Skip and warn (advisory).**
- Malformed constructs are **left untouched as literal plain text** in their original position.
- They do **not** count as slots and are **not** reordered.
- The application surfaces a **visible warning** for each malformed tag (with line/column and the offending text) **before** producing output.
- Warnings are **advisory only** — they never block saving. The user may proceed with the output (malformed text passed through literally).

### 4.4 Content limitations
- **No escaping (v1).** Content is assumed not to contain the delimiter sequences `~N%`, `%~`, or `~%`. If they do, parsing results are undefined for that tag; this is an accepted limitation for v1.
- **Nesting is not supported.** The first `%~` after an opening delimiter closes the tag. With nested/overlapping constructs, the inner `%~` closes the outer tag; the dangling outer remainder is treated as **malformed literal text and warned** (§4.3). Structured nesting handling is deferred to a future version.

---

## 5. Reordering Algorithm

Given the input file's full text:

1. **Scan** the text in reading order and classify it into an ordered sequence of segments: *valid tags* and *untagged text* runs.
2. **Enumerate slots.** Each valid tag, in reading order, defines slot `0, 1, 2, …`. The slot records its position in the document. Untagged text runs are fixed and never move.
3. **Extract** the list of valid tags as `(number, content, original_index)` records.
4. **Sort** the tag records by `number` ascending. Ties (duplicate numbers) preserve `original_index` order (stable).
5. **Refill slots:** place the *i*-th tag of the sorted list into slot *i*. (Slot enumeration order = sorted-tag insertion order = reading order.)
6. **Render each placed tag** according to the switch (§6):
   - **Remove tags ON:** emit `content` only (delimiters stripped).
   - **Remove tags OFF (keep):** emit the full `~N%content%~`, carrying the tag's own original number and content into its new slot.
7. **Emit untagged text** exactly as-is, in its original positions, preserving all whitespace and line endings.
8. **Write** the result to the output file (§7).

### 5.1 Worked example (mixed content)

**Input** (`notes.txt`):
```
Intro line.
~3%Third piece%~ and ~1%First piece%~
Middle text.
~2%Second piece%~
```

Slots in reading order: slot 0 = the `~3` tag, slot 1 = the `~1` tag, slot 2 = the `~2` tag.
Sorted tags by number: `1:First piece`, `2:Second piece`, `3:Third piece`.
Refill: slot 0 ← First, slot 1 ← Second, slot 2 ← Third.

**Output, Remove tags ON** (`notesordered.txt`):
```
Intro line.
First piece and Second piece
Middle text.
Third piece
```

**Output, Remove tags OFF** (`notesordered.txt`):
```
Intro line.
~1%First piece%~ and ~2%Second piece%~
Middle text.
~3%Third piece%~
```

Note how `Intro line.`, ` and `, and `Middle text.` (untagged scaffolding) never move.

### 5.2 Canonical examples from the original spec
These must continue to hold (single tag per line, no untagged text):

- `cows.txt` with two tags `~3%…%~` and `~1%…%~`, Remove ON → `cowsordered.txt` lists the `~1` content first, then the `~3` content.
- Same input, Remove OFF → tags kept, `~1%…%~` line first then `~3%…%~` line.
- `cow.md` input → output `cowordered.md` (see §7.1 for the exact naming rule).

---

## 6. The Keep/Remove Switch

- A single toggle in the GUI: **Remove tags** vs **Keep tags**.
- **Default: Remove tags ON** (delimiters stripped from output).
- The setting applies to the entire file for the current run.
- When **Keep** is selected, each tag's full marker (`~N%…%~`) is preserved but relocated to its new slot; numbers and content are unchanged.

---

## 7. File Handling

### 7.1 Output naming
- Output filename = input base name + literal `ordered` + original extension.
  - `cows.txt` → `cowsordered.txt`
  - `cow.md` → `cowordered.md` *(insert `ordered` immediately before the final `.ext`)*
  - Files with no extension → append `ordered` to the name.
  - Files with multiple dots → `ordered` is inserted before the **final** extension only (`my.notes.txt` → `my.notesordered.txt`).
- Output is written to the **same folder** as the input.

### 7.2 Overwrite policy
- **Never overwrite.** If the target name already exists, auto-suffix with an incrementing counter: `cowsordered.txt`, `cowsordered(1).txt`, `cowsordered(2).txt`, …
- The actual output path is reported to the user after writing.

### 7.3 Encoding & line endings
- **Auto-detect encoding** on read (UTF-8, UTF-16 LE/BE, Latin-1, BOM-based detection) and **round-trip** the same encoding on write.
- **Preserve original line endings** (CRLF vs LF) and trailing newline state.
- "Read any file type": Tag treats input as text under the detected encoding. Files that cannot be decoded as text (true binary) are reported with a clear error and not processed.
- **Encoding-detection failure:** if the encoding cannot be confidently detected/decoded, Tag **refuses with a clear error** rather than producing lossy output. No silent fallback or character replacement.

---

## 8. GUI / UX Requirements

- **Cross-platform desktop application.** No framework mandated by the user; see §10 recommendation.
- **Single-file workflow:**
  1. User selects a file (file picker and/or drag-and-drop).
  2. User sets the **Remove tags / Keep tags** toggle (default Remove).
  3. A **live preview** pane shows the reordered result (reflecting the current toggle state) before anything is written.
  4. **Warnings** for malformed tags (§4.3) are displayed prominently, ideally with location info, before/alongside the preview.
  5. User clicks **Save** (or equivalent) to write the output file per §7.
- The preview must update when the toggle changes or a new file is loaded.
- After a successful save, show the resulting output path.
- Errors (undecodable file, unreadable/locked file, write failure) are shown as clear, non-fatal messages.

---

## 9. Edge Cases & Rules Summary

| Case | Behavior |
|------|----------|
| File with no valid tags | Output equals input (subject to encoding round-trip); optionally inform the user that nothing was reordered. |
| Empty file | Produces an empty output file. |
| Gaps in numbering (`1,3`) | Allowed; sorted with no error. |
| Duplicate numbers | Allowed; stable order (original reading order preserved among duplicates). |
| Multiple tags on one line | Slots enumerated in reading order across the whole document; tags may move between lines. |
| Multi-line tag content | Supported; the whole block moves as one unit. |
| Malformed/unterminated/zero/negative/non-integer tag | Left as literal text in place; warned; not a slot. |
| Leading zeros in number (`~01%`) | Accepted; parsed as the integer value (`~01%` → 1). |
| Nested/overlapping tags | Not supported; first `%~` closes the tag; dangling remainder is malformed + warned. |
| Delimiter chars inside content | Not supported (no escaping in v1). |

---

## 10. Non-Functional Requirements

- **Determinism:** identical input + switch state always yields identical output.
- **Stability:** sort is stable (preserves order of equal-numbered tags).
- **Fidelity:** untagged bytes/characters, whitespace, and line endings are preserved exactly.
- **Performance:** handle typical document sizes with sub-second preview updates. The file is processed **in memory**; above a **soft cap of ~50 MB** the app warns about possible slowness but still allows processing (no hard limit).
- **Whitespace fidelity:** when Remove tags is ON, only the tag delimiters are stripped. Surrounding whitespace and untagged text are **preserved exactly** — no space collapsing or trimming.
- **Safety:** never overwrite existing files (§7.2); never modify the input file.

### 10.1 Framework recommendation (non-binding)
Since no framework was specified, suitable cross-platform options include:
- **Tauri** (Rust core + web UI) — small binary, good text handling.
- **Avalonia** (C#/.NET) — native cross-platform, strong on Windows (your current OS).
- **Electron** (JS/TS) — fastest to build a live-preview text UI, larger footprint.

Recommendation: **Avalonia or Tauri** for a lean text utility; final choice deferred to implementation.

---

## 11. Resolved Decisions

All previously-open questions are now resolved and folded into the sections above:

1. **Leading zeros** (`~01%`) — **Accepted**, parsed as the integer value. (§4.2, §9)
2. **Nested/overlapping tags** — **Unsupported**; first `%~` closes the tag, dangling remainder is malformed + warned; structured handling deferred to a future version. (§4.4, §9)
3. **Maximum file size** — **In-memory** processing with a **soft cap (~50 MB)** that warns but still allows; no hard limit; no streaming in v1. (§10)
4. **Whitespace when Remove is ON** — **Strictly preserved**; only delimiters are stripped, no space collapsing/trimming. (§5, §10)
5. **Malformed-tag warnings** — **Advisory only**; never block saving. (§4.3, §8)
6. **Encoding-detection failure** — **Refuse with a clear error**; no lossy fallback. (§7.3)

---

## 12. Acceptance Criteria (samples)

- AC-1: Given `cows.txt` containing `~3%…%~` then `~1%…%~` (each on its own line) with Remove ON, the output `cowsordered.txt` contains the `~1` content first, then the `~3` content, with no delimiters.
- AC-2: Same input with Keep ON preserves both full tags, `~1%…%~` before `~3%…%~`.
- AC-3: `cow.md` input produces `cowordered.md` in the same folder.
- AC-4: A mixed file (§5.1) keeps untagged text in place while tagged pieces swap into sorted slot order.
- AC-5: A file containing a malformed tag produces a visible warning and leaves that construct as literal text.
- AC-6: Re-running on a file whose output already exists produces `…ordered(1).<ext>` without overwriting.
- AC-7: A UTF-16 file round-trips to UTF-16 output with original line endings preserved.
