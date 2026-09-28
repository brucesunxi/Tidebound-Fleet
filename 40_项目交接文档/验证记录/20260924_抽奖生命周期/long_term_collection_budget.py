#!/usr/bin/env python3
"""Design-only model: three no-duplicate pools, capped prices, expansion checks.

Reads the actual catalog identities and 100 layouts. Never edits player data or
production configuration. Rarity assignments for trails/showcase are proposals.
"""
import hashlib
import json
from pathlib import Path
import random
import math
import re
from collections import Counter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PROJECT = ROOT / '10_开发工作区/TideboundFleet_Unity'
COLLECTION = PROJECT / 'Assets/Tidebound/Runtime/Core/Collection'
CONFIG = PROJECT / 'Assets/Tidebound/Config'
appearance_path = COLLECTION / 'AppearanceCatalog.cs'
showcase_path = COLLECTION / 'ShowcaseCatalog.cs'
appearance = appearance_path.read_text()
showcase = showcase_path.read_text()
skins = re.findall(r'new AppearanceDefinition\("(TF_SKIN_K\d+)",AppearanceKind.Skin,"[^"]+","([^"]+)",ShowcaseSourceGroup.Draw,0,(\d)\)', appearance)
shapes = re.findall(r'new ShowcaseDefinition\("(TF_SHOWCASE_H\d+)","[^"]+","([^"]+)"[^\n]*ShowcaseSourceGroup.Draw\)', showcase)
trail_rarities = {'TF_TRAIL_W04': 1, 'TF_TRAIL_W10': 1, 'TF_TRAIL_W11': 2,
                  'TF_TRAIL_W08': 3, 'TF_TRAIL_W12': 4}
shape_rarities = {f'TF_SHOWCASE_H{x:02d}': r for r, ids in (
    (1, [7, 10, 11]), (2, [13, 19, 20, 24]), (3, [15, 17, 21]), (4, [22])) for x in ids}
assert len(skins) == 14 and len(shapes) == 11
assert set(shape_rarities) == {x[0] for x in shapes}
assert all(f'"{item}"' in appearance for item in trail_rarities)
POOLS = {
    'skin': dict(prices=[200, 300, 500, 800, 1200, 1800, 2600, 3800, 5400, 7500],
                 gold=6, red=10, items={x[0]: int(x[2]) for x in skins}, free=['TF_SKIN_K11']),
    'trail': dict(prices=[300, 600, 1200, 2200, 3600, 5000, 6500, 7500],
                  gold=3, red=4, items=trail_rarities, free=[]),
    'showcase': dict(prices=[500, 800, 1300, 2000, 2900, 4000, 5200, 6500, 7500],
                     gold=6, red=10, items=shape_rarities, free=[]),
}
WEIGHTS = [48, 30, 15, 6, 1]

def price(pool, paid):
    assert paid >= 0
    return POOLS[pool]['prices'][min(paid, len(POOLS[pool]['prices']) - 1)]

def draw(items, owned, next_paid, gold_at, red_at, rng):
    groups = {r: [key for key, tier in items.items() if tier == r and key not in owned]
              for r in range(5)}
    weights = [WEIGHTS[r] if groups[r] and (r != 3 or next_paid >= gold_at)
               and (r != 4 or next_paid >= red_at) else 0 for r in range(5)]
    assert sum(weights) > 0, 'Candidate creates an empty eligible pool before completion'
    roll = rng.randrange(sum(weights))
    tier = 0
    while roll >= weights[tier]:
        roll -= weights[tier]
        tier += 1
    item = rng.choice(groups[tier])
    assert item not in owned
    owned.add(item)
    return tier

rarity_results = {}
for name, pool in POOLS.items():
    gold, red = Counter(), Counter()
    for profile in range(3000):
        rng = random.Random(20260924 + profile)
        owned = set(pool['free'])
        first = {}
        count = len(pool['items']) - len(owned)
        for n in range(1, count + 1):
            tier = draw(pool['items'], owned, n, pool['gold'], pool['red'], rng)
            first.setdefault(tier, n)
        assert owned == set(pool['items'])
        gold[first[3]] += 1
        red[first[4]] += 1
    rarity_results[name] = dict(firstGold=dict(gold), firstRed=dict(red), profiles=3000)
    assert all(price(name, n) <= 7500 for n in range(1000))
    assert all(price(name, n + 1) >= price(name, n) for n in range(1000))

manifest_path = CONFIG / 'Levels/Campaign/manifest-100.json'
manifest = json.loads(manifest_path.read_text())
boat_counts = []
for row in manifest['levels']:
    folder = CONFIG / 'LevelPrototypes/Phase5R_TenLevelCandidates' if row['number'] <= 10 else CONFIG / 'Levels/Campaign'
    boat_counts.append(len(json.loads((folder / row['layoutFile']).read_text())['ships']))
assert len(boat_counts) == 100 and sum(boat_counts) == 7927

def budget(selected, tool_budget, reserve, normal_clear_income):
    paid = {name: 0 for name in selected}
    capacity = {name: len(POOLS[name]['items']) - len(POOLS[name]['free']) for name in selected}
    events = {name: [] for name in selected}
    wallet = 0
    completed = None
    for level, boats in enumerate(boat_counts, 1):
        wallet += 100 + (normal_clear_income - 100) * boats // 80  # Victory-only fixed 100; boat component scales with ship count.
        if level >= 3:
            wallet -= min(wallet, tool_budget)
        if level >= 2:
            while True:
                active = [name for name in selected if paid[name] < capacity[name]]
                if not active:
                    if completed is None:
                        completed = level
                    break
                name = min(active, key=lambda k: (price(k, paid[k]), k))
                cost = price(name, paid[name])
                if wallet < cost + reserve:
                    break
                wallet -= cost
                paid[name] += 1
                events[name].append(level)
    return dict(selected=selected, normalClearTotalIncome=normal_clear_income, toolBudgetPerClear=tool_budget, reserve=reserve,
                completedLevel=completed, draws=paid, paidDrawLevels=events, walletAt100=wallet)

