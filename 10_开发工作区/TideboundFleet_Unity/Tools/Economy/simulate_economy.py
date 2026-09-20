"""C3 offline economy model. No player files, SDKs, or production price changes.
Production draws are checked byte-for-byte against an Editor-exported oracle.
Battle income uses the equivalent grouped binomial distribution (not per-ship SHA seeds).
Run --help; assumptions and limits are persisted with every output.
"""
import argparse
import bisect
import functools
import hashlib
import json
import math
from pathlib import Path
import random
import statistics

ROOT = Path(__file__).resolve().parents[2]
CONTRACT_PATH = Path(__file__).with_name('production_contract.json')
CHECKPOINTS = (3, 10, 30, 100, 300)
WEIGHTS = (48, 30, 15, 6, 1)


class ProductionRandom:
    def __init__(self, seed):
        self.state = int.from_bytes(hashlib.sha256(seed.encode()).digest()[:4], 'little') or 0x9e3779b9

    def next(self, maximum):
        if maximum <= 0:
            raise ValueError('maximum must be positive')
        x = self.state
        x ^= (x << 13) & 0xffffffff
        x ^= x >> 17
        x ^= (x << 5) & 0xffffffff
        self.state = x
        return x * maximum >> 32


def load_contract(path=CONTRACT_PATH):
    contract = json.loads(Path(path).read_text())
    if contract['contractVersion'] != 1:
        raise ValueError('Unknown production contract')
    for name, digest in contract['sources'].items():
        if hashlib.sha256((ROOT / name).read_bytes()).hexdigest() != digest:
            raise ValueError('Production changed; export contract again: ' + name)
    # Probabilities are also checked against 520 production transactions below.
    return contract


class Collection:
    def __init__(self, contract, seed):
        self.contract = contract
        self.seed = seed
        self.pool = list(contract['catalog'])
        self.owned = [0]
        self.tickets = self.draws = self.gold = self.red = self.duplicates = 0
        self.duplicate_tickets = self.exchange_spend = self.gift_tickets = 0
        self.first_blue = False
        self.exchange_count = 0

    def add_expansion(self, batch):
        for rarity in range(5):
            exemplar = next(s for s in self.pool if s['rarity'] == rarity)
            self.pool.append(dict(exemplar, id=f'CANDIDATE_BATCH_{batch}_{rarity}'))

    def transaction(self, kind, target=None):
        rng = ProductionRandom(f'{self.seed}:{self.contract["collectionVersion"]}:{self.draws}:{kind}')
        cost = ticket_cost = 0
        results = []
        if kind == 'FirstBlue':
            if self.first_blue:
                raise ValueError('Already claimed')
            pool = [i for i, s in enumerate(self.pool) if s['rarity'] == 1 and i not in self.owned]
            results = [pool[rng.next(len(pool))]]
            self.owned.extend(results)
            self.first_blue = True
        elif kind == 'Exchange':
            index = next(i for i, s in enumerate(self.pool) if s['id'] == target)
            ticket_cost = self.pool[index]['exchange']
            if index in self.owned or self.tickets < ticket_cost:
                raise ValueError('Unavailable exchange')
            self.owned.append(index)
            self.tickets -= ticket_cost
            self.exchange_spend += ticket_cost
            self.exchange_count += 1
            results = [index]
        elif kind in ('Single', 'Ten'):
            if not self.first_blue:
                raise ValueError('Claim the gift first')
            count = 10 if kind == 'Ten' else 1
            cost = self.contract['prices'][min(15, len(self.owned)-1)] * (9 if count == 10 else 1)
            high = False
            for step in range(count):
                minimum = 4 if self.red >= 59 else 3 if self.gold >= 19 else 2 if count == 10 and step == 9 and not high else 0
                roll = rng.next(sum(WEIGHTS[minimum:]))
                rarity = minimum
                while roll >= WEIGHTS[rarity]:
                    roll -= WEIGHTS[rarity]
                    rarity += 1
                pool = [i for i, s in enumerate(self.pool) if i != 0 and s['rarity'] == rarity]
                if minimum == 0 and self.duplicates >= 4 and rarity <= 2:
                    fresh = [i for i, s in enumerate(self.pool) if i and s['rarity'] <= 2 and i not in self.owned]
                    if fresh:
                        pool = fresh
                index = pool[rng.next(len(pool))]
                skin = self.pool[index]
                rarity = skin['rarity']
                self.draws += 1
                self.gold = 0 if rarity >= 3 else self.gold + 1
                self.red = 0 if rarity == 4 else self.red + 1
                if index in self.owned:
                    self.tickets += skin['duplicate']
                    self.duplicate_tickets += skin['duplicate']
                    self.duplicates = min(4, self.duplicates + 1)
                else:
                    self.owned.append(index)
                    self.duplicates = 0
                high |= rarity >= 2
                results.append(index)
                assert self.gold < 20 and self.red < 60
        else:
            raise ValueError('Unknown kind')
        return dict(kind=kind, target=target, results=[self.pool[i]['id'] for i in results], cost=cost,
                    ticketCost=ticket_cost, owned=[self.pool[i]['id'] for i in self.owned],
                    tickets=self.tickets, draws=self.draws, gold=self.gold, red=self.red, duplicates=self.duplicates)

    def exchange(self, strategy):
        while True:
            missing = [i for i in range(1, len(self.pool)) if i not in self.owned]
            if strategy == 'save_red':
                red = [i for i in missing if self.pool[i]['rarity'] == 4]
                if red:
                    missing = red
            affordable = [i for i in missing if self.pool[i]['exchange'] <= self.tickets]
            if not affordable:
                break
            target = max(affordable, key=lambda i: (self.pool[i]['rarity'], -i))
            self.transaction('Exchange', self.pool[target]['id'])

    def equipment(self, strategy):
        if strategy == 'default':
            return [0]
        if strategy == 'favorite':
            # Explicit modeled choice: use one highest-rarity favorite; no automatic production equip.
            return [max(self.owned, key=lambda i: (self.pool[i]['rarity'], -i))]
        return sorted(self.owned, key=lambda i: (-self.pool[i]['rarity'], i))[:5]


