# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**Tag** is a cross-platform .NET 8 desktop app (Avalonia/MVVM) that reorders pieces of
text in a file by embedded numeric tags. A tag is `~N%content%~` where `N` is an integer
≥ 1; lower numbers sort earlier. The governing rule is **"sort tags within their slots"**:
untagged text is fixed scaffolding that never moves — only tagged pieces are collected,
stably sorted by number, and dropped back into the original tag positions in reading order.

The full behavioral contract lives in [`specs/requirements.md`](specs/requirements.md) and is
authoritative — consult it (especially §4 grammar, §5 algorithm, §9 edge cases, §12 acceptance
criteria) before changing parsing, ordering, encoding, or output-naming behavior.

## Commands

```sh
dotnet build Tag.sln
dotnet test Tag.sln
dotnet run --project src/Tag.App        # launch the GUI
```

Run a single test or a subset (xUnit via `dotnet test`):

```sh
dotnet test --filter "FullyQualifiedName~ParserTests"
dotnet test --filter "DisplayName~malformed"
```

Publish a self-contained single-file build (swap `-r` for `osx-arm64`, `linux-x64`, etc.):

```sh
dotnet publish src/Tag.App/Tag.App.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

## Architecture

Two projects plus a test project (`Tag.sln`):

- **`src/Tag.Core`** — the pure, UI-independent engine. No Avalonia dependency. All
  business logic and the only place tag semantics live.
- **`src/Tag.App`** — Avalonia desktop GUI (MVVM via CommunityToolkit.Mvvm). References
  Tag.Core; contains no tag logic of its own.
- **`tests/Tag.Core.Tests`** — xUnit suite mirroring the acceptance criteria. Tag.App has
  no tests; keep logic in Core so it stays testable.

### The Core pipeline

`TagProcessor` is the façade the GUI drives. Everything flows through these stages, each in
its own namespace/file and each a small pure unit:

```
FileReader → EncodingDetector → TagParser → TagReorderer → DocumentRenderer → OutputNamer → FileWriter
   (IO)         (IO)             (Parsing)    (Ordering)      (Rendering)        (IO)         (IO)
```

The central data type is `ParsedDocument` (`Model/`): an ordered, flat list of `Segment`
records — `TextSegment` (fixed untagged text) and `TagSegment(Number, Content, ReadingIndex)`
(a reorderable "slot") — plus the metadata needed for lossless round-tripping (encoding,
line ending, trailing-newline flag, warnings).

GUI usage pattern (see `MainWindowViewModel`): `Load()` once per file, then `Render()` on
every toggle change for the live preview, and `Save()` on commit. `Render` is pure and cheap;
it re-runs `TagReorderer.Reorder` + `DocumentRenderer.Render` each call.

### Behaviors that are easy to break

- **Reordering** (`TagReorderer`): tags are stably sorted by `Number` (LINQ `OrderBy` is
  stable — relied upon for duplicate-number ordering) and refilled into slots in reading
  order. Untagged segments are copied through unchanged.
- **Parsing** (`TagParser`): deliberately conservative malformed-tag detection to avoid false
  positives on arbitrary text. Malformed constructs are left as literal `TextSegment` text and
  surfaced as **advisory** `MalformedWarning`s (line/col/snippet) — warnings never block saving.
  First `%~` closes a tag; no nesting, no escaping (v1 limitations).
- **Encoding** (`EncodingDetector`): detection order is BOM → BOM-less UTF-16 heuristic →
  strict UTF-8 → Latin-1, else throw `EncodingDetectionException` (binary files are refused —
  no lossy fallback). Encoding instances are chosen to round-trip BOM presence exactly.
- **Output naming** (`OutputNamer`): insert literal `ordered` before the final extension; never
  overwrite — auto-suffix `(1)`, `(2)`, …. `FileWriter` uses `FileMode.CreateNew` to enforce this.
- **Fidelity**: untagged text, whitespace, and line endings are preserved byte-for-byte. When
  Remove-tags is on, only delimiters are stripped — never trim or collapse whitespace.

When changing engine behavior, update or add a test in `Tag.Core.Tests` tied to the relevant
requirement, and keep all logic in Tag.Core (the GUI should remain a thin shell).
