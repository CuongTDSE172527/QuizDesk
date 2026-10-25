"""Create a native macOS Installer product archive on Windows/macOS/Linux.

Requires macospkg 0.7.3 (MIT): https://github.com/deploymenttheory/go-macos-pkg
The package is unsigned. No Developer ID or notarization is implied.
"""
import argparse
import hashlib
import json
import plistlib
import re
import shutil
import struct
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--tool', type=Path, required=True)
parser.add_argument('--app', type=Path, required=True)
parser.add_argument('--arch', choices=['arm64', 'x64'], required=True)
parser.add_argument('--work', type=Path, required=True, help='New staging directory')
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
tool, app, stage, output = (p.resolve() for p in (args.tool, args.app, args.work, args.output))
info = plistlib.loads((app / 'Contents/Info.plist').read_bytes())
if info['CFBundleIdentifier'] != 'com.quizdesk.desktop' or app.name != 'QuizDesk.app':
    raise SystemExit('Unexpected app identity')
version = info['CFBundleShortVersionString']
header = (app / 'Contents/MacOS/QuizDesk').read_bytes()[:8]
if header[:4] != b'\xcf\xfa\xed\xfe' or struct.unpack('<I', header[4:])[0] != {'arm64': 0x0100000c, 'x64': 0x01000007}[args.arch]:
    raise SystemExit('The apphost architecture does not match this installer')
stage.mkdir(parents=True, exist_ok=False)
root, scripts, resources = (stage / n for n in ('root', 'scripts', 'resources'))
root.mkdir(); scripts.mkdir(); resources.mkdir()
shutil.copytree(app, root / app.name)
preinstall = '''#!/bin/sh
set -eu
if /usr/bin/pgrep -x QuizDesk >/dev/null 2>&1; then
  echo "Close QuizDesk before installing this update; your open quiz may have unsaved changes." >&2
  exit 1
fi
apple_silicon=$(/usr/sbin/sysctl -n hw.optional.arm64 2>/dev/null || printf '0')
EXPECTED_ARCH
exit 0
'''
arch_check = ('if [ "$apple_silicon" != "1" ]; then echo "Choose the Intel installer for this Mac." >&2; exit 1; fi'
              if args.arch == 'arm64' else
              'if [ "$apple_silicon" = "1" ]; then echo "Choose the Apple Silicon installer for this Mac." >&2; exit 1; fi')
(scripts / 'preinstall').write_text(preinstall.replace('EXPECTED_ARCH', arch_check), encoding='utf-8', newline='\n')
postinstall = '''#!/bin/sh
set -eu
volume="${3:-/}"
app="${volume%/}/Applications/QuizDesk.app"
identifier=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$app/Contents/Info.plist")
if [ "$identifier" != "com.quizdesk.desktop" ]; then
  echo "The installed application identity is unexpected." >&2
  exit 1
fi
# Development ad-hoc signing happens on the destination Mac, without a certificate.
# This does NOT create a Developer ID signature or a notarization ticket.
/usr/bin/codesign --force --deep --sign - "$app"
/usr/bin/codesign --verify --deep --strict "$app"
exit 0
'''
(scripts / 'postinstall').write_text(postinstall, encoding='utf-8', newline='\n')
for script in scripts.iterdir():
    script.chmod(0o755)
