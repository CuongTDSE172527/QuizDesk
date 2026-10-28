# Word Symbol import fix — 28/10/2026

Reported in installed version 5.16.5: DME flashcards show 12s instead of 12µs.
Current source version remains 5.16.7; no published version/tag was downgraded.

Root cause: WordReader.Plain read w:t/tab/br only. The original Đề ôn DME.docx
contains 16 w:sym nodes: 8 Symbol F06D (micro), 4 F0B1 (plus-minus), 4 F074 (tau).
These are font-encoded glyphs rather than ordinary text runs.

Fix: read w:sym in document order, remove the F000 legacy encoding offset only
for recognized Symbol font codes, map common mathematical/Greek glyphs to Unicode,
and handle directly formatted Symbol text runs. Existing Unicode text stays unchanged.
Unknown/malformed/non-XML glyph codes retain visible labels and review warnings;
they cannot silently turn values with unsupported units into ordinary numbers.
Apply warnings per parsed section, including single/multi-file import and fallback parser.
No font file, new package dependency or source document was added to the repository.

Validation:
- 28 symbol checks passed, including real DME 32-question import, all 16 symbol
  nodes retained, 12µs/50µs, text answer matching, explanation, choices, .quiz roundtrip,
  common Greek/math signs, malformed/unknown symbols.
- 268 existing parser/export regressions passed: word sections 18, recognition 77,
  review 35, batch 19, merge 30, media 39, export/sampling 25, Office export 25.
  Total 296 core checks passed. CI Test-Release includes the portable word-symbols suite.
- Actual Preview UI import-to-flashcard check passed and rendered screenshot inspected:
  flashcard shows A. 12µs. with proper micro glyph.
- The original DME file has no answer keys; QA confirms A in isolated in-memory/test
  data only before generating its flashcard. The source file is never modified.
- Desktop Release build: zero warnings/errors. QA uses existing obsolete Bitmap.Save
  capture API warning; no native phone/Mac execution performed.

Limitations: this is Symbol/Unicode text import, not a full OMML/MathType/OLE formula
engine or an all-font glyph mapper. Existing imported banks/lifetime flashcard snapshots
are not repaired automatically. Reimport source and start a new flashcard session.
No version change, commit, push or release upload performed.

References:
https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.symbolchar
https://www.unicode.org/Public/MAPPINGS/VENDORS/ADOBE/symbol.txt
