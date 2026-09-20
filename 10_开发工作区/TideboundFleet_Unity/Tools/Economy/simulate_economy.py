"""Reproducible scenario model, not a retention/revenue forecast or production RNG.
Run: python3 Tools/Economy/simulate_economy.py --players 1000 --out /tmp/economy.json
Models single pulls, dated content expansion, cosmetic coin yields, aid demand and budgets.
Does not simulate puzzle solving, failed attempts, real ad fill, conversion, or cash prices.
"""
import argparse, bisect, functools, json, math, random, statistics
CAPS = (1, 2, 3, 5, 8)
PRICES = (250, 300, 200)  # Rescue, shuffle, reverse
TICKETS = (10, 20, 50, 120, 300)
EXCHANGE = (60, 150, 400, 1000, 2500)
BASE_POOL = [0]*4+[1]*4+[2]*3+[3]*2+[4]*2
COHORTS = {'light_free': (.3, False, False), 'high_need_free': (1.5, False, False),
           'high_need_ads': (1.5, True, False), 'high_need_paid': (1.5, False, True)}

@functools.lru_cache(None)
def binomial_cdf(n, q):
    p = max(.1, min(.8, .1+.875*(1-q)))
    cumulative, result = 0., []
    for k in range(n+1):
        cumulative += math.comb(n,k)*p**k*(1-p)**(n-k)
        result.append(cumulative)
    result[-1] = 1.
    return result

def battle(rng, owned, pool, level):
    size=7 if level==1 else 80
    longs=6 if level>=4 else 0
    equipped=sorted([CAPS[pool[i]] for i in owned]+[1],reverse=True)[:5]
    n=size-longs; total=size
    for i, cap in enumerate(equipped):
        count=n//len(equipped)+(i<n%len(equipped))
        if cap>1: total+=bisect.bisect_left(binomial_cdf(count*(cap-1),count/n),rng.random())
    return total

def draw_cost(u, tuned=False):
    if tuned:return 300 if u<3 else 650 if u<6 else 1100 if u<10 else 1700 if u<15 else 2400 if u<20 else 3000
    return 300 if u<4 else 450 if u<8 else 700 if u<12 else 1000 if u<16 else 1400 if u<21 else 1800 if u<31 else 2200

def demand(rng, mean):
    count, product, stop=0,1.,math.exp(-mean)
    while product>stop: count+=1;product*=rng.random()
    return min(5,count-1)

