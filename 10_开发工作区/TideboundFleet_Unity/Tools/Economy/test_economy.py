"""Model tests, including a production-generated differential oracle."""
import copy
import hashlib
import json
import math
import random
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import patch
import simulate_economy as model


class EconomyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.contract = model.load_contract()

    def test_all_520_production_transactions_match(self):
        self.assertEqual(model.verify_contract(self.contract), 520)

    def test_changed_source_contract_fails_closed(self):
        contract = copy.deepcopy(self.contract)
        key = next(iter(contract['sources']))
        contract['sources'][key] = '0'*64
        with TemporaryDirectory() as directory:
            path = Path(directory)/'contract.json'
            path.write_text(json.dumps(contract))
            with self.assertRaisesRegex(ValueError, 'Production changed'):
                model.load_contract(path)

    def test_oracle_is_not_just_probability_check(self):
        contract = copy.deepcopy(self.contract)
        contract['traces'][0]['actions'][1]['cost'] += 1
        with self.assertRaises(AssertionError):
            model.verify_contract(contract)

    def test_binomial_mean_and_variance_match_production_probability(self):
        for row in self.contract['probabilities']:
            for cap in (1, 2, 3, 5, 8):
                trials = row['matching']*(cap-1)
                cdf = model.binomial_cdf(trials, row['matching'], row['standard'])
                pmf = [cdf[0]]+[cdf[i]-cdf[i-1] for i in range(1, len(cdf))]
                mean = sum(i*p for i, p in enumerate(pmf))
                var = sum((i-mean)**2*p for i, p in enumerate(pmf))
                self.assertAlmostEqual(mean, trials*row['p'], places=7)
                self.assertAlmostEqual(var, trials*row['p']*(1-row['p']), places=6)
                self.assertTrue(all(p >= -1e-12 for p in pmf))

    def test_actual_long_ship_counts_and_future_proxy_are_explicit(self):
        self.assertEqual([model.ship_counts(self.contract, n)[1] for n in range(1, 11)], [0,0,0,4,4,4,6,6,6,8])
        self.assertEqual(model.ship_counts(self.contract, 11), model.ship_counts(self.contract, 2))
        self.assertEqual(model.ship_counts(self.contract, 20), model.ship_counts(self.contract, 2))
        self.assertEqual(model.ship_counts(self.contract, 1, 110), (7,0))
        self.assertEqual(model.ship_counts(self.contract, 11, 110), (110,0))

    def test_long_ships_fixed_and_single_red_mean(self):
        c = model.Collection(self.contract, '1'*32)
        c.owned = [15]
        rng = random.Random(4)
        values = [model.battle(rng, c, [15], 80, 0, 0) for _ in range(10000)]
        self.assertAlmostEqual(sum(values)/len(values), 136, delta=.5)
        self.assertTrue(all(80 <= x <= 640 for x in values))
        self.assertEqual(model.battle(rng, c, [15], 80, 80, 0), 80)

    def test_ten_batch_locks_price_and_guarantees_purple(self):
        c = model.Collection(self.contract, 'a'*32)
        c.transaction('FirstBlue')
        r = c.transaction('Ten')
        self.assertEqual(r['cost'], 2700)
        self.assertEqual(len(r['results']), 10)
        by_id = {s['id']: s for s in c.pool}
        self.assertTrue(any(by_id[s]['rarity'] >= 2 for s in r['results']))
        self.assertGreater(len(c.owned)-1, 3)
        self.assertGreater(model.price(self.contract, 'c3_candidate', len(c.owned)-1), 300)

    def test_free_gift_does_not_advance_pity_or_auto_equip(self):
        c = model.Collection(self.contract, '2'*32)
        c.transaction('FirstBlue')
        self.assertEqual((c.draws, c.gold, c.red), (0,0,0))
        self.assertEqual(c.equipment('default'), [0])
        with self.assertRaises(ValueError):
            c.transaction('FirstBlue')

    def test_first_paid_draw_is_possible_after_level_two_settlement(self):
        c = dict(curve='live', policy='collector_single', need=0)
        r = model.simulate(self.contract, 1, c, 3)[3]
        self.assertEqual(r['first_draw'], 2)
        self.assertEqual(r['gift_items'], 3)
        self.assertEqual(r['tool_spend'], 0)
        self.assertEqual(r['first_clear'], 360)

    def test_currency_inventory_and_caps_across_every_scenario(self):
        for case in model.cases('full'):
            for seed in range(3):
                result = model.simulate(self.contract, seed, case)
                for level, row in result.items():
                    self.assertEqual(row['coins'], row['income']-row['tool_spend']-row['draw_spend'])
                    self.assertLessEqual(row['tool_used'], 5*(level+row['restarts']))
                    self.assertEqual(row['unfunded']+row['tool_used'], row['tool_requests'])
                    days = math.ceil(level/10)
                    self.assertLessEqual(row['ad_items'], 6*days)
                    self.assertLessEqual(row['ad_draws'], days)
                    self.assertLessEqual(row['ads'], 10*days)
                    self.assertLessEqual(row['packs'], days)
                    self.assertEqual(row['pool'], 25 if case.get('expand') and level>=201 else 20 if case.get('expand') and level>=101 else 15)

    def test_restarts_share_daily_ordinal_even_when_voluntary(self):
        kinds = [False, True]*6
        rewards = [model.restart_reward(80, kind, n+1) for n, kind in enumerate(kinds)]
        self.assertEqual(sum(rewards), 400)
        self.assertEqual(rewards[10:], [0,0])
        self.assertEqual(model.restart_reward(80, True, 1), 80)
        with self.assertRaises(ValueError):
            model.restart_reward(80, True, 0)

    def test_expansion_and_chapter_grants_are_opt_in(self):
        a = model.simulate(self.contract, 1, dict(curve='c3_candidate', policy='balanced_single', need=.3))[300]
        b = model.simulate(self.contract, 1, dict(curve='c3_candidate', policy='balanced_single', need=.3, expand=True, chapter=10))[300]
        self.assertEqual(a['pool'], 15)
        self.assertEqual(a['tickets'], a['duplicate_tickets']-a['ticket_spend'])
        self.assertEqual(b['pool'], 25)
        self.assertEqual(b['tickets'], b['duplicate_tickets']+300-b['ticket_spend'])

    def test_censored_events_report_reached_fraction(self):
        samples = [model.simulate(self.contract, seed, dict(curve='c3_candidate', policy='balanced_ten', need=1.5), 30) for seed in range(10)]
        stats = model.summarize(samples)[30]['first_ten']
        reached = [s[30]['first_ten'] for s in samples if s[30]['first_ten']]
        self.assertEqual(stats['reached'], len(reached)/10)
        if reached:
            self.assertEqual(stats['reached_quantiles'], model.quantiles(reached))

    def test_reproducible_and_demand_does_not_change_with_prices(self):
        case = dict(curve='live', policy='balanced_single', need=1.5)
        a = model.simulate(self.contract, 33, case)
        self.assertEqual(a, model.simulate(self.contract, 33, case))
        b = model.simulate(self.contract, 33, dict(case, curve='c3_candidate'))
        self.assertEqual(a[300]['tool_requests'], b[300]['tool_requests'])


if __name__ == '__main__':
    unittest.main(verbosity=2)
