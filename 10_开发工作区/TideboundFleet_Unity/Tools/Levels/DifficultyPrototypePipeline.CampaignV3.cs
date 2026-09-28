using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using Tidebound.Tools;

// Offline authoring only. No game rules, UI assets or existing campaigns are rewritten here.
internal static partial class DifficultyPrototypePipeline
{
    static readonly List<GridPosition> V3Protected = new List<GridPosition>();
    static readonly List<string> V3Starts = new List<string>();
    static readonly List<string> V3Partials = new List<string>();
    static readonly List<string> V3CoreKeys = new List<string>();
    static string V3LibraryPath => Path.Combine(output, "core-library.json");
    static void CampaignV3(string[] args)
    {
        output = Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
        if (args[3] == "validate-pack") { Console.WriteLine(CampaignV3Validator.Validate(File.ReadAllText(Path.Combine(output,"manifest-"+args[4]+".json")),file=>File.ReadAllText(Path.Combine(output,file)))); return; }
        if (args[3] == "audit-peeling") {
            var easy=new List<int>();
            for(int n=4;n<=100;n++){var l=LevelJsonReader.Read(File.ReadAllText(Path.Combine(output,"TF_V3_"+n.ToString("D3")+".json")));if(LevelDifficultyAnalysis.Peel(Board(l)).IsComplete)easy.Add(n);}
            Console.WriteLine("Exit-only levels from 4: "+string.Join(",",easy));return;
        }
        if (args[3] == "cores") { V3BuildCores(); return; }
        if (args[3] == "pack") { V3Pack(int.Parse(args[4])); return; }
        if (args[3] == "certify") { V3Certify(int.Parse(args[4])); return; }
        V3Generate(int.Parse(args[3]), args.Length > 4 ? int.Parse(args[4]) : 0);
    }
    static void V3BuildCores()
    {
        var random = new Random(928031); var library = new JArray(); var known = new HashSet<string>();
        for (int trial = 0; trial < 1800 && library.Count < 24; trial++)
        {
            var l = Prototype(true);
            if (trial > 0)
            {
                var edits = 1 + random.Next(3);
                for (int k=0;k<edits;k++)
                {
                    int i = random.Next(2,7); if(i==6) { l.Ships[i]=Ship("G",2,5,ShipDirection.Down,3); continue; }
                    var s=l.Ships[i]; var d=(ShipDirection)random.Next(4);
                    var x=Math.Max(0,Math.Min(5,s.Position.X+random.Next(-2,3)));
                    var y=Math.Max(0,Math.Min(5,s.Position.Y+random.Next(-2,3)));
                    l.Ships[i]=Ship(s.Id,x,y,d,random.Next(5)==0?3:2);
                }
            }
            BoardModel board;
            try { Occupancy(l); board=Board(l); } catch(InvalidOperationException) { continue; } catch(ArgumentException) { continue; }
            var key=LevelStateIdentity.Fingerprint(board); if(known.Contains(key))continue;
            if((int)BridgeQuality(board)["oneGapRun"]>2)continue;
            var a=board.QueryForwardPath("A"); if(a.TravelDistance!=1 || a.CanExit)continue;
            var parent=board.ApplyPathResult(a);var h=parent.QueryForwardPath("H");if(h.TravelDistance<1||h.CanExit)continue;
            if(BoardDependencyAnalyzer.Analyze(parent.ApplyPathResult(h)).HardLockedCycleCount==0)continue;
            var solved=LevelSolver.Solve(parent,new LevelSolverOptions(40000,1500000,350));
            if(solved.Status!=LevelSolverStatus.Solved)continue;
            var route=new[]{"A"}.Concat(solved.ShipIds).ToArray();
            var proof=LevelSolutionProof.Create(l.LevelId,board,route);
            if(proof.Steps.Any(s=>s.Outcome==ForwardPathOutcome.Blocked&&s.ShipId!="A"&&s.ShipId!="B"))continue;
            known.Add(key); library.Add(new JObject { ["key"]="Crossing"+library.Count.ToString("D2"),["layout"]=JObject.Parse(LevelJsonWriter.Write(l)),["route"]=new JArray(route),["fingerprint"]=key });
            Console.WriteLine("core "+library.Count+" trial "+trial+" longs "+l.Ships.Count(s=>s.Length==3));
        }
        Require(library.Count>=8,"Insufficient certified relation variants");
        File.WriteAllText(V3LibraryPath,library.ToString());
    }
    static GridPosition V3Transform(GridPosition p,int dx,int dy,int rotation,bool mirror)
    {
        var x=mirror?5-p.X:p.X;var y=p.Y;
        for(int k=0;k<rotation;k++){var t=x;x=5-y;y=t;}return new GridPosition(x+dx,y+dy);
    }
    static ShipDirection V3Turn(ShipDirection d,int rotation,bool mirror)
    {
        if(mirror && (d==ShipDirection.Left||d==ShipDirection.Right))d=RemainingFleetShuffler.Opposite(d);
        for(int k=0;k<rotation;k++)d=d==ShipDirection.Up?ShipDirection.Left:d==ShipDirection.Left?ShipDirection.Down:d==ShipDirection.Down?ShipDirection.Right:ShipDirection.Up;
        return d;
    }
    static void V3Core(List<ShipPlacementData> ships,JToken core,string prefix,int dx,int dy,int rotation,bool mirror)
    {
        var l=LevelJsonReader.Read(core["layout"].ToString());
        foreach(var s in l.Ships){var p=V3Transform(s.Position,dx,dy,rotation,mirror);ships.Add(Ship(prefix+s.Id,p.X,p.Y,V3Turn(s.Direction,rotation,mirror),s.Length));}
        foreach(var p in new[]{new GridPosition(1,3),new GridPosition(3,2),new GridPosition(2,2)})V3Protected.Add(V3Transform(p,dx,dy,rotation,mirror));
        V3Starts.Add(prefix+"A");V3Partials.Add(prefix+"B");V3CoreKeys.Add((string)core["key"]);
    }
    static void V3Generate(int number,int retry)
    {
        var targets=JObject.Parse(File.ReadAllText(Path.Combine(root,"../../40_项目交接文档/CAMPAIGN_100_V3_TARGETS_20260928.json")));
        var target=targets["levels"][number-1];var count=(int)target["targetShipCount"];var longs=(int)target["targetLongShips"];var risks=(int)target["riskPointsTarget"];
        var id="TF_V3_"+number.ToString("D3");
        if(File.Exists(Path.Combine(output,id+".json"))) { Console.WriteLine(id+" already generated; certify separately");return; }
        if(number==1)
        {
            var original=Path.Combine(root,"Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates/P5R_Ten_001.json");
            var l1=Clone(LevelJsonReader.Read(File.ReadAllText(original)),id);var solved=LevelSolver.Solve(Board(l1));
            V3Save(l1,solved.ShipIds.ToList(),target);return;
        }
        var library=JArray.Parse(File.ReadAllText(V3LibraryPath));var random=new Random(928000+number*113+retry*100003);
        LevelData l=null;List<string> route=null;
        V3Protected.Clear();V3Starts.Clear();V3Partials.Clear();V3CoreKeys.Clear();
        var coreCount=number==4?1:risks;
        var cores=new List<ShipPlacementData>();
        for(int i=0;i<coreCount;i++)
        {
            int x,y;
            if(coreCount==1){x=random.Next(1,8);y=random.Next(2,11);}
            else if(coreCount==2){x=i==0?random.Next(0,3):random.Next(6,9);y=i==0?random.Next(0,4):random.Next(9,13);}
            else{x=i==1?random.Next(7,9):random.Next(0,3);y=i*6;}
            if(number==4)
            {
                var p4=Prototype(false);var rotation=random.Next(4);
                foreach(var s in p4.Ships){var p=V3Transform(s.Position,x,y,rotation,false);cores.Add(Ship("K0"+s.Id,p.X,p.Y,V3Turn(s.Direction,rotation,false)));}
                V3Starts.Add("K0A");V3Partials.Add("K0B");V3CoreKeys.Add("ForgivingP4");
                V3Protected.Add(V3Transform(new GridPosition(1,3),x,y,rotation,false));
            }
            else V3Core(cores,library[(number*7+i*11+retry)%library.Count],"K"+i,x,y,random.Next(4),random.Next(2)==0);
        }
        for(int attempt=0;attempt<50;attempt++)
        {
            var items=cores.ToList();
            l=new LevelData{SchemaVersion=2,LevelId=id,Width=14,Height=18,Ships=items.ToArray(),BossId="TF_KRAKEN_01"};
            route=new BridgeGrid(l).V3Peel(V3Starts.ToArray(),V3Partials.ToArray())?.ToList();
            if(route==null)throw new InvalidOperationException("Core composition needs another seed");
            while(items.Count<count)
            {
                l.Ships=items.ToArray();var grid=Occupancy(l);
                var remain=count-items.Count;var needLong=longs-items.Count(s=>s.Length==3);
                var length=needLong>0 && (remain<=needLong+1 || items.Count%Math.Max(3,count/Math.Max(1,longs))==0)?3:2;
                var ranked=new List<Tuple<ShipPlacementData,double>>();
                for(int d=0;d<4;d++)for(int y=0;y<18;y++)for(int x=0;x<14;x++)
                {
                    var s=Ship("Z"+items.Count.ToString("D3"),x,y,(ShipDirection)d,length);
                    var cells=GridFootprint.Cells(s.Position,s.Direction,length).ToArray();
                    if(cells.Any(c=>c.X<0||c.X>=14||c.Y<0||c.Y>=18||grid[c.X,c.Y]>=0||V3Protected.Contains(c)))continue;
                    var neighbours=cells.Sum(c=>new[]{new GridPosition(c.X+1,c.Y),new GridPosition(c.X-1,c.Y),new GridPosition(c.X,c.Y+1),new GridPosition(c.X,c.Y-1)}.Count(n=>n.X>=0&&n.X<14&&n.Y>=0&&n.Y<18&&grid[n.X,n.Y]>=0));
                    ranked.Add(Tuple.Create(s,random.NextDouble()*6+neighbours*.7-items.Count(b=>b.Direction==s.Direction)*.3));
                }
                bool added=false;
                foreach(var c in ranked.OrderByDescending(v=>v.Item2))
                {
                    l.Ships=items.Concat(new[]{c.Item1}).ToArray();var next=route.ToList();next.Add(c.Item1.Id);var fast=new BridgeGrid(l);
                    if(!fast.Replays(next.Select(name=>Array.FindIndex(l.Ships,s=>s.Id==name)).ToArray()))
                    {
                        next=route.ToList();next.Insert(V3Starts.Count,c.Item1.Id);
                        if(!fast.Replays(next.Select(name=>Array.FindIndex(l.Ships,s=>s.Id==name)).ToArray()))continue;
                    }
                    items.Add(c.Item1);route=next;added=true;break;
                }
                if(!added)break;
            }
            l.Ships=items.ToArray();Console.WriteLine(id+" fill="+items.Count+" attempt="+attempt);
            if(items.Count==count&&items.Count(s=>s.Length==3)==longs)break;
            if(attempt==49)throw new InvalidOperationException("Fill exhausted");
        }
        var best=V3Mix(l,route,random);
        V3Save(best,route,target);
    }
    static LevelData V3Mix(LevelData source,List<string> route,Random random)
    {
        var l=Clone(source);var state=new BridgeGrid(l);var cost=state.Cost()+state.V3Edges();var bestCost=cost;var best=Clone(l);var bestRoute=route.ToArray();
        var indices=route.Select(id=>Array.FindIndex(l.Ships,s=>s.Id==id)).ToArray();
        for(int iteration=0;iteration<3000000;iteration++)
        {
            if(iteration>0&&iteration%60000==0){Console.WriteLine(l.LevelId+" mix="+iteration+" best="+bestCost.ToString("0.00"));l=Clone(best);state=new BridgeGrid(l);cost=bestCost;route.Clear();route.AddRange(bestRoute);indices=route.Select(id=>Array.FindIndex(l.Ships,s=>s.Id==id)).ToArray();}
            int i=random.Next(l.Ships.Length);var old=l.Ships[i];if(!old.Id.StartsWith("Z"))continue;
            var mode=random.Next(10);var dir=(ShipDirection)random.Next(4);int x=old.Position.X,y=old.Position.Y;
            if(mode<3){var head=GridFootprint.Cells(old.Position,old.Direction,old.Length).Last();dir=RemainingFleetShuffler.Opposite(old.Direction);x=head.X;y=head.Y;}
            else if(mode<8){x+=random.Next(-3,4);y+=random.Next(-3,4);}else{x=random.Next(14);y=random.Next(18);}
            var s=Ship(old.Id,x,y,dir,old.Length);if(!state.Fits(s,i)||GridFootprint.Cells(s.Position,s.Direction,s.Length).Any(V3Protected.Contains))continue;
            l.Ships[i]=s;var next=new BridgeGrid(l);var nc=next.Cost()+next.V3Edges();var temp=3.5*(1-(iteration%60000)/60000d)+.06;
            if(nc>cost&&random.NextDouble()>=Math.Exp((cost-nc)/temp)){l.Ships[i]=old;continue;}
            string[] alternative=null;
            if(!next.Replays(indices)){alternative=next.V3Peel(V3Starts.ToArray(),V3Partials.ToArray());if(alternative==null){l.Ships[i]=old;continue;}}
            if(alternative!=null){route.Clear();route.AddRange(alternative);indices=route.Select(name=>Array.FindIndex(l.Ships,sh=>sh.Id==name)).ToArray();}
            state=next;cost=nc;
            if(cost<bestCost){bestCost=cost;best=Clone(l);bestRoute=route.ToArray();}
            if(next.Passes()&&next.V3Edges()==0){Console.WriteLine(l.LevelId+" quality passed at "+iteration);return Clone(l);}
        }
        throw new InvalidOperationException("Mix exhausted "+l.LevelId+" cost="+bestCost);
    }
    static LevelData V3Data(BoardModel b,string id) => new LevelData { SchemaVersion=2,LevelId=id,Width=b.Width,Height=b.Height,BossId="TF_KRAKEN_01",
        Ships=b.Ships.Select(s=>Ship(s.Id,s.Position.X,s.Position.Y,s.Direction,s.Length)).ToArray() };
    static void V3Save(LevelData l,List<string> route,JToken target)
    {
        var board=Board(l);var proof=LevelSolutionProof.Create(l.LevelId,board,route);Require(proof.Replay(l.LevelId,board).IsComplete,"Proof invalid");
        Require((int)target["number"]<4 || !LevelDifficultyAnalysis.Peel(board).IsComplete,"Exit-only route bypasses the intended relation");
        var risks=new JArray();
        foreach(var start in V3Starts.Take((int)target["riskPointsTarget"]))
        {
            var parent=board.ApplyPathResult(board.QueryForwardPath(start));var wrong=start.Substring(0,start.Length-1)+"H";
            var dead=parent.ApplyPathResult(parent.QueryForwardPath(wrong));
            Require(BoardDependencyAnalyzer.Analyze(dead).HardLockedCycleCount>0,"Wrong branch is not proven dead");
            var safe=new BridgeGrid(V3Data(parent,l.LevelId)).V3Peel(V3Starts.Where(s=>s!=start).ToArray(),V3Partials.ToArray());
            Require(safe!=null,"Risk safe continuation missing");LevelSolutionProof.Create(l.LevelId,parent,safe);
            JObject recovery=null;
            foreach(var ship in dead.Ships.OrderByDescending(s=>s.Id==wrong).ThenBy(s=>s.Id))
            {
                var repaired=dead.WithPlacements(dead.Ships.Select(s=>s.Id==ship.Id?s.WithPlacement(s.OccupiedCells[s.Length-1],RemainingFleetShuffler.Opposite(s.Direction)):s));
                var rr=new BridgeGrid(V3Data(repaired,l.LevelId)).V3Peel(V3Starts.Where(s=>s!=start).ToArray(),V3Partials.ToArray());
                if(rr==null)continue;
                var rp=LevelSolutionProof.Create(l.LevelId,repaired,rr);
                recovery=new JObject{["tool"]="Reverse",["target"]=ship.Id,["afterFingerprint"]=LevelStateIdentity.Fingerprint(repaired),["solution"]=JObject.Parse(LevelProofJson.Write(rp))};break;
            }
            Require(recovery!=null,"No certified one-tool recovery "+start);
            risks.Add(new JObject{["prefix"]=new JArray(start),["wrongMove"]=wrong,["deadFingerprint"]=LevelStateIdentity.Fingerprint(dead),["method"]="ClosedZeroTravelCycle",["safeContinuation"]=new JArray(safe),["recovery"]=recovery});
        }
        File.WriteAllText(Path.Combine(output,l.LevelId+".json"),LevelJsonWriter.Write(l));
        File.WriteAllText(Path.Combine(output,l.LevelId+".solution.json"),LevelProofJson.Write(proof));
        File.WriteAllText(Path.Combine(output,l.LevelId+".analysis.json"),new JObject{["version"]="CampaignV3.1",["number"]=(int)target["number"],["status"]="CoreCertified",["ships"]=l.Ships.Length,["longShips"]=l.Ships.Count(s=>s.Length==3),["bossHp"]=l.Ships.Length*10,["quality"]=BridgeQuality(board),["edgePenalty"]=new BridgeGrid(l).V3Edges(),["riskPoints"]=risks,["coreVariants"]=new JArray(V3CoreKeys),["witnessSteps"]=proof.Steps.Count,["humanPlaytest"]="Pending",["deviceValidation"]="Pending"}.ToString());
        Console.WriteLine(l.LevelId+" DONE ships="+l.Ships.Length+" risk="+risks.Count);
    }
    static void V3Pack(int count)
    {
        var rows=new JArray();
        for(int n=1;n<=count;n++)
        {
            var id="TF_V3_"+n.ToString("D3");var l=LevelJsonReader.Read(File.ReadAllText(Path.Combine(output,id+".json")));
            var analysis=JObject.Parse(File.ReadAllText(Path.Combine(output,id+".analysis.json")));
            var row=new JObject{["number"]=n,["levelId"]=id,["contentRevision"]=3,["ships"]=l.Ships.Length,["longShips"]=l.Ships.Count(s=>s.Length==3),
                ["fingerprint"]=LevelStateIdentity.Fingerprint(Board(l)),["riskPoints"]=analysis["riskPoints"].Count()};
            foreach(var kind in new[]{"layout","proof","analysis"})
            {
                var file=id+(kind=="layout"?".json":kind=="proof"?".solution.json":".analysis.json");
                row[kind+"File"]=file;row[kind+"Sha256"]=CampaignPackValidator.Sha256(File.ReadAllText(Path.Combine(output,file)));
            }
            rows.Add(row);
        }
        var archive=new JArray();var old=JObject.Parse(File.ReadAllText(Path.Combine(root,"Assets/Tidebound/Config/Levels/Campaign/manifest-100.json")));
        foreach(var row in old["levels"].Take(count))
        {
            var file=(string)row["layoutFile"];var folder=file.StartsWith("P5R_Ten_")?"LevelPrototypes/Phase5R_TenLevelCandidates/":"Levels/Campaign/";
            archive.Add(new JObject{["number"]=archive.Count+1,["levelId"]=(string)row["levelId"],["layoutFile"]=file,["layoutSha256"]=CampaignPackValidator.Sha256(File.ReadAllText(Path.Combine(root,"Assets/Tidebound/Config/"+folder+file)))});
        }
        var manifest=new JObject{["manifestVersion"]=1,["campaignVersion"]=CampaignV3Validator.Version,["rulesVersion"]=LevelRules.Version,["status"]="ModelVerified",["levels"]=rows,["previousRevisions"]=archive};
        var text=manifest.ToString();var result=CampaignV3Validator.Validate(text,file=>File.ReadAllText(Path.Combine(output,file)));
        File.WriteAllText(Path.Combine(output,"manifest-"+count+".json"),text);
        File.WriteAllText(Path.Combine(output,"validation-"+count+".json"),result.ToString());
        Console.WriteLine(result.ToString());
    }
    static void V3Certify(int count)
    {
        for(int n=1;n<=count;n++)
        {
            var id="TF_V3_"+n.ToString("D3");var l=LevelJsonReader.Read(File.ReadAllText(Path.Combine(output,id+".json")));var b=Board(l);
            var proof=LevelProofJson.Read(File.ReadAllText(Path.Combine(output,id+".solution.json")));Require(proof.Replay(id,b).IsComplete,"Correct route "+id);
            var report=JObject.Parse(File.ReadAllText(Path.Combine(output,id+".analysis.json")));
            if(n>1){CheckBridgeQuality(b);Require(new BridgeGrid(l).V3Edges()==0,"Edge mix "+id);}
            foreach(var risk in report["riskPoints"])
            {
                var parent=b;foreach(var a in risk["prefix"].Values<string>())parent=parent.ApplyPathResult(parent.QueryForwardPath(a));
                LevelSolutionProof.Create(id,parent,risk["safeContinuation"].Values<string>().ToArray());
                var wrong=(string)risk["wrongMove"];var dead=parent.ApplyPathResult(parent.QueryForwardPath(wrong));Require(BoardDependencyAnalyzer.Analyze(dead).HardLockedCycleCount>0,"Deadlock "+id);
                Require(LevelStateIdentity.Fingerprint(dead)==(string)risk["deadFingerprint"],"Dead state mismatch");
                var t=(string)risk["recovery"]["target"];var repaired=dead.WithPlacements(dead.Ships.Select(s=>s.Id==t?s.WithPlacement(s.OccupiedCells[s.Length-1],RemainingFleetShuffler.Opposite(s.Direction)):s));
                Require(LevelProofJson.Read(risk["recovery"]["solution"].ToString()).Replay(id,repaired).IsComplete,"Recovery "+id);
            }
        }
        Console.WriteLine("CERTIFIED "+count);
    }
    private sealed partial class BridgeGrid
    {
        public double V3Edges()
        {
            double penalty=0;
            for(int side=0;side<4;side++)for(int band=1;band<=2;band++)
            {
                int total=0;var counts=new int[4];
                for(int i=0;i<xs.Length;i++)
                {
                    bool touches=false;for(int k=0;k<lengths[i];k++){int x=xs[i]+dx[i]*k,y=ys[i]+dy[i]*k;if(side==0?y>=level.Height-band:side==1?y<band:side==2?x<band:x>=level.Width-band){touches=true;break;}}
                    if(touches){counts[directions[i]]++;total++;}
                }
                penalty+=Math.Max(0,(band==1?2:3)-counts.Count(c=>c>0))*40;
                penalty+=Math.Max(0,counts.Max()-total*(band==1?.6:.55))*35;
            }
            return penalty;
        }
        public string[] V3Peel(string[] prefix,string[] partials)
        {
            var occ=(int[])grid.Clone();var px=(int[])xs.Clone();var py=(int[])ys.Clone();var gone=new bool[xs.Length];var result=new List<string>();
            Func<int,bool> apply=i=>{if(i<0||gone[i])return false;var x=px[i]+dx[i]*lengths[i];var y=py[i]+dy[i]*lengths[i];var distance=0;
                while(Inside(x,y)&&occ[y*level.Width+x]<0){x+=dx[i];y+=dy[i];distance++;}
                var exit=!Inside(x,y);if(!exit&&distance==0)return false;
                for(var k=0;k<lengths[i];k++)occ[(py[i]+k*dy[i])*level.Width+px[i]+k*dx[i]]=-1;
                if(exit)gone[i]=true;else{px[i]+=dx[i]*distance;py[i]+=dy[i]*distance;for(var k=0;k<lengths[i];k++)occ[(py[i]+k*dy[i])*level.Width+px[i]+k*dx[i]]=i;}
                result.Add(level.Ships[i].Id);return true;};
            foreach(var id in prefix)if(!apply(Array.FindIndex(level.Ships,s=>s.Id==id)))return null;
            var partialIndices=partials.Select(id=>Array.FindIndex(level.Ships,s=>s.Id==id)).Where(i=>i>=0).ToArray();
            for(int round=0;round<xs.Length*4;round++)
            {
                int selected=-1;
                for(int i=0;i<xs.Length;i++){if(gone[i])continue;var x=px[i]+dx[i]*lengths[i];var y=py[i]+dy[i]*lengths[i];while(Inside(x,y)&&occ[y*level.Width+x]<0){x+=dx[i];y+=dy[i];}if(!Inside(x,y)){selected=i;break;}}
                if(selected<0)foreach(int i in partialIndices){if(gone[i])continue;var x=px[i]+dx[i]*lengths[i];var y=py[i]+dy[i]*lengths[i];if(Inside(x,y)&&occ[y*level.Width+x]<0){selected=i;break;}}
                if(selected<0)break;apply(selected);
            }
            return gone.All(v=>v)?result.ToArray():null;
        }
    }
}