# Hypothetical future content, not assets promised or already created.
old_pool = POOLS['skin']
owned = set(old_pool['items'])
expanded = dict(old_pool['items'])
expanded.update({f'HYPOTHETICAL_NEW_{i}': r for i, r in enumerate([1, 1, 2, 3, 4])})
old_paid = len(old_pool['items']) - len(old_pool['free'])
expansion_cost = 0
for offset in range(5):
    expansion_cost += price('skin', old_paid + offset)
    draw(expanded, owned, old_paid + offset + 1, old_pool['gold'], old_pool['red'], random.Random(offset))
assert len(owned) == len(expanded) and expansion_cost == 37500

costs = {name: sum(price(name, n) for n in range(len(pool['items']) - len(pool['free'])))
         for name, pool in POOLS.items()}
assert costs == {'skin': 46600, 'trail': 7900, 'showcase': 45700}
output = dict(
    status='Design candidate only; not activated', pools=POOLS, weights=WEIGHTS,
    currentCompletionCost=costs, totalThreePools=sum(costs.values()), raritySamples=rarity_results,
    budgetAssumptions='LATEST USER RULE: each white ship yields 1 coin; successful clear grants a fixed approximately 100 additional coins, independent of level. Deadlock exit/restart grants no victory bonus. 180 total is 80 white ships + 100; Red boats remain probabilistic 1-8 coins; 400 is a representative higher-quality mixed-fleet total INCLUDING the fixed 100, not a guaranteed amount or hard cap. 260/340 are sensitivity scenarios. The fixed 100 is not scaled on the 7-ship tutorial. All simulated attempts succeed; deadlock partial battle payouts are not modeled; no ads/payment/retries; fixed tool-spending proxies from level 3; all three proposed channels assumed open after level 2; cheapest next purchase among selected pools. This is a budget screen, not the new per-ship critical-coin implementation or measured behavior.',
    budgets=[budget(names, tools, reserve, income) for names in [['skin'], ['trail'], ['showcase'], list(POOLS)]
             for tools, reserve in [(0, 0), (75, 650)] for income in [180,260,340,400]],
    expansion=dict(hypotheticalNewSkins=5, priorPaidCount=old_paid, cost=expansion_cost, nextPrice=price('skin', old_paid + 5), resetPrice=False, resetRarityAccess=False),
    steadySavingClearEquivalents=[dict(totalIncomePerClear=income,toolBudget=tools,
        cappedDraw=math.ceil(7500/(income-tools)),skinPool=math.ceil(costs['skin']/(income-tools)),
        trailPool=math.ceil(costs['trail']/(income-tools)),showcasePool=math.ceil(costs['showcase']/(income-tools)),
        allThree=math.ceil(sum(costs.values())/(income-tools)))
        for income in [180,260,340,400] for tools in [0,75]],
    steadySavingAssumptions='Theoretical saving equivalents from zero money at constant income, no initial reserve or gifting, no other pool spending for individual-pool values. NOT predicted first acquisition or completion levels. 400 is a representative higher-quality mixed-fleet total including fixed 100, not a fixed per-boat payout or hard ceiling.',
    victoryRewardRule=dict(success=100,deadlockExit=0,restart=0,growsWithLevel=False,sameVictoryPaysOnce=True),
    withdrawn='Earlier linear first-clear, proposed 2080 cap, and intermediate no-separate-bonus/400-total interpretations are withdrawn. Existing runtime still uses legacy rules; this script does not change them.',
    equipmentExpectationsWithNewVictoryBonus=[dict(name=name,standardGroups=groups,longShips=4,
        expectedTotal=100+4+sum(q*(1+(cap-1)*max(.1,min(.8,.1+.875*(1-q/76)))) for q,cap in groups))
        for name,groups in [('white',[(76,1)]),('one_red_skin',[(76,8)]),
                           ('purple_gold_red',[(26,3),(25,5),(25,8)]),
                           ('three_red_skins',[(26,8),(25,8),(25,8)]),
                           ('three_red_two_gold',[(16,8),(15,8),(15,8),(15,5),(15,5)])]],
    equipmentExpectationMethod='Analytical expectation of current per-ship caps and Probability formula; 76 standard/4 long ships matches actual level 55, balanced hypothetical equipment. Assumes NEW fixed 100 victory reward, not current live first-clear formula. No sampled SHA rewards.',
    sourceSha256={str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in [appearance_path, showcase_path, manifest_path, Path(__file__).resolve()]})
(HERE / 'LONG_TERM_COLLECTION_BUDGET.json').write_text(json.dumps(output, ensure_ascii=False, indent=2) + '\n')
print(json.dumps(dict(costs=costs,total=sum(costs.values()),rarity=rarity_results,
                     budgets=[{k: v for k, v in row.items() if k != 'paidDrawLevels'} for row in output['budgets']],
                     expansion=output['expansion'],savingEquivalents=output['steadySavingClearEquivalents']),ensure_ascii=False,indent=2))
