using System;
using System.Linq;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.Collection
{
    public static class CollectionShipAllocator
    {
        public static ShipRuntimeData[] Assign(ShipRuntimeData[] source,CollectionData profile,long attemptOrdinal)
        {
            var pool=profile.EffectiveEquipment();var ids=source.Where(s=>s.Length==2).OrderBy(s=>s.Id,StringComparer.Ordinal).Select(s=>s.Id).ToArray();
            var offset=(int)(attemptOrdinal%pool.Length);
            var bag=Enumerable.Range(0,ids.Length).Select(i=>pool[(i+offset)%pool.Length]).ToArray();
            var random=new CollectionRandom(profile.ProfileSeed+":ships:"+attemptOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
            for(var i=bag.Length-1;i>0;i--){var j=random.Next(i+1);var tmp=bag[i];bag[i]=bag[j];bag[j]=tmp;}
            var assignment=ids.Select((id,i)=>new{id,skin=bag[i]}).ToDictionary(x=>x.id,x=>x.skin);
            return source.Select(s=>new ShipRuntimeData(s.Id,s.TypeId,s.Length==3?FoundationLimits.DefaultLongSkinId:assignment[s.Id],s.Position,s.Direction,s.Length,s.Damage)).ToArray();
        }
    }
}
