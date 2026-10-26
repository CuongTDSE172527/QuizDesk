# IPA publication — 26/10/2026

Implemented device-only IPA publication after successful unsigned-ipa job.
Pull requests build artifacts only. Build jobs retain read-only repository permissions;
a separate Windows publish job receives contents:write and GITHUB_TOKEN.
Simulator job remains independent. Artifacts and logs retain seven-day expiry.

Versioned assets: QuizDesk-X.Y.Z-ios-arm64-unsigned.ipa and
QuizDesk-X.Y.Z-iOS-unsigned-SHA256.txt. Original build checksum is checked before
staging the renamed IPA, and the shared publisher validates it after downloading.
iOS display version is checked alongside Desktop/Android/shared UI versions.
Exact built commit is used throughout. Tag conflicts/immutable asset checks are retained.

Verification:
- All 33 mocked release tests passed (no GitHub writes), including IPA+checksum upload,
  tampering rejection before any API call, identical existing asset reuse, and conflicting tag skip.
- actionlint passed for all four workflows; git diff --check passed.
- No iOS build, GitHub workflow dispatch, push, live release upload or native signing performed.

The IPA remains unsigned and needs signing for installation. This is not TestFlight/App Store
publication. Since v5.14.0 is already published at another commit, a new synchronized version
is required to publish a new commit; artifacts remain available without moving that tag.
