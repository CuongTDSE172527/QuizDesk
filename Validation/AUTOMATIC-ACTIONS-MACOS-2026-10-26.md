# Automatic Actions + macOS workflow validation — 2026-10-26

Implemented:
- Windows, Android, macOS and iOS all trigger on every push to main, without
  path filters. Release workflows also support version tags/manual dispatch.
- Build checkouts are pinned to the event SHA. A later push cannot change the
  source commit used by an earlier queued workflow.
- macOS matrix cross-publishes self-contained osx-arm64/osx-x64 on macos-26,
  creates .app bundles, ad-hoc signs them and invokes Apple's pkgbuild and
  productbuild for architecture-specific PKG installers/checksums.
- macOS installers target /Applications; architecture/running-app checks,
  fixed bundle placement and postinstall signature verification are included.
- All successful builds are retained as 7-day Actions artifacts. On automatic
  push events, an already assigned version or existing platform release is
  preserved, with a summary explaining that the new build remains in Artifacts.
  New versions publish APK/EXE/PKG assets; iOS remains artifact-only.
- Publisher supports both macOS architectures and handles tag/release creation
  races by reading back metadata and checking the built commit. Concurrency
  groups are separated by platform/architecture/commit to avoid cross-platform
  pending-job cancellation. No published asset is overwritten.

Verification completed locally:
- actionlint 1.7.12 passes all four workflow files, without suppressions.
- PowerShell parser passes all release helper scripts.
- 24 mocked publishing tests pass, including both macOS asset name pairs,
  automatic push skip behavior and concurrent tag/release creation.
- 6 portable macOS packaging checks pass with Apple tools mocked: both target
  architectures, package arguments/distribution/checksum, wrong version/arch,
  existing-file protection and non-macOS rejection.
- Trigger/checkout/automatic-flag assertions pass for all four workflow files.
- Executed the iOS SDK pin command in Bash and parsed the generated global.json.
- Python syntax checked and git diff --check passes.

Not performed:
- No real macOS native build, installer run or Apple signing/notarization test.
  The PKG is unsigned and the .app is ad-hoc signed; no Developer ID secret added.
- No live GitHub workflow execution, release write, commit, tag or push.
- Product/Core code was unchanged in this task; existing 138 Core checks are
  configured in release CI, but were not rerun for these workflow-only changes.
