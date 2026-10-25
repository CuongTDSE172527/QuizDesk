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
  device)
    : "${IOS_CODESIGN_KEY:?Set IOS_CODESIGN_KEY to your installed Apple signing identity}"
    : "${IOS_PROVISIONING_PROFILE:?Set IOS_PROVISIONING_PROFILE to your provisioning profile name or UUID}"
    dotnet publish QuizDesk.iOS/QuizDesk.iOS.csproj -c Release -r ios-arm64 -m:1 -p:ArchiveOnBuild=true -p:BuildIpa=true "-p:CodesignKey=$IOS_CODESIGN_KEY" "-p:CodesignProvision=$IOS_PROVISIONING_PROFILE" -o dist/ios
    ;;
  *) echo 'Use simulator or device' >&2; exit 1 ;;
esac
