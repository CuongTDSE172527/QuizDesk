# macOS signed bundle copy — 27/10/2026

Evidence: both 5.15.0 architecture jobs build the app and PKG successfully, then fail
codesign verification of the mounted DMG with Avalonia.Base.dll "code object is not
signed at all". Source bundle verification succeeds before copying.

Managed DLLs in Contents/MacOS are non-Mach-O code. Their ad-hoc signatures can
reside in extended attributes. shutil.copytree/copy2 on macOS does not preserve
this metadata; preserving file contents/mode alone is insufficient.
Reference: https://developer.apple.com/library/archive/technotes/tn2206/

Changes:
- DMG staging and mounted-to-local copying use /usr/bin/ditto --rsrc --extattr.
- Verify the staged bundle before creating an image; retain mounted and copied
  verification and detach-on-failure. Do not re-sign or weaken strict/deep verification.
- Native PKG staging also uses ditto and verifies the copy before pkgbuild.

Validation on Windows:
- 12 portable packaging tests pass, Apple tools mocked. Checks include both
  architectures, metadata-preserving copy commands, verification immediately after
  copying, early stop on staged signature failure, copied signature failure cleanup,
  wrong architecture and immutable output rejection.
- Python syntax checked; git diff --check passed.
- No native Mac build or actual codesign executed locally. Actions must confirm
  this fix using real ditto/codesign/hdiutil and the full published app.

Scope: DMG/native PKG signed-bundle copies. The legacy Python tar.gz generation
path is unchanged; it is not the source used to create DMG or the published DMG asset.
No version/tag/asset was moved or replaced and no commit/push/dispatch was performed.