def verify_contract(contract):
    checked = 0
    for trace in contract['traces']:
        state = Collection(contract, trace['seed'])
        for expected in trace['actions']:
            actual = state.transaction(expected['kind'], expected.get('target'))
            if actual != expected:
                raise AssertionError(f'Production trace mismatch: {trace["seed"]}, action {checked}')
            checked += 1
    return checked


@functools.lru_cache(maxsize=8192)
def binomial_cdf(trials, matching, standard):
    p = max(.1, min(.8, .1 + .875 * (1 - matching / standard)))
    # Recurrence avoids repeatedly constructing very large binomial coefficients.
    values = []
    probability = (1-p) ** trials
    cumulative = probability
    values.append(cumulative)
    for k in range(trials):
        probability *= (trials-k) / (k+1) * p / (1-p)
        cumulative += probability
        values.append(cumulative)
    values[-1] = 1.0
    return values


def restart_reward(pending, deadlock, ordinal):
    if ordinal < 1:
        raise ValueError("Restart ordinal starts at one")
    return pending if deadlock and ordinal <= 10 else 0


def ship_counts(contract, level, density=80):
    if level <= 10:
        item = contract['levels'][level-1]
        return item['ships'], item['longs']
    # Economic workload proxy only: repeat the actual level 2-10 counts, without inventing layouts.
    item = contract['levels'][1+(level-11) % 9]
    return density, round(item['longs'] * density / item['ships'])


