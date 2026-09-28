"""Research-only compile/run: unchanged production Data/Core sources, local Unity Mono.

Run from any directory with Python 3.8+. Binary/intermediates stay under 99_垃圾存储区;
the retained JSON contains source SHA-256s for detecting future source drift.
"""
from pathlib import Path
import hashlib
import json
import subprocess

EVIDENCE = Path(__file__).resolve().parent
ROOT = EVIDENCE.parents[2]
RUNTIME = ROOT / "10_开发工作区/TideboundFleet_Unity/Assets/Tidebound/Runtime"
TEMP = ROOT / "99_垃圾存储区/20260927_难度原型复核"
MONO = Path("/Applications/Unity/Hub/Editor/2022.3.25f1/Unity.app/Contents/MonoBleedingEdge/bin")
TEMP.mkdir(parents=True, exist_ok=True)
sources = sorted((RUNTIME / "Data").rglob("*.cs")) + sorted((RUNTIME / "Core").rglob("*.cs"))
binary = TEMP / "PrototypeRuntimeHarness.exe"
subprocess.run([str(MONO / "mcs"), "-langversion:latest", "-out:" + str(binary),
                str(EVIDENCE / "PrototypeRuntimeHarness.cs"), *(str(x) for x in sources)], check=True)
result = subprocess.run([str(MONO / "mono"), str(binary)], check=True, capture_output=True, text=True)
document = json.loads(result.stdout)
document["source_sha256"] = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
document["harness_sha256"] = hashlib.sha256((EVIDENCE / "PrototypeRuntimeHarness.cs").read_bytes()).hexdigest()
(EVIDENCE / "runtime_verification.json").write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n")
binary.unlink()
try:
    TEMP.rmdir()
except OSError:
    pass  # Preserve anything not created by this run.
for prototype in document["prototypes"]:
    print(json.dumps({k: prototype[k] for k in ["name", "minimum_effective_actions", "minimum_necessary_partial_moves",
                       "full_reachable_graph", "initial_effective_actions"]}, ensure_ascii=False))
