"""Build and verify a drag-to-Applications DMG on macOS, without Installer scripts."""
import argparse
import hashlib
import plistlib
import re
import struct
import subprocess
import sys
import tempfile
from pathlib import Path


def run(*args):
    result = subprocess.run(list(map(str, args)), capture_output=True, text=True)
    if result.returncode:
        print(result.stdout + result.stderr, file=sys.stderr)
    result.check_returncode()


def validate(app, arch, version):
    info = plistlib.loads((app / 'Contents/Info.plist').read_bytes())
    if app.name != 'QuizDesk.app' or info.get('CFBundleIdentifier') != 'com.quizdesk.desktop' or info.get('CFBundleShortVersionString') != version:
        raise SystemExit('Application identity/version does not match the DMG release.')
    with (app / 'Contents/MacOS/QuizDesk').open('rb') as stream:
        header = stream.read(8)
    expected = {'arm64': 0x0100000C, 'x64': 0x01000007}[arch]
    if header[:4] != bytes.fromhex('cffaedfe') or struct.unpack('<I', header[4:])[0] != expected:
        raise SystemExit('Application architecture does not match the DMG release.')
    run('/usr/bin/codesign', '--verify', '--deep', '--strict', app)


def build(app, output, arch, version):
    if sys.platform != 'darwin':
        raise SystemExit('DMG packaging requires macOS.')
    if not re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', version):
        raise SystemExit('Invalid release version.')
    app, output = app.resolve(), output.resolve()
    validate(app, arch, version)
    if output.exists():
        raise SystemExit('Refusing to overwrite an existing disk image.')
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='quizdesk-dmg-') as temporary:
        stage = Path(temporary)
        payload, mount = stage / 'payload', stage / 'mount'
        payload.mkdir(); mount.mkdir()
        # Non-Mach-O DLL signatures live in extended attributes. shutil.copytree
        # on macOS drops these, even if the original bundle passes codesign.
        staged_app = payload / app.name
        run('/usr/bin/ditto', '--rsrc', '--extattr', app, staged_app)
        validate(staged_app, arch, version)
        (payload / 'Applications').symlink_to('/Applications', target_is_directory=True)
        (payload / 'INSTALL.txt').write_text(
            'QuizDesk ' + version + ' (' + arch + ') — macOS 14+\n\n'
            '1. Quit QuizDesk if it is running.\n'
            '2. Drag QuizDesk.app onto Applications. Replace the previous app if updating.\n'
            '3. Eject this disk image, then open QuizDesk from Applications.\n'
            'Your saved quizzes, settings and history are stored separately in your user account.\n\n'
            'This development build is ad-hoc signed, not Developer ID signed or notarized.\n'
            'If macOS blocks an unverified developer, use Privacy & Security > Open Anyway\n'
            'only if you trust this build. No system-wide security changes are needed.\n',
            encoding='utf-8')
        run('/usr/bin/hdiutil', 'create', '-srcfolder', payload, '-volname', 'QuizDesk ' + version,
            '-fs', 'HFS+', '-format', 'UDZO', output)
        run('/usr/bin/hdiutil', 'verify', output)
        run('/usr/bin/hdiutil', 'attach', '-readonly', '-nobrowse', '-mountpoint', mount, output)
        try:
            if not (mount / 'Applications').is_symlink() or (mount / 'Applications').readlink() != Path('/Applications'):
                raise SystemExit('DMG Applications shortcut is missing or incorrect.')
            # Check the actual mounted payload, then a Finder-style copied bundle.
            # Do not overwrite /Applications or launch the app on the CI runner.
            validate(mount / app.name, arch, version)
            copied = stage / 'copied' / app.name
            copied.parent.mkdir()
            run('/usr/bin/ditto', '--rsrc', '--extattr', mount / app.name, copied)
            validate(copied, arch, version)
        finally:
            run('/usr/bin/hdiutil', 'detach', mount)
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    manifest = output.parent / f'QuizDesk-{version}-macos-{arch}-DMG-SHA256.txt'
    manifest.write_text(f'{digest}  {output.name}\n', encoding='ascii')
    print(f'PASS {output.name}: disk image, mounted payload, copied app signature, version and architecture verified.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--app', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--arch', choices=['arm64', 'x64'], required=True)
    parser.add_argument('--version', required=True)
    args = parser.parse_args()
    build(args.app, args.output, args.arch, args.version)
