using UnityEngine;

namespace Tidebound.Unity.Layout
{
    /// <summary>Approved wide-boss layout, in local bottom-left UI coordinates.</summary>
    public sealed class GameplayBattleLayout
    {
        public readonly float Unit, Width, Height, FleetHeight, FleetBottom, Waterline;
        public readonly bool Compact;
        public readonly Rect Health, Combo;
        public GameplayBattleLayout(float width,float height)
        {
            Width=width;Height=height;Unit=width/390f;Compact=height<190*Unit;
            var healthTop=(Compact?47:50)*Unit;
            Health=new Rect(27*Unit,height-healthTop-(Compact?16:18)*Unit,width-54*Unit,(Compact?16:18)*Unit);
            var fleetTop=(Compact?133:142)*Unit;
            FleetHeight=(Compact?29:42)*Unit;
            FleetBottom=height-fleetTop-FleetHeight;
            Waterline=height-fleetTop+Unit;
            // Keep the readable combo below the left of the health bar, clear of the boss face and fleet.
            Combo=new Rect(18*Unit,Waterline+12*Unit,104*Unit,24*Unit);
        }
        public Rect Boss(float inset,float aspect)
        {
            var margin=(Compact?15:inset)*Unit;var width=Width-2*margin;
            return new Rect(margin,Waterline,width,width/aspect);
        }
        public Rect Fleet(int slot)=>new Rect((slot+.5f)*Width/5-FleetHeight*.52f,FleetBottom,FleetHeight*1.04f,FleetHeight);
        public Rect Count(int slot)=>new Rect((slot+.5f)*Width/5-21*Unit,FleetBottom-15*Unit,42*Unit,12*Unit);
    }
}
