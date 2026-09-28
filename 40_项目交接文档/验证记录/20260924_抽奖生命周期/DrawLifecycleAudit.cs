using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Tidebound.Collection;
using Tidebound.Save;

// Audit only. Fresh synthetic profiles; actual draw/protection/price/ticket methods.
// Sensitivity income and spending policies are deliberately NOT production changes.
public static class DrawLifecycleAudit
{
    const int N=3000;
    static readonly string Request=new string('0',32);
    static readonly HashSet<string> PoolIds=new HashSet<string>(AppearanceDrawEngine.Pool.Select(x=>x.Id));
    static string Seed(int i)=>i.ToString("x32");
    static int Unique(AppearanceDrawState s)=>s.Owned.Count(PoolIds.Contains);
    static AppearanceDrawState Fresh(int i)
    {
        var old=new CollectionData{ProfileSeed=Seed(i)};
        var state=AppearanceDrawEngine.Replay(old,Array.Empty<CollectionReceipt>(),101);
        AppearanceDrawEngine.Generate(state,old.ProfileSeed,Request,"FirstBlue",3);
        return state;
    }
    static double[] Quantiles(IEnumerable<double> values)
    {
        var a=values.OrderBy(x=>x).ToArray();if(a.Length==0)return new double[0];
        return new[]{.1,.5,.9}.Select(q=>a[(int)Math.Round((a.Length-1)*q)]).ToArray();
    }
    static object BatchAudit()
    {
        var rows=new Dictionary<int,List<double[]>>();foreach(var b in new[]{1,2,3,5,10})rows[b]=new List<double[]>();
        var singlesCost=new List<double>();
        for(var n=1;n<=N;n++)
        {
            var s=Fresh(n);long spent=0;
            for(var batch=1;batch<=10;batch++)
            {
                var before=Unique(s);var tickets=s.Tickets;
                var r=AppearanceDrawEngine.Generate(s,Seed(n),Request,"Ten",4);spent+=r.CoinCost;
                if(rows.ContainsKey(batch))rows[batch].Add(new[]{(double)Unique(s),Unique(s)-before,10-(Unique(s)-before),s.Tickets-tickets,(double)spent});
            }
            s=Fresh(n);long singles=0;for(var k=0;k<10;k++)singles+=AppearanceDrawEngine.Generate(s,Seed(n),Request,"Single",4).CoinCost;
            singlesCost.Add(singles);
        }
        return new {batches=rows.Select(kv=>new{batch=kv.Key,unique=Quantiles(kv.Value.Select(x=>x[0])),newInBatch=Quantiles(kv.Value.Select(x=>x[1])),
            meanNew=kv.Value.Average(x=>x[1]),zeroNewRate=kv.Value.Count(x=>x[1]==0)/(double)N,meanDuplicate=kv.Value.Average(x=>x[2]),
            ticketGain=Quantiles(kv.Value.Select(x=>x[3])),coinsSpent=Quantiles(kv.Value.Select(x=>x[4]))}),
            firstTenSinglesCost=Quantiles(singlesCost),meanFirstTenSinglesCost=singlesCost.Average()};
    }
    static void Exchange(AppearanceDrawState s,int n,int level)
    {
        if(level<5)return;
        while(true)
        {
            var target=AppearanceDrawEngine.Pool.Where(x=>!s.Owned.Contains(x.Id)&&CollectionRules.ExchangeTickets(x.Rarity)<=s.Tickets)
                .OrderByDescending(x=>x.Rarity).ThenBy(x=>x.Id,StringComparer.Ordinal).FirstOrDefault();
            if(target==null)return;
            AppearanceDrawEngine.Generate(s,Seed(n),Request,"Exchange",level,target.Id);
        }
    }
    static object Lifecycle(string name,string kind,int toolBudget,bool capped,bool exchange,int fixedPrice=0)
    {
        var checkpoints=new Dictionary<int,List<double[]>>();foreach(var l in new[]{10,30,60,100})checkpoints[l]=new List<double[]>();
        var firstBatches=new List<double>();var completeLevels=new List<double>();
        for(var n=1;n<=N;n++)
        {
            var s=Fresh(n);double wallet=0;int completed=0,first=0;long spent=0;
            for(var l=1;l<=100;l++)
            {
                // Income lower bound: all-default boat equipment, L1=7, subsequent levels=80 boats.
                wallet+=(capped?100+20*Math.Min(l-1,9):BattleCoinRules.FirstClear(l))+(l==1?7:80);
                if(l>=3)wallet-=Math.Min(wallet,toolBudget);
                // Gift is free after L2; suppress all paid transactions before then.
                if(l>=2)
                {
                    while(Unique(s)<14)
                    {
                        var price=(fixedPrice>0?fixedPrice:s.Price)*(kind=="Ten"?9:1);
                        if(l+1<(kind=="Ten"?4:3)||wallet<price+650)break;
                        AppearanceDrawEngine.Generate(s,Seed(n),Request,kind,l+1);
                        wallet-=price;spent+=price;if(first==0&&kind=="Ten")first=l;
                        if(exchange)Exchange(s,n,l+1);
                    }
                    if(Unique(s)==14&&completed==0)completed=l;
                }
                if(checkpoints.ContainsKey(l))checkpoints[l].Add(new[]{(double)Unique(s),wallet,(double)s.Draws,(double)spent,completed>0?1.0:0.0});
            }
            if(first>0)firstBatches.Add(first);if(completed>0)completeLevels.Add(completed);
        }
        return new{name,kind,toolBudget,capped,exchange,fixedPrice,reserve=650,
            firstTenRate=firstBatches.Count/(double)N,firstTenLevel=Quantiles(firstBatches),completionRate=completeLevels.Count/(double)N,completionLevel=Quantiles(completeLevels),
            checkpoints=checkpoints.Select(kv=>new{level=kv.Key,unique=Quantiles(kv.Value.Select(x=>x[0])),wallet=Quantiles(kv.Value.Select(x=>x[1])),
                draws=Quantiles(kv.Value.Select(x=>x[2])),coinsSpent=Quantiles(kv.Value.Select(x=>x[3])),completeRate=kv.Value.Average(x=>x[4])})};
    }
    static object EndPoolAudit()
    {
        var zero=0;var nearZero=0;var fullAllowed=0;
        for(var n=1;n<=N;n++)
        {
            var s=Fresh(n);s.Owned.UnionWith(PoolIds);s.Owned.Remove("TF_SKIN_K31");s.DuplicateDry=4;
            var before=Unique(s);AppearanceDrawEngine.Generate(s,Seed(n),Request,"Ten",101);if(Unique(s)==before)zero++;
            s=Fresh(n);s.Owned.UnionWith(PoolIds);s.Owned.Remove("TF_SKIN_K31");s.DuplicateDry=4;s.RedDry=59;
            before=Unique(s);AppearanceDrawEngine.Generate(s,Seed(n),Request,"Ten",101);if(Unique(s)==before)nearZero++;
            s=Fresh(n);s.Owned.UnionWith(PoolIds);
            var r=AppearanceDrawEngine.Generate(s,Seed(n),Request,"Ten",101);if(r.CoinCost==9000&&r.SkinIds.Length==10)fullAllowed++;
        }
        return new{assumption="Controlled states, not sampled population: 13/14, missing one red; gold dry 0; red dry 0 or 59. Full-pool engine behavior only.",
            noNewWithoutNearRedPity=zero/(double)N,noNewWithRedPityNextDraw=nearZero/(double)N,fullPoolChargedTen=fullAllowed};
    }
    public static void Main()
    {
        if(PoolIds.Count!=14)throw new Exception("Audit assumes the current 14-style pool.");
        var result=new{profilesPerScenario=N,seedRange="000...001 through 000...bb8",quantiles="P10/P50/P90 nearest rank (rounded index)",
            method="Production C# draw engine. Synthetic profiles. Batch audit has no exchanges. Lifecycle uses minimum boat income, no ads/payment/retries, all 100 clears, optional highest-rarity-affordable exchanges after each purchase, stops buying at completion. Fixed per-clear tool budgets are sensitivity assumptions, not a simulation of actual tool demand or inventory. Candidate fixed price/cap affect audit wallet only; draw rules remain production.",
            pool=AppearanceDrawEngine.Pool.Select(x=>new{x.Id,x.ChineseName,rarity=x.Rarity.ToString()}),batchAudit=BatchAudit(),endPool=EndPoolAudit(),
            lifecycle=new[]{Lifecycle("current-ten-no-tools-no-exchange","Ten",0,false,false),Lifecycle("current-ten-no-tools-exchange","Ten",0,false,true),
                Lifecycle("current-single-75-exchange","Single",75,false,true),Lifecycle("current-ten-75-exchange","Ten",75,false,true),
                Lifecycle("current-ten-250-exchange","Ten",250,false,true),Lifecycle("candidate-single-75-exchange","Single",75,true,true,600),
                Lifecycle("candidate-ten-75-exchange","Ten",75,true,true,600),Lifecycle("candidate-ten-250-exchange","Ten",250,true,true,600)}};
        Console.WriteLine(JsonConvert.SerializeObject(result,Formatting.Indented));
    }
}