def battle(rng, collection, equipment, total, longs, ordinal):
    standard = total-longs
    counts = [standard // len(equipment)] * len(equipment)
    for i in range(standard % len(equipment)):
        counts[(i+ordinal) % len(equipment)] += 1
    coins = total
    for index, count in zip(equipment, counts):
        trials = count * (collection.pool[index]['cap']-1)
        if trials:
            coins += bisect.bisect_left(binomial_cdf(trials, count, standard), rng.random())
    return coins


def poisson(rng, mean):
    product, count, stop = 1., 0, math.exp(-mean)
    while product > stop:
        product *= rng.random()
        count += 1
    return max(0, count-1)


CURVES = {
    'live': dict(cap=False, prices=None),
    'cap_only': dict(cap=True, prices=None),
    'prior_candidate': dict(cap=True, prices=((3, 300), (6, 650), (10, 1100), (15, 1700), (20, 2400), (99, 3000))),
    'c3_candidate': dict(cap=True, prices=((3, 300), (6, 900), (10, 1600), (15, 2400), (20, 3100), (99, 3800))),
}
POLICIES = {
    'collector_single': dict(reserve=0, draw='Single', bundle=False),
    'balanced_single': dict(reserve=650, draw='Single', bundle=True),
    'balanced_ten': dict(reserve=650, draw='Ten', bundle=True),
    'tool_first': dict(reserve=1500, draw='Single', bundle=True),
}


def price(contract, curve, unique):
    tiers = CURVES[curve]['prices']
    return next(value for limit, value in tiers if unique < limit) if tiers else contract['prices'][min(unique, 15)]


def case_id(case):
    return '/'.join(str(case[k]) for k in ('curve', 'policy', 'need')) + ''.join(
        f'/{k}={case[k]}' for k in ('equip', 'exchange', 'support', 'retry', 'expand', 'density', 'chapter') if k in case)


def cases(suite):
    main = [dict(curve=curve, policy=policy, need=need) for curve in CURVES
            for policy in POLICIES for need in (.3, 1.5)]
    if suite == 'pilot':
        return main
    # Change one assumption at a time relative to the C3 balanced-single cohort.
    extras = []
    for need in (.3, 1.5):
        base = dict(curve='c3_candidate', policy='balanced_single', need=need)
        for key, values in {'equip': ('default', 'favorite'), 'exchange': ('save_red',),
                            'support': ('ads', 'pack2', 'pack5'), 'retry': (.25,),
                            'expand': (True,), 'density': (100, 110), 'chapter': (10,)}.items():
            extras.extend(dict(base, **{key: value}) for value in values)
    # Ten behavior when expansion or weekly/section grants are present.
    extras.append(dict(curve='c3_candidate', policy='balanced_ten', need=.3, expand=True))
    return main + extras


def simulate(contract, seed, case, max_level=300):
    profile_seed = hashlib.sha256(f'C3:{seed}'.encode()).hexdigest()[:32]
    collection = Collection(contract, profile_seed)
    rngs = {key: random.Random(f'{seed}:{key}') for key in ('need', 'kind', 'gift', 'battle', 'support', 'retry')}
    policy = POLICIES[case['policy']]
    shop = contract['shop']['products']
    unit_prices = [next(p['price'] for p in shop if p['id'] == name) for name in ('rescue_1', 'shuffle_1', 'reverse_1')]
    bundle = next(p for p in shop if p['id'] == 'tools_bundle_1')['price']
    stock = [0, 0, 0]
    metrics = {key: 0 for key in ('coins', 'income', 'first_clear', 'battle_income', 'restart_income', 'tool_spend', 'draw_spend',
        'tool_used', 'tool_requests', 'unfunded', 'over_limit', 'bought_items', 'bundles', 'gift_items', 'ad_items', 'paid_items', 'packs',
        'ads', 'ad_coins', 'singles', 'tens', 'ad_draws', 'restarts', 'unrewarded_restarts')}
    checkpoints = {}
    first_draw = first_ten = first_complete = last_draw = max_gap = 0
    attempts = 0
    clock = 0
    day = -1
    restart_count = ad_tools = ad_draws = ad_double = packs = 0
    last_ad = -10000
    support = case.get('support', 'none')
    reserve = policy['reserve']

    def income(amount, source):
        metrics['income'] += amount
        metrics['coins'] += amount
        metrics[source] += amount

    for level in range(1, max_level+1):
        if (level-1)//10 != day:
            day = (level-1)//10
            restart_count = ad_tools = ad_draws = ad_double = packs = 0
        if case.get('expand') and level in (101, 201):
            collection.add_expansion(level)
            last_draw = level-1  # Waiting for unreleased content is not a wallet-induced draw gap.
        if level == contract['giftUnlock']:
            stock = [x+1 for x in stock]
            metrics['gift_items'] += 3
        if level % contract['giftInterval'] == 0:
            stock[rngs['gift'].randrange(3)] += 1
            metrics['gift_items'] += 1
        equipment = collection.equipment(case.get('equip', 'five'))
        total, longs = ship_counts(contract, level, case.get('density', 80))
        failed = int(level >= 3 and rngs['retry'].random() < case.get('retry', 0))
        for attempt in range(failed+1):
            use_count = 0
            if level >= 3:
                requests = poisson(rngs['need'], case['need'])
                metrics['over_limit'] += max(0, requests-contract['maxUsesPerAttempt'])
                for _ in range(min(contract['maxUsesPerAttempt'], requests)):
                    clock += 30
                    roll = rngs['kind'].random()
                    kind = 0 if roll < .4 else 1 if roll < .65 else 2
                    metrics['tool_requests'] += 1
                    if not stock[kind] and support == 'ads' and ad_tools < 6 and clock-last_ad >= 90:
                        # Assumption: 80% eligible requests finish with a reward. Not an observed fill/completion rate.
                        if rngs['support'].random() < .8:
                            stock[kind] += 1
                            ad_tools += 1
                            metrics['ads'] += 1
                            metrics['ad_items'] += 1
                            last_ad = clock
                    if not stock[kind] and support in ('pack2', 'pack5') and packs == 0:
                        quantity = 2 if support == 'pack2' else 5
                        stock = [x+quantity for x in stock]
                        metrics['paid_items'] += quantity*3
                        metrics['packs'] += 1
                        packs += 1
                    if not stock[kind]:
                        # Player strategy: buy a bundle when two or more types are empty; otherwise buy the requested type.
                        buy_bundle = policy['bundle'] and stock.count(0) >= 2 and metrics['coins'] >= bundle
                        cost = bundle if buy_bundle else unit_prices[kind]
                        if metrics['coins'] >= cost:
                            metrics['coins'] -= cost
                            metrics['tool_spend'] += cost
                            metrics['bought_items'] += 3 if buy_bundle else 1
                            metrics['bundles'] += int(buy_bundle)
                            if buy_bundle:
                                stock = [x+1 for x in stock]
                            else:
                                stock[kind] += 1
                    if stock[kind]:
                        stock[kind] -= 1
                        use_count += 1
                        metrics['tool_used'] += 1
                    else:
                        metrics['unfunded'] += 1
            assert use_count <= contract['maxUsesPerAttempt']
            coins = battle(rngs['battle'], collection, equipment, total, longs, attempts)
            attempts += 1
            clock += 180
            if attempt < failed:
                restart_count += 1
                metrics['restarts'] += 1
                # Explicit scenario: half of failed attempts are strict terminal deadlocks at 50% realized reward.
                deadlock = rngs['retry'].random() < .5
                if deadlock and restart_count <= 10:
                    income(restart_reward(coins//2, deadlock, restart_count), 'restart_income')
                else:
                    metrics['unrewarded_restarts'] += 1
            else:
                income(coins, 'battle_income')
                first = 100+20*min(level-1, 9) if CURVES[case['curve']]['cap'] else contract['firstClear'][level-1]
                income(first, 'first_clear')
                if support == 'ads' and level >= 3 and ad_double < 3 and rngs['support'].random() < .8:
                    income(coins, 'ad_coins')
                    ad_double += 1
                    metrics['ads'] += 1
        if level == 2:
            collection.transaction('FirstBlue')
        if case.get('chapter') and level % 10 == 0:
            collection.tickets += case['chapter']
            collection.gift_tickets += case['chapter']
        if support == 'ads' and level >= 3 and ad_draws < 1 and rngs['support'].random() < .8:
            collection.transaction('Single')  # Future confirmed-ad reward would share normal pity; currently model-only.
            metrics['ad_draws'] += 1
            metrics['ads'] += 1
            ad_draws += 1
        # Unlocks are based on the current level AFTER settlement, matching PlayerSaveService.
        if level+1 >= 5:
            collection.exchange(case.get('exchange', 'greedy'))
        kind = policy['draw']
        while level >= 2 and (kind != 'Ten' or level+1 >= 4) and len(collection.owned) < len(collection.pool):
            cost = price(contract, case['curve'], len(collection.owned)-1) * (9 if kind == 'Ten' else 1)
            if metrics['coins'] < cost+reserve:
                break
            collection.transaction(kind)
            metrics['coins'] -= cost
            metrics['draw_spend'] += cost
            metrics['tens' if kind == 'Ten' else 'singles'] += 1
            first_draw = first_draw or level
            if kind == 'Ten':
                first_ten = first_ten or level
            max_gap = max(max_gap, level-(last_draw or 2))
            last_draw = level
            if level+1 >= 5:
                collection.exchange(case.get('exchange', 'greedy'))
        if len(collection.owned) == len(collection.pool):
            first_complete = first_complete or level
        assert metrics['coins'] == metrics['income']-metrics['tool_spend']-metrics['draw_spend'] >= 0
        assert collection.tickets == collection.duplicate_tickets+collection.gift_tickets-collection.exchange_spend >= 0
        assert sum(stock) == metrics['gift_items']+metrics['bought_items']+metrics['ad_items']+metrics['paid_items']-metrics['tool_used']
        if level in CHECKPOINTS:
            checkpoints[level] = dict(metrics, skins=len(collection.owned)-1, pool=len(collection.pool)-1,
                tickets=collection.tickets, exchanges=collection.exchange_count, stock=sum(stock),
                duplicate_tickets=collection.duplicate_tickets, ticket_spend=collection.exchange_spend,
                first_draw=first_draw, first_ten=first_ten, first_complete=first_complete,
                # Censored current wait included only while there are missing skins.
                max_draw_gap=max(max_gap, level-(last_draw or 2)) if len(collection.owned)<len(collection.pool) else max_gap,
                pulls=collection.draws, complete=int(len(collection.owned)==len(collection.pool)),
                coverage=metrics['tool_used']/metrics['tool_requests'] if metrics['tool_requests'] else 1.)
    return checkpoints


def quantiles(values):
    values = sorted(values)
    return dict(p10=values[int(.1*(len(values)-1))], p50=statistics.median(values), p90=values[int(.9*(len(values)-1))])


def summarize(samples):
    output = {}
    for level in samples[0]:
        rows = [s[level] for s in samples]
        stats = {key: quantiles([r[key] for r in rows]) for key in rows[0]}
        stats['completion_fraction'] = sum(r['complete'] for r in rows)/len(rows)
        for name in ('first_draw', 'first_ten', 'first_complete'):
            reached = [r[name] for r in rows if r[name]]
            stats[name] = dict(reached=len(reached)/len(rows), reached_quantiles=quantiles(reached) if reached else None)
        output[level] = stats
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--players', type=int, default=1000)
    parser.add_argument('--seed', type=int, default=20260920)
    parser.add_argument('--suite', choices=('pilot', 'full'), default='full')
    parser.add_argument('--out', required=True)
    args = parser.parse_args()
    if args.players < 1:
        parser.error('--players must be positive')
    contract = load_contract()
    verified = verify_contract(contract)
    matrix = cases(args.suite)
    result = dict(model_version=3, seed=args.seed, players_per_case=args.players, levels=300,
        level_rounds=args.players*300*len(matrix), contract_sha256=hashlib.sha256(CONTRACT_PATH.read_bytes()).hexdigest(),
        production_transactions_verified=verified, curves=CURVES, policies=POLICIES,
        assumptions=dict(progress='All players eventually complete 300 levels; no retention, revenue or solve-rate inference.',
            level_workload='Actual counts in levels 1-10; repeat level 2-10 counts after 10 as workload proxy; no new layouts.',
            behavior='Player-chosen equip/update strategy; real game never auto-equips. Reserve is a preference, not a budget enforced in game.',
            battle='Exact grouped binomial distribution; independent PRNG samples, not production per-ship SHA identity.',
            exchange='After level 4 settlement (current level 5); one strategy selects highest affordable missing rarity; alternate saves for red.',
            tools='Poisson requested aid, .3 or 1.5 mean, types 40/25/35%; cap5 per attempt; assume legal use. Unfunded aid is not failed levels.',
            packs='Model-only: at most one pack per ten completed levels, two or five of each. No currency prices or conversion.',
            ads='Model-only: 80% completed reward assumption; caps 6 tools/1 draw/3 double per day; 90s tool cooldown; 10 completed levels/day.',
            retry='Sensitivity only: 25% levels with one extra attempt, half strict deadlock, reward proxy 50%; all restarts consume daily ordinal.',
            expansion='Sensitivity only: one hypothetical skin per rarity at levels101/201; no production assets added.',
            chapter='Sensitivity only: 10 tickets per 10 completed levels; not a published grant.',
            percentiles='Nearest lower p10/p90 and median. First-event statistics condition on reached and also report censoring fraction.'), cases={})
    for case in matrix:
        name = case_id(case)
        samples = [simulate(contract, args.seed+i, case) for i in range(args.players)]
        result['cases'][name] = dict(parameters=case, checkpoints=summarize(samples))
        print('Finished', name, flush=True)
    output = Path(args.out)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2)+'\n')
    print(f'Saved {len(matrix)} cases / {result["level_rounds"]:,} modeled level rounds.', flush=True)


if __name__ == '__main__':
    main()
