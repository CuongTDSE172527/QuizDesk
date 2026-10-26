# Release automation verification — 2026-10-26

Added independent Android APK and Windows EXE workflows, triggered by a version
tag or manual dispatch against an existing tag. Both attach platform-specific
assets and SHA-256 manifests to a shared GitHub Release.

Validation completed locally:

- actionlint 1.7.12: both new workflows pass (expressions, job dependencies,
  permissions, action inputs and YAML syntax). Download SHA-256 verified against
  the official GitHub asset digest.
- PowerShell parser: release helper scripts and modified Android build script pass.
- 12 mocked publishing checks pass: create/upload, both platforms in one release,
  identical-asset retries, conflicting assets, tampered checksums, draft publishing,
  valid/invalid version tags and refusal to generate a replacement signing key.
- git diff --check passes.
- CI runs the 12 publishing checks and the existing 88 targeted Core regressions
  before building either platform.

Not executed here: Android SDK/workload build, signed APK verification,
self-contained Windows publish, Inno Setup compilation, or live GitHub release
creation/upload. The publication tests mock gh and do not write to GitHub.
Android signing secrets must be configured by the repository owner.
No tag, commit, push or live release was created in this task.
