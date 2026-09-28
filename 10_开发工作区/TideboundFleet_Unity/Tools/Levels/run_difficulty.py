#!/usr/bin/env python3
"""Build/validate the isolated DifficultyV2 review pack with actual production C# rules.

Never edits campaign assets, scenes, or player saves. Generation refuses an existing manifest.
"""
import argparse
from pathlib import Path
import shutil
import subprocess
import tempfile
import uuid

parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['generate', 'validate', 'refine-bridges', 'audit-risk'])
parser.add_argument('--unity', type=Path, default=Path('/Applications/Unity/Hub/Editor/2022.3.25f1/Unity.app'))
args = parser.parse_args()
project = Path(__file__).resolve().parents[2]
runtime = project / 'Assets/Tidebound/Runtime'
mono_root = args.unity / 'Contents/MonoBleedingEdge'
json_dll = next((project / 'Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'))
sources = sorted((runtime / 'Data').rglob('*.cs')) + sorted((runtime / 'Core').rglob('*.cs'))
sources += [runtime / 'Unity/Loading' / (name + '.cs') for name in ('LevelJsonReader', 'LevelJsonWriter', 'LevelProofJson', 'CampaignPackValidator', 'CampaignV3Validator')]
sources += sorted(Path(__file__).parent.glob('DifficultyPrototypePipeline*.cs'))
with tempfile.TemporaryDirectory(prefix='tidebound-difficulty-') as tmp:
    directory = Path(tmp)
    shutil.copy2(json_dll, directory / 'Newtonsoft.Json.dll')
    response = directory / 'compile.rsp'
    response.write_text('\n'.join(['-nologo', '-langversion:9.0', '-optimize+', '-target:exe',
        '-out:"' + str(directory / 'Difficulty.exe') + '"', '-r:"' + str(json_dll) + '"',
        '-r:"' + str(mono_root / 'lib/mono/4.5/Facades/netstandard.dll') + '"'] + ['"' + str(p) + '"' for p in sources]))
    subprocess.run([str(mono_root / 'bin/mono'), str(mono_root / 'lib/mono/4.5/csc.exe'), '@' + str(response)], check=True)
    subprocess.run([str(mono_root / 'bin/mono'), str(directory / 'Difficulty.exe'), str(project), args.action], check=True)

if args.action == 'generate':
    folder = project / 'Assets/Tidebound/Config/LevelPrototypes/DifficultyV2'
    for asset in [folder] + sorted(folder.glob('*.json')):
        meta = Path(str(asset) + '.meta')
        if not meta.exists():
            guid = uuid.uuid5(uuid.NAMESPACE_URL, 'tidebound/difficulty-v2/' + str(asset.relative_to(project))).hex
            importer = 'folderAsset: yes\nDefaultImporter:' if asset.is_dir() else 'TextScriptImporter:'
            meta.write_text(f'fileFormatVersion: 2\nguid: {guid}\n{importer}\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
