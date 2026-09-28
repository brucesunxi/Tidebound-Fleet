#!/usr/bin/env python3
"""Compile production C# rules, stage a new variable-fleet pack, or verify the installed pack.

Generation is staged outside Assets. The first 30 must pass before generating 31–100.
Use the Campaign V3 Unity menu to bind a certified pack without rebuilding the portrait UI.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import shutil
import subprocess
import tempfile

parser=argparse.ArgumentParser()
parser.add_argument('action',choices=['generate','validate'])
parser.add_argument('--count',type=int,choices=[30,100],default=100)
parser.add_argument('--stage',type=Path)
parser.add_argument('--unity',type=Path,default=Path('/Applications/Unity/Hub/Editor/2022.3.25f1/Unity.app'))
args=parser.parse_args()
project=Path(__file__).resolve().parents[2]
source=Path(__file__).parent
runtime=project/'Assets/Tidebound/Runtime'
mono=args.unity/'Contents/MonoBleedingEdge'
json_dll=next((project/'Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'))
if args.action=='generate' and (not args.stage or args.stage.resolve().is_relative_to(project/'Assets')):
    parser.error('Generation requires --stage outside Assets; published layouts are immutable.')
destination=args.stage.resolve() if args.stage else project/'Assets/Tidebound/Config/Levels/CampaignV3'
destination.mkdir(parents=True,exist_ok=True)
sources=sorted((runtime/'Data').rglob('*.cs'))+sorted((runtime/'Core').rglob('*.cs'))
sources += [runtime/'Unity/Loading'/(n+'.cs') for n in ['LevelJsonReader','LevelJsonWriter','LevelProofJson','CampaignPackValidator','CampaignV3Validator']]
sources += sorted(source.glob('DifficultyPrototypePipeline*.cs'))
with tempfile.TemporaryDirectory(prefix='tidebound-v3-build-') as tmp:
    work=Path(tmp);exe=work/'CampaignV3.exe';shutil.copy2(json_dll,work/json_dll.name)
    rsp=work/'compile.rsp'
    rsp.write_text('\n'.join(['-nologo','-langversion:9.0','-optimize+','-target:exe',f'-out:"{exe}"',f'-r:"{json_dll}"',f'-r:"{mono}/lib/mono/4.5/Facades/netstandard.dll"']+[f'"{f}"' for f in sources]))
    subprocess.run([str(mono/'bin/mono'),str(mono/'lib/mono/4.5/csc.exe'),'@'+str(rsp)],check=True)
    command=[str(mono/'bin/mono'),str(exe),str(project),'campaign-v3',str(destination)]
    def generate(n):
        logs=destination/'generation-logs';logs.mkdir(exist_ok=True)
        for retry in range(30):
            with (logs/f'{n:03d}-{retry}.txt').open('w') as log:
                result=subprocess.run(command+[str(n),str(retry)],stdout=log,stderr=log)
            if result.returncode==0:
                print(f'Level {n}: generated and certified (seed variant {retry})',flush=True)
                return
        raise RuntimeError(f'Level {n} exhausted its candidate budget; inspect generation-logs.')
    if args.action=='generate':
        library=destination/'core-library.json'
        if not library.exists():shutil.copy2(source/'CampaignV3/core-library.json',library)
        with ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(generate,range(1,31)))
        subprocess.run(command+['pack','30'],check=True)
        if args.count==100:
            with ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(generate,range(31,101)))
            subprocess.run(command+['pack','100'],check=True)
    else:subprocess.run(command+['validate-pack',str(args.count)],check=True)
