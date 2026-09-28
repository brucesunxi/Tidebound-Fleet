using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Combat;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;

namespace Tidebound.Config
{
    /// <summary>Authoring gate for the versioned, variable-fleet campaign. Does not estimate human win rates.</summary>
    public static class CampaignV3Validator
    {
        public const string Version = "CampaignV3.1";
        public static JObject Validate(string manifestJson, Func<string,string> read)
        {
            var manifest=JObject.Parse(manifestJson);var rows=(JArray)manifest["levels"];
            Check((string)manifest["campaignVersion"]==Version && (int?)manifest["manifestVersion"]==1 &&
                (string)manifest["rulesVersion"]==LevelRules.Version && rows!=null && (rows.Count==30||rows.Count==100),"Unsupported campaign");
            int ships=0, moves=0, partials=0, risks=0;double maximumOverlap=0;
            var previous=new List<LevelData>();var identities=new HashSet<string>();
            for(int i=0;i<rows.Count;i++)
            {
                var row=rows[i];var id=(string)row["levelId"];
                Check((int?)row["number"]==i+1 && id=="TF_V3_"+(i+1).ToString("D3") && identities.Add(id),"Level order or identity");
                var layoutText=ReadChecked(row,"layout",read);var proofText=ReadChecked(row,"proof",read);var analysisText=ReadChecked(row,"analysis",read);
                var level=LevelJsonReader.Read(layoutText);var proof=LevelProofJson.Read(proofText);var report=JObject.Parse(analysisText);
                Check(level.LevelId==id && level.Ships.Length==(int)row["ships"] && (i==0?level.Ships.Length==7:level.Ships.Length>=80&&level.Ships.Length<=92),"Fleet count");
                Check(level.Ships.Count(s=>s.Length==3)==(int)row["longShips"],"Long ship quota");
                using(var world=Create(level,i+1))
                {
                    var board=world.Session.Board;
                    Check(LevelStateIdentity.Fingerprint(board)==(string)row["fingerprint"],"Initial fingerprint");
                    Check(world.Session.Boss.InitialHp==level.Ships.Length*10,"Boss damage budget");
                    Check(proof.Replay(id,board).IsComplete,"Correct route replay");
                    if(i>0)CheckLayout(board);
                    if(i>=3)Check(!LevelDifficultyAnalysis.Peel(board).IsComplete,"Exit-only route bypasses the intended relation");
                    var records=(JArray)report["riskPoints"];
                    Check(records.Count==(int)row["riskPoints"] && (i<4||records.Count>=1),"Missing required risks");
                    Check(records.Select(r=>(string)r["wrongMove"]).Distinct().Count()==records.Count,"Repeated risk counted twice");
                    int stepIndex=0;
                    foreach(var step in proof.Steps)
                    {
                        Check(LevelStateIdentity.Fingerprint(world.Session.Board)==step.BeforeHash,"Runtime before move");
                        Move(world,step.ShipId);
                        Check(LevelStateIdentity.Fingerprint(world.Session.Board)==step.AfterHash,"Runtime after move");
                        moves++;if(step.Outcome==ForwardPathOutcome.Blocked)partials++;
                        if(++stepIndex==Math.Min(8,proof.Steps.Count-1))
                            using(var restored=SavedGameRuntime.Restore(world.Capture()))
                            {
                                Check(LevelStateIdentity.Fingerprint(restored.Session.Board)==step.AfterHash,"Checkpoint restore");
                                Check(restored.Session.Boss.Hp==world.Session.Boss.Hp,"Checkpoint battle restore");
                            }
                    }
                    Finish(world,level.Ships.Length);
                    foreach(var risk in records)
                    {
                        using(var branch=Create(level,i+1))
                        {
                            foreach(var a in risk["prefix"].Values<string>())Move(branch,a);
                            LevelSolutionProof.Create(id,branch.Session.Board,risk["safeContinuation"].Values<string>().ToArray());
                            Move(branch,(string)risk["wrongMove"]);
                            Check(LevelStateIdentity.Fingerprint(branch.Session.Board)==(string)risk["deadFingerprint"],"Wrong state fingerprint");
                            Check(BoardDependencyAnalyzer.Analyze(branch.Session.Board).HardLockedCycleCount>0,"Unproven deadlock");
                            var inventory=new ToolInventory();inventory.Grant("campaign-v3-cert",0,0,1);
                            using(var tool=new ShipToolSystem(branch.Session,branch.Movement,inventory))
                            {
                                Check((string)risk["recovery"]["tool"]=="Reverse","Unsupported recovery evidence");
                                Check(tool.Select(ShipTool.Reverse)==ToolUseStatus.Selected,"Reverse selection");
                                Check(tool.UseSelected((string)risk["recovery"]["target"])==ToolUseStatus.Applied,"Reverse application");
                                Check(inventory.Count(ShipTool.Reverse)==0 && branch.Session.ToolUses==1,"Tool cost");
                            }
                            Check(LevelStateIdentity.Fingerprint(branch.Session.Board)==(string)risk["recovery"]["afterFingerprint"],"Tool geometry");
                            var recovery=LevelProofJson.Read(risk["recovery"]["solution"].ToString());
                            Check(recovery.Replay(id,branch.Session.Board).IsComplete,"Tool continuation");
                            foreach(var step in recovery.Steps)Move(branch,step.ShipId);
                            Finish(branch,level.Ships.Length);risks++;
                        }
                    }
                }
                foreach(var other in previous)
                {
                    var overlap=GeometryOverlap(level,other);maximumOverlap=Math.Max(maximumOverlap,overlap);
                    Check(overlap<.6,"Near-duplicate layouts: "+id+" / "+other.LevelId);
                }
                previous.Add(level);ships+=level.Ships.Length;
            }
            return new JObject{["passed"]=true,["count"]=rows.Count,["ships"]=ships,["moves"]=moves,["partialMoves"]=partials,
                ["certifiedDeadlocksAndToolRecoveries"]=risks,["restoredCheckpoints"]=rows.Count,["maximumGeometryOverlap"]=maximumOverlap,
                ["manifestSha256"]=CampaignPackValidator.Sha256(manifestJson),["humanPlaytest"]="Pending",["deviceValidation"]="Pending"};
        }
        static string ReadChecked(JToken row,string kind,Func<string,string> read)
        {
            var text=read((string)row[kind+"File"]);
            Check(CampaignPackValidator.Sha256(text)==(string)row[kind+"Sha256"],"Changed "+kind+" asset");return text;
        }
        static SavedGameRuntime Create(LevelData level,int number) => SavedGameRuntime.Create(level,number,new LaneTransitTiming(.1,.01,.01),new CombatTiming(.01,.02));
        static void Move(SavedGameRuntime world,string id)
        {
            var path=world.Session.Board.QueryForwardPath(id);Check(path.CanExit||path.TravelDistance>0,"Zero-distance witness move");
            var result=world.Movement.TryBeginMove(id);Check(result.IsAccepted,"Move not accepted");
            Check(world.Movement.CompleteTravel(result.Operation.OperationId)==ShipMoveAdvanceStatus.Applied,"Travel not committed");
            if(result.Operation.WasBlocked)Check(world.Movement.CompleteBlockedFeedback(result.Operation.OperationId)==ShipMoveAdvanceStatus.Applied,"Partial feedback not completed");
            world.Transit.Advance(.25);world.Combat.Advance();
        }
        static void Finish(SavedGameRuntime world,int count)
        {
            world.Transit.Advance(30);world.Combat.Advance();
            Check(world.Session.Board.ShipCount==0 && world.Combat.IsVictorious && world.Session.Boss.Hp==0,"Battle not complete");
            Check(world.Combat.HitCount==count && world.Combat.Attacks.Select(a=>a.Ship.ShipId).Distinct().Count()==count,"Missing or duplicated attack");
        }
        public static void CheckLayout(BoardModel board)
        {
            var q=LocalLayoutAnalyzer.Analyze(board);
            Check(Math.Max(q.LongestGappedRow?.ShipCount??0,q.LongestGappedColumn?.ShipCount??0)<=2,"Same-direction wall");
            Check(q.LargestEmptyArea<=6 && q.MinimumWindowEntropy>=.65 && q.P10WindowEntropy>=.75,"Poor local mix");
            int exits=board.Ships.Count(s=>board.QueryForwardPath(s.Id).CanExit);Check(exits>=4&&exits<=8,"Opening exit budget");
            for(int side=0;side<4;side++)for(int band=1;band<=2;band++)
            {
                var selected=board.Ships.Where(s=>s.OccupiedCells.Any(c=>side==0?c.Y>=board.Height-band:side==1?c.Y<band:side==2?c.X<band:c.X>=board.Width-band)).ToArray();
                var groups=selected.GroupBy(s=>s.Direction).Select(g=>g.Count()).ToArray();
                Check(groups.Length>=(band==1?2:3) && groups.Max()<=selected.Length*(band==1?.6:.55),"Unbalanced outer edge");
            }
        }
        // Compare footprints + orientation, ignoring ship ids and array order, also across reflections/180°.
        public static double GeometryOverlap(LevelData a,LevelData b)
        {
            if(a.Width!=b.Width||a.Height!=b.Height)return 0;
            var key=new HashSet<string>(a.Ships.Select(s=>GeometryKey(s,a.Width,a.Height,false,false)));
            double maximum=0;
            foreach(bool flipX in new[]{false,true})foreach(bool flipY in new[]{false,true})
                maximum=Math.Max(maximum,b.Ships.Count(s=>key.Contains(GeometryKey(s,b.Width,b.Height,flipX,flipY)))/(double)Math.Min(a.Ships.Length,b.Ships.Length));
            return maximum;
        }
        static string GeometryKey(ShipPlacementData s,int width,int height,bool flipX,bool flipY)
        {
            int x=flipX?width-1-s.Position.X:s.Position.X,y=flipY?height-1-s.Position.Y:s.Position.Y;var d=s.Direction;
            if(flipX){if(d==ShipDirection.Left)d=ShipDirection.Right;else if(d==ShipDirection.Right)d=ShipDirection.Left;}
            if(flipY){if(d==ShipDirection.Up)d=ShipDirection.Down;else if(d==ShipDirection.Down)d=ShipDirection.Up;}
            return x+","+y+","+d+","+s.Length;
        }
        static void Check(bool condition,string message){if(!condition)throw new ArgumentException("Campaign V3: "+message);}
    }
}