welcome = f'''<!DOCTYPE html><html lang="vi"><meta charset="utf-8"><body style="font-family:-apple-system;font-size:14px">
<h1>QuizDesk {version}</h1><p>Bản dành cho {'Apple Silicon (M1/M2/M3/M4/...)' if args.arch == 'arm64' else 'Mac Intel'} · macOS 14 trở lên.</p>
<p>Cài vào <b>/Applications/QuizDesk.app</b>. Hãy đóng QuizDesk trước khi tiếp tục.</p>
<p>Giữ nguyên bộ đề, Settings và Lịch sử trong tài khoản của bạn. Đã kèm .NET runtime, không cần Microsoft Office.</p>
<p>Đây là bộ cài thử nghiệm, chưa được chạy thử trên máy Mac. Chưa có chữ ký Developer ID hoặc notarization.
Ứng dụng được ký ad-hoc cục bộ sau khi cài; đây không phải chứng nhận của Apple.</p></body></html>'''
(resources / 'welcome.html').write_text(welcome, encoding='utf-8')
conclusion = '''<!DOCTYPE html><html lang="vi"><meta charset="utf-8"><body style="font-family:-apple-system;font-size:14px">
<h1>Đã cài QuizDesk</h1><p>Mở Applications → QuizDesk để bắt đầu.</p>
<p>Settings và Lịch sử được lưu trong <code>~/Library/Application Support/QuizDesk</code>.
Gỡ ứng dụng bằng cách đưa QuizDesk.app vào Thùng rác; giữ thư mục dữ liệu nếu muốn giữ lịch sử.</p>
<p>Bộ cài này chưa ký Developer ID/notarize và chưa nghiệm thu trên macOS. Nếu macOS chặn bộ cài tải về,
chỉ dùng Open Anyway trong Privacy &amp; Security khi bạn tin cậy bản build này.</p></body></html>'''
(resources / 'conclusion.html').write_text(conclusion, encoding='utf-8')
component = stage / 'QuizDesk.component.pkg'
def run(*command, capture=False):
    result = subprocess.run([str(tool), *map(str, command)], check=True, capture_output=capture, text=True, encoding='utf-8')
    return result.stdout if capture else None
run('build', root, component, '--identifier', 'com.quizdesk.desktop.pkg', '--version', version,
    '--install-location', '/Applications', '--scripts', scripts, '--min-os-version', '14.0',
    '--no-bundle-relocation', '--auth', 'root', '--ownership', 'recommended',
    '--executable', r'(^|/)(QuizDesk|createdump|[^/]+\.dylib)$')
distribution = stage / 'Distribution.xml'
host_arch = 'arm64' if args.arch == 'arm64' else 'x86_64'
run('product', distribution, '--package', component, '--title', f'QuizDesk {version}',
    '--identifier', 'com.quizdesk.desktop.installer', '--version', version,
    '--min-os-version', '14.0', '--host-architectures', host_arch, '--synthesize')
tree = ET.parse(distribution)
document = tree.getroot()
document.find('options').set('hostArchitectures', host_arch)
ET.SubElement(document, 'welcome', {'file': 'welcome.html', 'mime-type': 'text/html'})
ET.SubElement(document, 'conclusion', {'file': 'conclusion.html', 'mime-type': 'text/html'})
# This app is installed for the machine; per-user Settings/History stay outside the payload.
ET.SubElement(document, 'domains', {'enable_anywhere': 'false', 'enable_currentUserHome': 'false', 'enable_localSystem': 'true'})
tree.write(distribution, encoding='utf-8', xml_declaration=True)
output.parent.mkdir(parents=True, exist_ok=True)
run('product', output, '--distribution', distribution, '--package-path', stage, '--resources', resources)
# Validate both archive checksums and each payload entry's BOM CRC before comparing SHA-256.
expanded = stage / 'expanded'
run('expand', output, expanded, '--verify')
extracted = stage / 'extracted'
run('extract', output, extracted, '--verify')
expected = list((root / app.name).rglob('*'))
verified = 0
for original in expected:
    if not original.is_file():
        continue
    relative = original.relative_to(root)
    copies = [p for p in extracted.rglob(original.name) if p.as_posix().endswith(relative.as_posix())]
    if len(copies) != 1 or hashlib.sha256(original.read_bytes()).digest() != hashlib.sha256(copies[0].read_bytes()).digest():
        raise SystemExit(f'Installed payload hash mismatch: {relative}')
    verified += 1
report = {'package': output.name, 'version': version, 'architecture': args.arch,
          'installed_location': '/Applications/QuizDesk.app', 'payload_files_sha256_matched': verified,
          'xar_checksums': 'passed', 'bom_crc_checks': 'passed',
          'signature': 'unsigned package; app ad-hoc signed on destination Mac',
          'macos_runtime_installation_test': 'not performed; Windows build host'}
(stage / 'validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False))
