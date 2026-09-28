# Campaign content pipeline

## Current campaign: V3

The portrait scene now uses `Config/Levels/CampaignV3/manifest-100.json`. It has 100 active layouts (7, then 80–92 ships), 100 proofs and archived references to the original 100 layouts. The older commands below continue to validate the historical pack; do not use the legacy **Use Validated 100** menu to update the current game.

- Browse/edit: **Tools → Tidebound → Campaign V3 → Browse New 100 Levels**. Save edits as a new revision; published V3 paths are protected.
- Verify: `python3 Tools/Levels/run_campaign_v3.py validate`.
- Stage a replacement: `python3 Tools/Levels/run_campaign_v3.py generate --stage /tmp/tidebound-next-candidates --count 100`. Generation certifies 30 before extending to 100 and retries a rejected seed at most 30 times. Existing staged assets are re-certified by the pack gate; no save data or scene is touched.
- Install only after model, Unity animation, migration and visual verification. **Campaign V3 → Use Validated 100 In Current Portrait Layout** changes catalog references only and refuses an unsaved loaded scene.

V3 validates correct routes, actual reverse-tool recovery from 191 proven deadlocks, one attack per initial ship, checkpoint restore, direction mixing along every edge and local row/column, and reflection-aware geometry similarity even when ship counts differ. From level 4, an exit-only strategy cannot clear the board. These are structural checks, not human failure/tool-use rates. The current relation library has 24 authored-and-mutated crossing/stopper variants; it is not 24 unrelated mechanics. The 100 layouts retain rectangular 14×18 placement bounds; irregular silhouettes, late-game delayed traps and release-wave pacing remain playtest/refinement work.


Difficulty v2 review entry: `Tools → Tidebound → Difficulty v2 → Open Review Scene`. The independent pack has six A contrasts, two small dynamic prototypes and two 80-ship bridges; it is not a replacement campaign manifest. `python3 Tools/Levels/run_difficulty.py validate` replays its full-rule proofs and release traces. Generation refuses an existing review manifest. R contrasts preserve each original ship footprint; the 220-cell V mask constrains initial placement only. Human review, dynamic hints/feedback and content/save revision routing remain separate release gates.

Run from the Unity project root. The tool compiles the actual Data, Core and JSON C# sources with the locally installed Unity Mono compiler; it does not reimplement the rules in Python or open the user's Editor. Requires an already imported Newtonsoft package in Library/PackageCache. `--unity` selects another local Unity 2022.3 installation.

```sh
python3 Tools/Levels/run_campaign.py generate 30
python3 Tools/Levels/run_campaign.py validate 30
# Run CampaignPackTests and CampaignPlaybackTests in Unity before expanding.
python3 Tools/Levels/run_campaign.py generate 100
python3 Tools/Levels/run_campaign.py validate 100
```

The 100-level generation gate requires a passing 30-level report bound to the current manifest and production source fingerprint. Unity animation/test XML is reviewed separately. Files are kept in Assets/Tidebound/Config/Levels/Campaign; the first ten continue referencing their original assets. Every existing candidate is revalidated on resume; invalid or changed data causes an error instead of silent replacement. To intentionally revise a shipped level, introduce a reviewed revision/migration; do not delete files to regenerate in place.

Each new recipe tries at most 16 versioned seeds, 60000 refinements per seed and 120 seconds total per seed (scaffold 30 seconds). Budgets return an explicit rejection, never relax the target. Detailed attempt records are local to Library/Tidebound/CampaignGeneration.jsonl. A fully verified manifest is replaced atomically; a failed partial batch never replaces an earlier manifest. The layout/proof checkpoint pair is reverified after interruption.

Editor menus: Tools → Tidebound → Campaign → Validate / Use Validated 30 or 100 In Portrait Scene. Applying changes only the serialized catalog assets, preserves the scene's other objects and refuses a dirty loaded scene. This does not change Build Settings, release a mobile build or touch player saves.

`TideboundLevelStudio.Save Proof` now writes the current versioned format. A manually edited campaign level needs independent solver, recipe/structure, metadata, content revision and whole-pack re-certification; the old content digest deliberately refuses an edited layout. The legacy ten-candidate audit remains strict and independent.

Optional atlas (Pillow):

```sh
python3 Tools/Levels/render_atlas.py 100 /path/to/review-output
```

Atlas colors encode logical directions, white outlines indicate long ships. They are layout diagnostics, not screenshots or device evidence. Human pacing/readability and Android performance are not certified by these tools.
