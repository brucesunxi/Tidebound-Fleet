#!/usr/bin/env python3
"""Candidate arithmetic, not production logic or a player-behavior simulation."""
import json
import random
from collections import Counter
from pathlib import Path

here = Path(__file__).resolve().parent
config = here.parents[2] / '10_开发工作区/TideboundFleet_Unity/Assets/Tidebound/Config'
campaign = config / 'Levels/Campaign'
manifest = json.loads((campaign / 'manifest-100.json').read_text())
ships = []
for row in manifest['levels']:
    folder = config / 'LevelPrototypes/Phase5R_TenLevelCandidates' if row['number'] <= 10 else campaign
    ships.append(len(json.loads((folder / row['layoutFile']).read_text())['ships']))
assert len(ships) == 100 and sum(ships) == 7927
prices = [200, 300, 500, 800, 1200, 1800, 2600, 3800, 5400, 7500, 10000, 13000, 16500]
scenarios = []
for capped in (False, True):
    for tools, reserve in ((0, 0), (75, 650), (250, 650)):
        wallet = paid = 0
        events = []
        for level, boat_count in enumerate(ships, 1):
            wallet += 100 + 20 * (min(level - 1, 9) if capped else level - 1) + boat_count
            if level >= 3:
                wallet -= min(wallet, tools)
            if level >= 2:
                while paid < len(prices) and wallet >= prices[paid] + reserve:
                    wallet -= prices[paid]
                    paid += 1
                    events.append(level)
        scenarios.append(dict(firstClearCapped=capped,toolBudgetPerClear=tools,reserve=reserve,
                              paidDrawLevels=events,completedLevel=events[-1] if paid == 13 else None))
gold_first, red_first = Counter(), Counter()
for profile in range(3000):
    rng = random.Random(20260924 + profile)
    remaining = [2, 2, 3, 3, 3]  # One of the three blue skins was granted free.
    first_gold = first_red = None
    for draw_number in range(1, 14):
        weights = [48, 30, 15, 6 if draw_number >= 6 else 0, 1 if draw_number >= 10 else 0]
        weights = [w if remaining[i] else 0 for i, w in enumerate(weights)]
        assert sum(weights) > 0
        roll = rng.randrange(sum(weights))
        rarity = 0
        while roll >= weights[rarity]:
            roll -= weights[rarity]
            rarity += 1
        remaining[rarity] -= 1
        if rarity == 3 and first_gold is None:
            first_gold = draw_number
        if rarity == 4 and first_red is None:
            first_red = draw_number
    assert sum(remaining) == 0 and 6 <= first_gold <= 8 and 10 <= first_red <= 11
    gold_first[first_gold] += 1
    red_first[first_red] += 1
result = dict(status='Nonlinear price / staged rarity design candidate; not activated',
              priceFormula='Explicit 13-step table, by successful paid draws in this pool',
              rarityRules=dict(goldUnlockPaidDraw=6,redUnlockPaidDraw=10,
                              weights=[48,30,15,6,1],method='Exclude unopened and fully-owned rarities; normalize remaining tier weights; choose uniformly among unowned items in that tier.'),
              candidateSample=dict(profiles=3000,firstGoldDrawCounts=dict(gold_first),firstRedDrawCounts=dict(red_first)),
              prices=prices,total=sum(prices),freeBlueCountsTowardPrice=False,
              assumptions='Existing 100 layouts; minimum 1 coin per boat; all levels cleared; no ads/payments/retries; '
              'fixed tool-spending proxies from level 3; all available nonreserved coins go to this pool; '
              '13 paid new items after the free blue; no additional coins from early high-rarity equipment.',scenarios=scenarios)
(here / 'SINGLE_DRAW_BUDGET.json').write_text(json.dumps(result,ensure_ascii=False,indent=2) + '\n')
print(json.dumps(result,ensure_ascii=False,indent=2))