def simulate(seed, curve, cohort):
    rng=random.Random(seed);need,ads,paid=COHORTS[cohort]
    pool=list(BASE_POOL);owned=set();stock=[0,0,0]
    wallet=income=tool_spend=draw_spend=unfunded=used=tool_bought=ad_count=paid_items=tickets=pulls=0
    gold_dry=red_dry=duplicate_dry=0;out={}
    def pull():
        nonlocal gold_dry,red_dry,duplicate_dry,tickets,pulls
        roll=rng.random();rarity=0 if roll<.48 else 1 if roll<.78 else 2 if roll<.93 else 3 if roll<.99 else 4
        if red_dry>=59:rarity=4
        elif gold_dry>=19:rarity=4 if rng.random()<1/7 else 3
        candidates=[i for i,r in enumerate(pool) if r==rarity]
        if duplicate_dry>=4 and rarity<=2:
            fresh=[i for i,r in enumerate(pool) if r<=2 and i not in owned]
            if fresh:candidates=fresh
        item=rng.choice(candidates);rarity=pool[item];pulls+=1
        gold_dry=0 if rarity>=3 else gold_dry+1;red_dry=0 if rarity==4 else red_dry+1
        if item in owned:tickets+=TICKETS[rarity];duplicate_dry+=1
        else:owned.add(item);duplicate_dry=0
        # Model one explicit player strategy: exchange for the highest affordable missing rarity.
        for r in range(4,-1,-1):
            missing=[i for i,rr in enumerate(pool) if rr==r and i not in owned]
            while missing and tickets>=EXCHANGE[r]:tickets-=EXCHANGE[r];owned.add(missing.pop())
    for level in range(1,301):
        if (level-1)%10==0:ad_tools=ad_draw=ad_double=paid_packs=0
        if curve.startswith('capped_expanding') and level in (101,201):pool.extend(range(5))
        if level==3:stock=[v+1 for v in stock]
        if level%10==0:stock[rng.randrange(3)]+=1
        if level>=3:
            for _ in range(demand(rng,need)):
                roll=rng.random();kind=0 if roll<.4 else 1 if roll<.65 else 2
                if not stock[kind] and ads and ad_tools<6:
                    stock[kind]+=1;ad_tools+=1;ad_count+=1
                if not stock[kind] and paid and paid_packs<1:
                    stock=[v+2 for v in stock];paid_items+=6;paid_packs+=1
                price=PRICES[kind]
                if not stock[kind] and wallet>=price and tool_spend+price<=income*.35:
                    stock[kind]+=1;wallet-=price;tool_spend+=price;tool_bought+=1
                if stock[kind]:stock[kind]-=1;used+=1
                else:unfunded+=1
        coins=battle(rng,owned,pool,level)
        first=100+20*((level-1) if curve=='linear_fixed' else min(level-1,9))
        reward=coins+first
        if ads and level>=3 and ad_double<3:reward+=coins;ad_double+=1;ad_count+=1
        wallet+=reward;income+=reward
        if level==2:owned.add(rng.choice([i for i,r in enumerate(pool) if r==1]))
        if ads and level>=3 and ad_draw<1:pull();ad_draw+=1;ad_count+=1
        while level>=3 and len(owned)<len(pool):
            price=draw_cost(len(owned),curve.endswith("draw_v2"))
            if wallet<price or draw_spend+price>income*.65:break
            wallet-=price;draw_spend+=price;pull()
        assert wallet==income-tool_spend-draw_spend and wallet>=0
        if level in (30,100,300):
            out[level]={'coins':wallet,'income':income,'tool_spend':tool_spend,'draw_spend':draw_spend,'skins':len(owned),'pool':len(pool),
                'tool_bought':tool_bought,'tool_used':used,'unfunded_assists':unfunded,'ad_completions':ad_count,'paid_items':paid_items,'pulls':pulls,
                'collection_complete':int(len(owned)==len(pool))}
    return out

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--players',type=int,default=1000);parser.add_argument('--out',required=True);args=parser.parse_args()
    result={'model_version':1,'seed':20260920,'players_per_case':args.players,'levels':300,'rounds':args.players*300*len(COHORTS)*4,
        'assumptions':{'levels_per_day':10,'max_uses_per_attempt':5,'coin_budget_share_tools':.35,'coin_budget_share_draws':.65,
        'ad_caps':{'tools':6,'draws':1,'battle_double':3},'paid_pack_model':'at most one 2-of-each pack per 10 levels; no cash price or conversion forecast',
        'draws':'single pulls only; free blue after level 2; rarity/gold/red/duplicate protection and tickets modeled; ten-pull discount not modeled',
        'outcomes':'all 300 levels assumed completed; unfunded assists do not imply failed levels',
        'curves':'linear_fixed=old uncapped income/15 skins; capped_fixed=first-clear cap280/15 skins; capped_expanding=cap280 plus5 skins at101/201; draw_v2=slower disclosed price tiers 300/650/1100/1700/2400/3000'},'cases':{}}
    for curve in ('linear_fixed','capped_fixed','capped_expanding','capped_expanding_draw_v2'):
        for cohort in COHORTS:
            samples=[simulate(20260920+i,curve,cohort) for i in range(args.players)]
            case={}
            for level in (30,100,300):
                stats={}
                for key in samples[0][level]:
                    values=sorted(x[level][key] for x in samples)
                    stats[key]={'p10':values[int(.1*(len(values)-1))],'p50':statistics.median(values),'p90':values[int(.9*(len(values)-1))]}
                stats['completion_fraction']=sum(x[level]['collection_complete'] for x in samples)/len(samples)
                case[level]=stats
            result['cases'][curve+'/'+cohort]=case
    with open(args.out,'w') as f:json.dump(result,f,indent=2)
    print('Simulated',result['rounds'],'level rounds. Saved',args.out)
if __name__=='__main__':main()
