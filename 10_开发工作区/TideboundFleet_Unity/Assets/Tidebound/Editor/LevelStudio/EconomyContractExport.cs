using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Collection;
using Tidebound.Save;
using Tidebound.Tools;
using UnityEditor;
using UnityEngine;

namespace Tidebound.EditorTools
{
    /// <summary>Read-only production oracle for the offline C3 model. Never loads a player save.</summary>
    public static class EconomyContractExport
    {
        [MenuItem("Tools/Tidebound/Export Economy Model Contract")]
        public static void Export()
        {
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            var output=Environment.GetEnvironmentVariable("TIDEBOUND_ECONOMY_CONTRACT") ?? Path.Combine(root,"Tools/Economy/production_contract.json");
            var files=new[]{"Runtime/Core/Collection/SkinCatalog.cs","Runtime/Core/Collection/CollectionData.cs","Runtime/Core/Collection/CollectionDrawEngine.cs",
                "Runtime/Core/Collection/CollectionShipAllocator.cs","Runtime/Core/Save/BattleCoinRules.cs","Runtime/Core/Save/PlayerSaveService.Draws.cs",
                "Runtime/Core/Save/PlayerSaveService.cs","Runtime/Core/Tools/ToolInventory.cs","Runtime/Core/Tools/ShipToolSystem.cs","Runtime/Core/Constants/FoundationLimits.cs","Resources/CoinToolShop.json",
                "Config/LevelPrototypes/Phase5R_TenLevelCandidates/manifest.json"}.ToList();
            var folder="Config/LevelPrototypes/Phase5R_TenLevelCandidates/";
            var manifest=JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Tidebound",folder,"manifest.json")));
            var levels=new List<object>();
            foreach(var row in manifest["levels"])
            {
                var name=(string)row["layoutFile"];files.Add(folder+name);
                var level=JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Tidebound",folder,name)));
                var ships=(JArray)level["ships"];levels.Add(new{number=levels.Count+1,ships=ships.Count,longs=ships.Count(s=>(int)s["length"]==3)});
            }
            var hashes=new SortedDictionary<string,string>();
            using(var sha=SHA256.Create())foreach(var file in files)hashes["Assets/Tidebound/"+file]=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Application.dataPath,"Tidebound",file)))).Replace("-","").ToLowerInvariant();
            var traces=new List<object>();
            for(var n=1;n<=8;n++)
            {
                var d=new CollectionData{ProfileSeed=n.ToString("x32")};var actions=new List<object>();
                for(var i=0;i<65;i++)
                {
                    var kind=i==0?"FirstBlue":i%3==0?"Ten":"Single";string target=null;
                    if(i>0 && i%7==0)
                    {
                        target=SkinCatalog.All.LastOrDefault(s=>!s.IsDefault && !d.OwnedIds.Contains(s.Id) && CollectionRules.ExchangeTickets(s.Rarity)<=d.Tickets)?.Id;
                        if(target!=null)kind="Exchange";
                    }
                    var receipt=CollectionDrawEngine.Generate(d,(i+1).ToString("x32"),kind,301,target);d.Validate(301);
                    actions.Add(new{kind,target,results=receipt.SkinIds,cost=receipt.CoinCost,ticketCost=receipt.TicketCost,
                        owned=d.OwnedIds.ToArray(),tickets=d.Tickets,draws=d.TotalDraws,gold=d.GoldDry,red=d.RedDry,duplicates=d.DuplicateDry});
                }
                traces.Add(new{seed=d.ProfileSeed,actions});
            }
            var result=new{contractVersion=1,collectionVersion=CollectionRules.Version,catalogVersion=SkinCatalog.Version,battleVersion=BattleCoinRules.Version,
                sources=hashes,catalog=SkinCatalog.All.Select(s=>new{id=s.Id,rarity=(int)s.Rarity,cap=CollectionDrawEngine.CoinCap(s.Rarity),duplicate=CollectionRules.DuplicateTickets(s.Rarity),exchange=CollectionRules.ExchangeTickets(s.Rarity)}),
                maxUsesPerAttempt=ShipToolSystem.MaxUsesPerAttempt,giftUnlock=new ToolGiftPolicy().UnlockLevel,giftInterval=new ToolGiftPolicy().Interval,
                probabilities=new[]{new[]{1,1},new[]{16,80},new[]{19,76},new[]{38,76},new[]{14,72},new[]{15,72},new[]{110,110}}.Select(pair=>new{matching=pair[0],standard=pair[1],p=BattleCoinRules.Probability(pair[0],pair[1])}),
                prices=Enumerable.Range(0,16).Select(CollectionRules.SinglePrice),firstClear=Enumerable.Range(1,300).Select(BattleCoinRules.FirstClear),levels,
                shop=JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"Tidebound/Resources/CoinToolShop.json"))),traces};
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonConvert.SerializeObject(result,Formatting.Indented),new UTF8Encoding(false));
            Debug.Log("Economy contract exported: 8 production traces, 520 validated transactions.");
        }
    }
}
