"""Install the arm64 PKG on an ephemeral GitHub macOS runner, with diagnostics.

This performs a real system installation. It is a CI check, not a user repair tool.
"""
import argparse
import platform
import plistlib
import struct
import subprocess
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', type=Path, required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--logs', type=Path, required=True)
    args = parser.parse_args()
    if sys.platform != 'darwin' or platform.machine() != 'arm64':
        parser.error('This real installation check requires an Apple Silicon macOS runner.')
    package = args.package.resolve(strict=True)
    if package.suffix != '.pkg':
        parser.error('Expected a PKG installer.')
    args.logs.mkdir(parents=True, exist_ok=True)

    def run(name, command):
        result = subprocess.run(command, capture_output=True, text=True, errors='replace')
        output = result.stdout + result.stderr
        (args.logs / name).write_text(output, encoding='utf-8')
        print(output, flush=True)
        result.check_returncode()

    try:
        run('installer.log', ['sudo', '-n', '/usr/sbin/installer', '-verboseR', '-dumplog',
                              '-pkg', str(package), '-target', '/'])
        app = Path('/Applications/QuizDesk.app')
        info = plistlib.loads((app / 'Contents/Info.plist').read_bytes())
        if (info.get('CFBundleIdentifier'), info.get('CFBundleShortVersionString')) != ('com.quizdesk.desktop', args.version):
            raise RuntimeError('Installed application identity/version mismatch.')
        with (app / 'Contents/MacOS/QuizDesk').open('rb') as stream:
            header = stream.read(8)
        if header[:4] != bytes.fromhex('cffaedfe') or struct.unpack('<I', header[4:])[0] != 0x0100000C:
            raise RuntimeError('Installed application is not arm64.')
        run('codesign.log', ['/usr/bin/codesign', '--verify', '--deep', '--strict', str(app)])
        print('PASS real Apple Silicon PKG installation, installed version, architecture and signature.')
    except Exception as error:
        (args.logs / 'failure.txt').write_text(str(error), encoding='utf-8')
        raise
    finally:
        result = subprocess.run(['sudo', '-n', '/usr/bin/tail', '-n', '500', '/var/log/install.log'],
                                capture_output=True, text=True, errors='replace')
        (args.logs / 'system-install.log').write_text(result.stdout + result.stderr, encoding='utf-8')


if __name__ == '__main__':
    main()
