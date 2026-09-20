using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Tidebound.Save
{
    public static class BattleCoinRules
    {
        public const string Version="BattleCoinsV1";
        public static int FirstClear(int level) {if(level<1 || level>10000)throw new ArgumentOutOfRangeException(nameof(level));return checked(100+20*(level-1));}
        public static double Probability(int matching,int standardCount)
        {if(matching<1 || standardCount<matching)throw new ArgumentOutOfRangeException(nameof(matching));return Math.Max(.10,Math.Min(.80,.10+.875*(1-(double)matching/standardCount)));}
        public static int Calculate(string seed,SavedShip ship,SavedShip[] all)
        {
            if(ship.Length==3)return 1;
            if(!new[]{1,2,3,5,8}.Contains(ship.CoinCap))throw new ArgumentException("Unknown coin cap.");
            var p=Probability(all.Count(s=>s.Length==2 && s.SkinId==ship.SkinId),all.Count(s=>s.Length==2));var coins=1;
            using(var sha=SHA256.Create())for(var i=0;i<ship.CoinCap-1;i++)
            {
                var bytes=sha.ComputeHash(Encoding.UTF8.GetBytes(Version+"|"+seed+"|"+ship.Id.Length.ToString(CultureInfo.InvariantCulture)+":"+ship.Id+"|"+i.ToString(CultureInfo.InvariantCulture)));
                // Fixed byte order and 53 bits, independent of platform Random and attack order.
                ulong value=0;for(var n=0;n<7;n++)value=(value<<8)|bytes[n];var sample=(value>>3)/9007199254740992d;
                if(sample<p)coins++;
            }
            return coins;
        }
    }
}
