"""Build a macOS .app and a tar.gz retaining executable permissions, on any OS."""
import argparse
import os
import plistlib
import shutil
import struct
import subprocess
import sys
import tarfile
import re
import xml.etree.ElementTree as ET
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('publish', type=Path)
parser.add_argument('output', type=Path, help='A new directory; existing output is never overwritten')
parser.add_argument('--arch', choices=['arm64', 'x64'], required=True)
args = parser.parse_args()
source, output = args.publish.resolve(), args.output.resolve()
version = ET.parse(Path(__file__).resolve().parent / 'QuizDesk.Desktop' / 'QuizDesk.Desktop.csproj').findtext('.//Version')
if not version or not re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', version):
    raise SystemExit('Desktop project must declare a three-component release version')
host = source / 'QuizDesk'
header = host.read_bytes()[:8]
expected = {'arm64': 0x0100000C, 'x64': 0x01000007}[args.arch]
if header[:4] != b'\xcf\xfa\xed\xfe' or struct.unpack('<I', header[4:8])[0] != expected:
    raise SystemExit('The publish directory has the wrong Mach-O apphost architecture')
output.mkdir(parents=True, exist_ok=False)
app = output / 'QuizDesk.app'
binary = app / 'Contents' / 'MacOS'
resources = app / 'Contents' / 'Resources'
binary.mkdir(parents=True)
resources.mkdir()
for item in source.iterdir():
    if item.suffix in {'.pdb', '.log'}:
        continue
    if item.name in {'Samples', 'Licenses', 'README.md'}:
        target = resources / item.name
    else:
        target = binary / item.name
    if item.is_dir():
        shutil.copytree(item, target)
    else:
        shutil.copy2(item, target)
assets = Path(__file__).resolve().parent / 'Assets'
for name in ('Samples', 'Licenses'):
    if not (resources / name).exists():
        shutil.copytree(Path(__file__).resolve().parent / ('TestAssets/Samples' if name == 'Samples' else 'QuizDesk.Mobile/Assets/Licenses'), resources / name)
icon = assets / 'QuizDesk.icns'
if icon.exists():
    shutil.copy2(icon, resources / icon.name)
metadata = {
    'CFBundleName': 'QuizDesk', 'CFBundleDisplayName': 'QuizDesk',
    'CFBundleIdentifier': 'com.quizdesk.desktop', 'CFBundleExecutable': 'QuizDesk',
    'CFBundlePackageType': 'APPL', 'CFBundleVersion': version,
    'CFBundleShortVersionString': version, 'LSMinimumSystemVersion': '14.0',
    'NSHighResolutionCapable': True,
    'LSApplicationCategoryType': 'public.app-category.education',
    'CFBundleIconFile': 'QuizDesk.icns',
    'CFBundleDocumentTypes': [{'CFBundleTypeName': 'Bộ đề QuizDesk', 'CFBundleTypeRole': 'Viewer', 'LSHandlerRank': 'Owner', 'LSItemContentTypes': ['com.quizdesk.quiz']}],
    'UTExportedTypeDeclarations': [{'UTTypeIdentifier': 'com.quizdesk.quiz', 'UTTypeDescription': 'Bộ đề QuizDesk', 'UTTypeConformsTo': ['public.xml'], 'UTTypeTagSpecification': {'public.filename-extension': ['quiz'], 'public.mime-type': ['application/vnd.quizdesk.quiz']}}],
}
with (app / 'Contents' / 'Info.plist').open('wb') as stream:
    plistlib.dump(metadata, stream)
for file in binary.rglob('*'):
    if file.is_file() and (file.name == 'QuizDesk' or file.suffix == '.dylib'):
        file.chmod(0o755)
if sys.platform == 'darwin':
    # Local development signing; a public release still needs Developer ID and notarization.
    subprocess.run(['codesign', '--force', '--deep', '--sign', '-', str(app)], check=True)
readme = Path(__file__).resolve().parent / 'MACOS.txt'
shutil.copy2(readme, output / 'MACOS.txt')
shutil.copy2(Path(__file__).resolve().parent / 'prepare-local-macos.sh', output / 'prepare-local-macos.sh')
archive = output.parent / f'QuizDesk-{version}-macos-{args.arch}.tar.gz'
def permissions(info):
    info.uid = info.gid = 0
    info.uname = info.gname = ''
    info.mode = 0o755 if info.isdir() or info.name.endswith(('/QuizDesk', '.dylib', '.sh')) else 0o644
    return info
with tarfile.open(archive, 'w:gz') as tar:
    for item in output.iterdir():
        tar.add(item, arcname=item.name, filter=permissions)
with tarfile.open(archive) as tar:
    entry = tar.getmember('QuizDesk.app/Contents/MacOS/QuizDesk')
    assert entry.mode & 0o111, 'Executable permission was lost'
    assert tar.getmember('QuizDesk.app/Contents/Resources/Samples/De-mau.txt').size > 0
    assert tar.getmember('QuizDesk.app/Contents/MacOS/libAvaloniaNative.dylib').size > 0
    assert tar.getmember('QuizDesk.app/Contents/MacOS/libcoreclr.dylib').size > 0
print(f'Packaged {args.arch}: {archive}; apphost, native runtime, resources and executable mode verified.')

