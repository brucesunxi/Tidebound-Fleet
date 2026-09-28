#!/usr/bin/env python3
"""Compile and run the actual C# content pipeline without opening or mutating the user's Unity Editor.

Usage: python3 Tools/Levels/run_campaign.py generate 30
       python3 Tools/Levels/run_campaign.py validate 30
Then generate/validate 100. No external Python packages or network required.
"""
import argparse
from pathlib import Path
import shutil
import subprocess
import tempfile
import uuid

parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['generate', 'validate'])
parser.add_argument('count', type=int, choices=[30, 100])
parser.add_argument('--unity', type=Path, default=Path('/Applications/Unity/Hub/Editor/2022.3.25f1/Unity.app'))
args = parser.parse_args()
project = Path(__file__).resolve().parents[2]
runtime = project / 'Assets/Tidebound/Runtime'
mono_root = args.unity / 'Contents/MonoBleedingEdge'
mono = mono_root / 'bin/mono'
compiler = mono_root / 'lib/mono/4.5/csc.exe'
json_dll = next((project / 'Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'))
sources = sorted((runtime / 'Data').rglob('*.cs')) + sorted((runtime / 'Core').rglob('*.cs'))
sources += [runtime / 'Unity/Loading' / (name + '.cs') for name in (
    'LevelJsonReader', 'LevelJsonWriter', 'LevelProofJson', 'LevelCandidateJson', 'CampaignPackValidator')]
sources += [Path(__file__).with_name('CampaignPipeline.cs')]
with tempfile.TemporaryDirectory(prefix='tidebound-campaign-') as tmp:
    directory = Path(tmp)
    shutil.copy2(json_dll, directory / 'Newtonsoft.Json.dll')
    response = directory / 'compile.rsp'
    response.write_text('\n'.join(['-nologo', '-langversion:9.0', '-optimize+', '-target:exe',
        '-out:"' + str(directory / 'Campaign.exe') + '"', '-r:"' + str(json_dll) + '"',
        '-r:"' + str(mono_root / 'lib/mono/4.5/Facades/netstandard.dll') + '"'] +
        ['"' + str(p) + '"' for p in sources]))
    subprocess.run([str(mono), str(compiler), '@' + str(response)], check=True)
    subprocess.run([str(mono), str(directory / 'Campaign.exe'), str(project), args.action, str(args.count)], check=True)

# Stable asset identities also when generating with no Editor open. Preserve any existing Unity GUID.
campaign = project / 'Assets/Tidebound/Config/Levels/Campaign'
for asset in [campaign] + sorted(campaign.glob('*.json')):
    meta = Path(str(asset) + '.meta')
    if not meta.exists():
        guid = uuid.uuid5(uuid.NAMESPACE_URL, 'tidebound/campaign/' + str(asset.relative_to(project))).hex
        importer = ('folderAsset: yes\nDefaultImporter:' if asset.is_dir() else 'TextScriptImporter:')
        meta.write_text(f'fileFormatVersion: 2\nguid: {guid}\n{importer}\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
