#!/bin/sh
set -eu
cd "$(dirname "$0")"
if [ "$(uname -s)" != Darwin ]; then echo 'iOS build requires macOS and Xcode.' >&2; exit 1; fi
export AVALONIA_TELEMETRY_OPTOUT=1
xcodebuild -version
case "${1:-simulator}" in
  simulator)
    case "$(uname -m)" in arm64) rid=iossimulator-arm64 ;; *) rid=iossimulator-x64 ;; esac
    dotnet build QuizDesk.iOS/QuizDesk.iOS.csproj -c Debug -r "$rid" -p:EnableCodeSigning=false -m:1
    echo 'Simulator app built. Install the .app from bin/Debug/net10.0-ios/<rid>/ using xcrun simctl install booted <app-path>.'
    ;;
  unsigned)
    # Compile for real iPhones, with Release AOT. A simulator bundle cannot be sideloaded.
    dotnet publish QuizDesk.iOS/QuizDesk.iOS.csproj -c Release -r ios-arm64 -m:1 \
      -p:EnableCodeSigning=false -p:CodesignRequireProvisioningProfile=false \
      -p:ArchiveOnBuild=false -p:BuildIpa=false
    app='QuizDesk.iOS/bin/Release/net10.0-ios/ios-arm64/QuizDesk.iOS.app'
    test -d "$app" || { echo 'Device .app was not produced.' >&2; exit 1; }
    executable=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Info.plist")
    xcrun lipo -verify_arch arm64 "$app/$executable"
    xcrun vtool -show-build "$app/$executable" | grep -Eq 'platform[[:space:]]+IOS$' || {
      echo 'Expected an iPhone device binary, not a simulator binary.' >&2; exit 1;
    }
    mkdir -p dist/ios
    output="$(pwd)/dist/ios/QuizDesk-ios-arm64-unsigned.ipa"
    staging=$(mktemp -d)
    trap 'rm -rf "$staging"' EXIT HUP INT TERM
    mkdir -p "$staging/Payload"
    ditto "$app" "$staging/Payload/QuizDesk.iOS.app"
    # Preserve bundle structure, permissions and symlinks in the standard IPA layout.
    (cd "$staging" && ditto -c -k --keepParent Payload "$output")
    unzip -t "$output"
    shasum -a 256 "$output" > "$output.sha256"
    echo "Unsigned device IPA: $output"
    echo 'Sign and install with AltStore Classic/AltServer. This IPA cannot be installed directly.'
    ;;
  device)
    : "${IOS_CODESIGN_KEY:?Set IOS_CODESIGN_KEY to your installed Apple signing identity}"
    : "${IOS_PROVISIONING_PROFILE:?Set IOS_PROVISIONING_PROFILE to your provisioning profile name or UUID}"
    dotnet publish QuizDesk.iOS/QuizDesk.iOS.csproj -c Release -r ios-arm64 -m:1 -p:ArchiveOnBuild=true -p:BuildIpa=true "-p:CodesignKey=$IOS_CODESIGN_KEY" "-p:CodesignProvision=$IOS_PROVISIONING_PROFILE" -o dist/ios
    ;;
  *) echo 'Use simulator, unsigned, or device' >&2; exit 1 ;;
esac
