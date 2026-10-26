"""Create a native unsigned .pkg on macOS using Apple's pkgbuild/productbuild."""
import argparse
import hashlib
import plistlib
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def run(*args):
    try:
        return subprocess.run(list(map(str, args)), check=True, capture_output=True, text=True).stdout
    except subprocess.CalledProcessError as error:
        if error.stdout:
            print(error.stdout, file=sys.stderr)
        if error.stderr:
            print(error.stderr, file=sys.stderr)
        raise


def build(app: Path, output: Path, arch: str, version: str):
    if sys.platform != 'darwin':
        raise SystemExit('Native macOS packaging requires macOS.')
    if not re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', version):
        raise SystemExit('Invalid package version.')
    app, output = app.resolve(), output.resolve()
    info = plistlib.loads((app / 'Contents/Info.plist').read_bytes())
    if app.name != 'QuizDesk.app' or info.get('CFBundleIdentifier') != 'com.quizdesk.desktop' or info.get('CFBundleShortVersionString') != version:
        raise SystemExit('Application identity/version does not match the release.')
    header = (app / 'Contents/MacOS/QuizDesk').read_bytes()[:8]
    expected = {'arm64': 0x0100000C, 'x64': 0x01000007}[arch]
    if header[:4] != bytes.fromhex('cffaedfe') or struct.unpack('<I', header[4:])[0] != expected:
        raise SystemExit('Application architecture does not match the installer.')
    run('/usr/bin/codesign', '--verify', '--deep', '--strict', app)
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        raise SystemExit('Refusing to overwrite an existing installer.')
    with tempfile.TemporaryDirectory(prefix='quizdesk-pkg-') as temp:
        stage = Path(temp)
        root, scripts = stage / 'root', stage / 'scripts'
        root.mkdir(); scripts.mkdir()
        shutil.copytree(app, root / app.name)
        check = ('[ "$apple_silicon" = "1" ]' if arch == 'arm64' else '[ "$apple_silicon" != "1" ]')
        preinstall = '''#!/bin/sh
set -eu
if /usr/bin/pgrep -x QuizDesk >/dev/null 2>&1; then
  echo 'Close QuizDesk before installing this update.' >&2
  exit 1
fi
apple_silicon=$(/usr/sbin/sysctl -n hw.optional.arm64 2>/dev/null || printf '0')
ARCH_CHECK || { echo 'Choose the QuizDesk installer matching your Mac architecture.' >&2; exit 1; }
'''.replace('ARCH_CHECK', check)
        postinstall = '''#!/bin/sh
set -eu
volume="${3:-/}"
app="${volume%/}/Applications/QuizDesk.app"
identifier=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$app/Contents/Info.plist")
[ "$identifier" = 'com.quizdesk.desktop' ] || exit 1
/usr/bin/codesign --verify --deep --strict "$app"
register='/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister'
if [ -x "$register" ]; then "$register" -f "$app" || echo 'Launch Services will refresh when QuizDesk opens.' >&2; fi
'''
        for name, text in [('preinstall', preinstall), ('postinstall', postinstall)]:
            file = scripts / name
            file.write_text(text, encoding='utf-8', newline='\n'); file.chmod(0o755)
        # Disable bundle relocation: updates consistently target /Applications.
        components = stage / 'components.plist'
        components.write_bytes(plistlib.dumps([{'RootRelativeBundlePath': 'QuizDesk.app', 'BundleIsRelocatable': False, 'BundleIsVersionChecked': True, 'BundleHasStrictIdentifier': True, 'BundleOverwriteAction': 'upgrade'}]))
        component = stage / 'QuizDesk.component.pkg'
        run('/usr/bin/pkgbuild', '--root', root, '--scripts', scripts, '--component-plist', components, '--identifier', 'com.quizdesk.desktop.pkg', '--version', version, '--install-location', '/Applications', '--ownership', 'recommended', component)
        distribution = stage / 'Distribution.xml'
        run('/usr/bin/productbuild', '--synthesize', '--package', component, distribution)
        tree = ET.parse(distribution)
        document = tree.getroot()
        options = document.find('options')
        if options is None:
            options = ET.SubElement(document, 'options')
        options.set('hostArchitectures', 'arm64' if arch == 'arm64' else 'x86_64')
        options.set('customize', 'never')
        title = document.find('title')
        if title is None:
            title = ET.SubElement(document, 'title')
        title.text = f'QuizDesk {version}'
        volume_check = ET.SubElement(document, 'volume-check')
        ET.SubElement(ET.SubElement(volume_check, 'allowed-os-versions'), 'os-version', {'min': '14.0'})
        ET.SubElement(document, 'domains', {'enable_anywhere': 'false', 'enable_currentUserHome': 'false', 'enable_localSystem': 'true'})
        tree.write(distribution, encoding='utf-8', xml_declaration=True)
        run('/usr/bin/productbuild', '--distribution', distribution, '--package-path', stage, '--identifier', 'com.quizdesk.desktop.installer', '--version', version, output)
        contents = run('/usr/bin/xar', '-tf', output).splitlines()
        if 'Distribution' not in contents or not any('QuizDesk.component.pkg' in name for name in contents):
            raise SystemExit('Built product archive does not contain its distribution/component package.')
        if output.stat().st_size < 1024:
            raise SystemExit('Installer is unexpectedly small.')
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    manifest = output.parent / f'QuizDesk-{version}-macos-{arch}-SHA256.txt'
    manifest.write_text(f'{digest}  {output.name}\n', encoding='ascii')
    print(f'Built {output.name}. Package is unsigned; app is ad-hoc signed, not Developer ID signed or notarized.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--app', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--arch', choices=['arm64', 'x64'], required=True)
    parser.add_argument('--version', required=True)
    args = parser.parse_args()
    build(args.app, args.output, args.arch, args.version)
