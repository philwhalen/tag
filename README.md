# Tag

A cross-platform desktop app that reorders pieces of text in a file by embedded
numeric tags. A tag is `~N%content%~` where `N` is a whole number ≥ 1; lower numbers
sort earlier. Untagged text stays exactly where it is ("sort tags within their slots").

See [`specs/requirements.md`](specs/requirements.md) and
[`plans/implementation-plan.md`](plans/implementation-plan.md) for the full design.

## Layout

| Project | Purpose |
|---------|---------|
| `src/Tag.Core` | Pure, UI-independent engine: parse → reorder → render → encoding-aware IO. |
| `src/Tag.App` | Avalonia (MVVM) desktop GUI: open, toggle keep/remove, live preview, save. |
| `tests/Tag.Core.Tests` | xUnit suite covering the acceptance criteria and edge cases. |

## Requirements

- .NET 8 SDK.

## Build, test, run

```sh
dotnet build Tag.sln
dotnet test Tag.sln
dotnet run --project src/Tag.App
```

Try it on the included `samples/cows.txt`: open it, leave **Remove tags** on, and the
preview reorders the two lines; **Save Ordered File** writes `samples/cowsordered.txt`.

## Publish (self-contained Windows single-file)

```sh
dotnet publish src/Tag.App/Tag.App.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

(Swap `-r` for `osx-arm64`, `linux-x64`, etc. for other platforms.)

## How it works

1. **Read & detect encoding** (BOM → UTF-16 heuristic → strict UTF-8 → Latin-1; binary
   files are refused).
2. **Parse** into an ordered list of tag/text segments; malformed tag-like constructs are
   left as literal text and reported as advisory warnings.
3. **Reorder**: tags are stably sorted by number and dropped back into the original tag
   positions in reading order; untagged text never moves.
4. **Render** with tags kept (`~N%…%~`) or removed (content only); whitespace is preserved.
5. **Save** beside the input as `<name>ordered<ext>`, never overwriting (auto-suffixes
   `(1)`, `(2)`, …), round-tripping the original encoding.
