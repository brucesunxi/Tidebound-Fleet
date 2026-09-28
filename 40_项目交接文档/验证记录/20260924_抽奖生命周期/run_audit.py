#!/usr/bin/env python3
"""Read-only economy audit using the project's actual C# draw implementation.

Run with Python 3. Compiler artifacts are temporary; results stay beside this file.
Never opens Unity, reads player files, or edits production code/configuration.
"""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import tempfile

here = Path(__file__).resolve().parent
root = here.parents[2]
project = root / '10_开发工作区/TideboundFleet_Unity'
runtime = project / 'Assets/Tidebound/Runtime'
mono_root = Path('/Applications/Unity/Hub/Editor/2022.3.25f1/Unity.app/Contents/MonoBleedingEdge')
mono = mono_root / 'bin/mono'
sources = sorted((runtime / 'Data').rglob('*.cs')) + sorted((runtime / 'Core').rglob('*.cs'))
json_dll = next((project / 'Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'))
with tempfile.TemporaryDirectory(prefix='tidebound-draw-audit-') as temp:
    temp = Path(temp)
    shutil.copy2(json_dll, temp / 'Newtonsoft.Json.dll')
    rsp = temp / 'compile.rsp'
    rsp.write_text('\n'.join(['-nologo', '-langversion:9.0', '-optimize+', '-target:exe',
        '-out:"' + str(temp / 'Audit.exe') + '"', '-r:"' + str(json_dll) + '"',
        '-r:"' + str(mono_root / 'lib/mono/4.5/Facades/netstandard.dll') + '"'] +
        ['"' + str(p) + '"' for p in sources + [here / 'DrawLifecycleAudit.cs']]))
    subprocess.run([str(mono), str(mono_root / 'lib/mono/4.5/csc.exe'), '@' + str(rsp)], check=True)
    result = subprocess.run([str(mono), str(temp / 'Audit.exe')], check=True, text=True, capture_output=True)
    parsed = json.loads(result.stdout)
    parsed['sourceSha256'] = {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest()
        for p in sources + [here / 'DrawLifecycleAudit.cs', Path(__file__).resolve()]}
    (here / 'RESULTS.json').write_text(json.dumps(parsed, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps({k: v for k, v in parsed.items() if k != 'sourceSha256'}, ensure_ascii=False, indent=2))
