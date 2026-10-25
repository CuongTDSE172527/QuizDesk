#!/bin/sh
# Run on a Mac only, for local testing of this unsigned cross-built bundle.
set -eu
app="${1:-$(dirname "$0")/QuizDesk.app}"
if [ ! -f "$app/Contents/Info.plist" ] || [ ! -f "$app/Contents/MacOS/QuizDesk" ]; then
  echo 'Pass the path to the extracted QuizDesk.app' >&2
  exit 1
fi
chmod +x "$app/Contents/MacOS/QuizDesk"
/usr/bin/codesign --force --deep --sign - "$app"
/usr/bin/codesign --verify --deep --strict "$app"
echo 'Local ad-hoc signing complete. This does not notarize a public release.'
