# Export, sampling and bulk import verification — 2026-10-26

Implemented:

- Export chooser for .quiz, JSON, CSV, TSV, TXT and standalone HTML.
- JSON preserves embedded media and review metadata; HTML escapes user text,
  uses a restrictive CSP and embeds media without external requests/autoplay.
- CSV/TSV/TXT are text-only. Spreadsheet formula prefixes are escaped with an
  apostrophe; the UI warns that reimporting those cells retains the apostrophe.
- Optional omission of answer/explanation fields for non-native exports.
- Per-bank random sample quotas, without replacement within each bank, followed
  by optional global shuffle; generated mock exam defaults to all sampled items.
- Source banks remain unchanged; original numbers remain in the merge audit.
- Removed artificial file-count cap. Combined-bank content/media budgets remain.
- Ctrl+A / Cmd+A in the library selects all banks and preserves text-box editing.
- Desktop title and Settings version derive from the shared UI assembly version.

Verification:

- Core, shared UI and test project Release builds pass: 0 warnings, 0 errors.
- 25 new export/sampling checks pass.
- 19 batch-import, 30 merge and 39 media regression checks pass (113 total).
- JSON/CSV/TSV roundtrips, embedded PNG, HTML escaping, formula safety, quota
  boundaries, deterministic draws, source hashes and 21-file import are covered.
- Compiled UI assembly version verified as 5.12.0.
- CI release test script includes the new export-sampling suite.

Not run here: interactive export/sampling dialogs, OS-specific file picker and
keyboard gestures, native Android/iOS/macOS execution or rebuilt APK/EXE installers.
Sampling occurs when a new combined bank is generated. Reusing that bank shuffles
its existing sample; generating a new bank draws another sample from the sources.
