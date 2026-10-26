# Release tag fix verification — 2026-10-26

Observed failure: manual workflow input v5.12.0 was passed to checkout as
refs/tags/v5.12.0 before the tag existed. Both platform jobs stopped at checkout.
The source also still declared 5.11.0.

Changes:

- Manual dispatch checks out the selected branch/tag (github.ref), not a tag
  that might not exist. The tag input is optional; blank derives it from source.
- Push-tag builds still use the pushed tag and validate it against source version.
- Version synchronized to 5.12.0 in shared UI/native hosts and Windows installer;
  Android versionCode is 60101 and iOS build is 16.
- Publisher receives the actual build commit, creates a missing lightweight tag
  at that SHA, or resolves an existing lightweight/annotated tag and requires
  that it point to the same commit. Existing tags and assets are never moved.
- GitHub HTTP 404 is distinguished from authorization/network errors before
  deciding to create a tag or release.

Verification:

- actionlint 1.7.12: both workflows pass.
- PowerShell parser: all release scripts pass.
- 18 mocked publishing/version tests pass, including the missing-tag scenario,
  blank input, mismatched tag commit, annotated tags and HTTP 403 rejection.
- git diff --check passes.
- No live tag, release, build run, git commit or push was performed here.

After committing/pushing, start a NEW Run workflow for each platform from main,
leaving tag blank. Re-running the old failed run uses its old workflow revision.
