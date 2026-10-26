"""Portable checks for macOS packaging; Apple commands are mocked, never executed."""
import hashlib
import importlib.util
import plistlib
import struct
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path
from unittest.mock import patch

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('native_installer', REPO / 'Installer/macOS/build-native-installer.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class MacPackageTests(unittest.TestCase):
    def fixture(self, directory, arch='arm64'):
        app = Path(directory) / 'QuizDesk.app'
        (app / 'Contents/MacOS').mkdir(parents=True)
        (app / 'Contents/Info.plist').write_bytes(plistlib.dumps({'CFBundleIdentifier': 'com.quizdesk.desktop', 'CFBundleShortVersionString': '5.13.0'}))
        (app / 'Contents/MacOS/QuizDesk').write_bytes(bytes.fromhex('cffaedfe') + struct.pack('<I', {'arm64': 0x0100000C, 'x64': 0x01000007}[arch]))
        return app

    def exercise(self, arch):
        with tempfile.TemporaryDirectory(prefix='quizdesk-pkg-test-') as temporary:
            root = Path(temporary)
            app = self.fixture(root, arch)
            output = root / f'QuizDesk-Setup-5.13.0-macos-{arch}.pkg'
            calls = []
            def fake_run(*arguments):
                arguments = list(map(str, arguments)); calls.append(arguments)
                command = Path(arguments[0]).name
                if command == 'pkgbuild':
                    stage = Path(arguments[arguments.index('--root') + 1]).parent
                    scripts = Path(arguments[arguments.index('--scripts') + 1])
                    pre = (scripts / 'preinstall').read_text()
                    post = (scripts / 'postinstall').read_text()
                    self.assertIn('pgrep -x QuizDesk', pre)
                    self.assertIn('"$apple_silicon" = "1"' if arch == 'arm64' else '"$apple_silicon" != "1"', pre)
                    self.assertIn('codesign --verify --deep --strict', post)
                    self.assertIn('lsregister', post)
                    self.assertNotIn('rm ', post)
                    components = plistlib.loads((stage / 'components.plist').read_bytes())
                    self.assertFalse(components[0]['BundleIsRelocatable'])
                    self.assertEqual(arguments[arguments.index('--install-location') + 1], '/Applications')
                    Path(arguments[-1]).write_bytes(b'inert-component-fixture')
                elif command == 'productbuild' and '--synthesize' in arguments:
                    Path(arguments[-1]).write_text('<installer-gui-script minSpecVersion="2"><title>Fixture</title><options/></installer-gui-script>')
                elif command == 'productbuild':
                    distribution = ET.parse(arguments[arguments.index('--distribution') + 1])
                    self.assertEqual(distribution.findtext('title'), 'QuizDesk 5.13.0')
                    self.assertEqual(distribution.find('options').get('hostArchitectures'), 'arm64' if arch == 'arm64' else 'x86_64')
                    self.assertEqual(distribution.find('volume-check/allowed-os-versions/os-version').get('min'), '14.0')
                    self.assertEqual(distribution.find('domains').get('enable_currentUserHome'), 'false')
                    Path(arguments[-1]).write_bytes(b'inert-product-fixture' * 100)
                elif command == 'xar':
                    return 'Distribution\nQuizDesk.component.pkg\nQuizDesk.component.pkg/Payload\n'
                elif command != 'codesign':
                    raise AssertionError(f'Unexpected command: {command}')
                return ''
            with patch.object(module.sys, 'platform', 'darwin'), patch.object(module, 'run', fake_run):
                module.build(app, output, arch, '5.13.0')
            manifest = root / f'QuizDesk-5.13.0-macos-{arch}-SHA256.txt'
            self.assertEqual(manifest.read_text(), f'{hashlib.sha256(output.read_bytes()).hexdigest()}  {output.name}\n')
            self.assertEqual([Path(command[0]).name for command in calls], ['codesign', 'pkgbuild', 'productbuild', 'productbuild', 'xar'])

    def test_arm64_native_packaging_arguments_and_manifest(self):
        self.exercise('arm64')

    def test_intel_native_packaging_arguments_and_manifest(self):
        self.exercise('x64')

    def test_wrong_architecture_fails_before_packaging(self):
        with tempfile.TemporaryDirectory() as temp:
            app = self.fixture(temp)
            with patch.object(module.sys, 'platform', 'darwin'), patch.object(module, 'run') as run:
                with self.assertRaises(SystemExit): module.build(app, Path(temp) / 'out.pkg', 'x64', '5.13.0')
                run.assert_not_called()

    def test_wrong_version_fails_before_packaging(self):
        with tempfile.TemporaryDirectory() as temp:
            app = self.fixture(temp)
            with patch.object(module.sys, 'platform', 'darwin'), patch.object(module, 'run') as run:
                with self.assertRaises(SystemExit): module.build(app, Path(temp) / 'out.pkg', 'arm64', '5.14.0')
                run.assert_not_called()

    def test_existing_output_is_never_overwritten(self):
        with tempfile.TemporaryDirectory() as temp:
            app = self.fixture(temp);output = Path(temp) / 'existing.pkg';output.write_bytes(b'original')
            with patch.object(module.sys, 'platform', 'darwin'), patch.object(module, 'run'):
                with self.assertRaises(SystemExit): module.build(app, output, 'arm64', '5.13.0')
            self.assertEqual(output.read_bytes(), b'original')

    def test_non_macos_host_is_rejected(self):
        with patch.object(module.sys, 'platform', 'win32'), patch.object(module, 'run') as run:
            with self.assertRaises(SystemExit): module.build(Path('unused.app'), Path('unused.pkg'), 'arm64', '5.13.0')
            run.assert_not_called()


dmg_spec = importlib.util.spec_from_file_location('dmg_builder', REPO / 'Installer/macOS/build-dmg.py')
dmg = importlib.util.module_from_spec(dmg_spec)
dmg_spec.loader.exec_module(dmg)


class DmgTests(unittest.TestCase):
    fixture = MacPackageTests.fixture
    def check_dmg(self, arch, fail_copy=False):
        import shutil
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            app = self.fixture(root, arch)
            output = root / f'QuizDesk-5.13.0-macos-{arch}.dmg'
            calls = []
            payload = None
            def fake_run(*arguments):
                nonlocal payload
                args = list(map(str, arguments)); calls.append(args)
                if Path(args[0]).name == 'hdiutil':
                    if args[1] == 'create':
                        payload = Path(args[args.index('-srcfolder') + 1])
                        output.write_bytes(b'inert-dmg-fixture')
                    elif args[1] == 'attach':
                        mount = Path(args[args.index('-mountpoint') + 1])
                        shutil.copytree(payload, mount, dirs_exist_ok=True, symlinks=True)
                elif fail_copy and Path(args[-1]).parent.name == 'copied':
                    raise RuntimeError('copied signature invalid')
            # Windows test hosts cannot create Unix symlinks without elevated rights.
            # Represent the shortcut as a file here; native CI checks the actual symlink.
            with patch.object(dmg.sys, 'platform', 'darwin'), patch.object(dmg, 'run', fake_run), \
                 patch.object(Path, 'symlink_to', lambda self, target, **kw: self.write_text(str(target))), \
                 patch.object(Path, 'is_symlink', lambda self: self.name == 'Applications'), \
                 patch.object(Path, 'readlink', return_value=Path('/Applications')):
                if fail_copy:
                    with self.assertRaisesRegex(RuntimeError, 'copied signature invalid'):
                        dmg.build(app, output, arch, '5.13.0')
                else:
                    dmg.build(app, output, arch, '5.13.0')
            self.assertEqual([c[1] for c in calls if Path(c[0]).name == 'hdiutil'], ['create', 'verify', 'attach', 'detach'])
            manifest = root / f'QuizDesk-5.13.0-macos-{arch}-DMG-SHA256.txt'
            if fail_copy:
                self.assertFalse(manifest.exists())
            else:
                self.assertEqual(manifest.read_text(), f'{hashlib.sha256(output.read_bytes()).hexdigest()}  {output.name}\n')
                self.assertEqual(len([c for c in calls if Path(c[0]).name == 'codesign']), 3)

    def test_arm64_dmg_mount_copy_and_checksum(self):
        self.check_dmg('arm64')

    def test_x64_dmg_mount_copy_and_checksum(self):
        self.check_dmg('x64')

    def test_failed_copied_signature_detaches_and_has_no_manifest(self):
        self.check_dmg('arm64', fail_copy=True)

    def test_dmg_wrong_architecture_rejected_without_apple_commands(self):
        with tempfile.TemporaryDirectory() as temporary:
            app = self.fixture(temporary)
            with patch.object(dmg.sys, 'platform', 'darwin'), patch.object(dmg, 'run') as run:
                with self.assertRaises(SystemExit): dmg.build(app, Path(temporary) / 'out.dmg', 'x64', '5.13.0')
                run.assert_not_called()

    def test_existing_dmg_is_not_overwritten(self):
        with tempfile.TemporaryDirectory() as temporary:
            app = self.fixture(temporary)
            output = Path(temporary) / 'existing.dmg'; output.write_bytes(b'original')
            with patch.object(dmg.sys, 'platform', 'darwin'), patch.object(dmg, 'run'):
                with self.assertRaises(SystemExit): dmg.build(app, output, 'arm64', '5.13.0')
            self.assertEqual(output.read_bytes(), b'original')


if __name__ == '__main__':
    unittest.main()
