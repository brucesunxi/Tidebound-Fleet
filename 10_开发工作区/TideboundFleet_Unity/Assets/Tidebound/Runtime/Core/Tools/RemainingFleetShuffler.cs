using System;
using System.Diagnostics;
using System.Linq;
using Tidebound.Board;
using Tidebound.Ship;
using Tidebound.LevelDesign;

namespace Tidebound.Tools
{
    /// <summary>Change only five randomly chosen ships. Their combined footprint stays legal; Solver approves the whole board.</summary>
    public static class RemainingFleetShuffler
    {
        public static BoardModel Propose(BoardModel current,int seed,LevelSolverOptions options,int attempts=32)
        {
            var random=new Random(seed);var timer=Stopwatch.StartNew();
            options=options ?? new LevelSolverOptions();
            // Quickly reject a poor local candidate instead of spending the entire budget on its search tree.
            var probe=new LevelSolverOptions(Math.Min(1,options.MaxVisitedStates),options.MaxStoredFootprintCells,
                Math.Min(15,options.TimeLimitMilliseconds),options.UseExitPeeling,options.RulesVersion,options.ExitMode);
            for(var attempt=0;attempt<attempts && timer.ElapsedMilliseconds<150;attempt++)
            {
                var selected=current.Ships.ToArray();Randomize(selected,random);selected=selected.Take(5).ToArray();
                if(selected.Length==0)return null;
                var assigned=selected.ToDictionary(s=>s.Id,s=>s,StringComparer.Ordinal);
                foreach(var group in selected.GroupBy(s=>s.Length))
                {
                    var ships=group.ToArray();
                    var offset=ships.Length==1 ? 0 : random.Next(1,ships.Length);
                    for(var i=0;i<ships.Length;i++)
                    {
                        var slot=ships[(i+offset)%ships.Length];
                        var flip=ships.Length==1 || ships[i].Id==selected[0].Id;
                        assigned[ships[i].Id]=ships[i].WithPlacement(flip ? slot.OccupiedCells[slot.Length-1] : slot.Position,
                            flip ? Opposite(slot.Direction) : slot.Direction);
                    }
                }
                var candidate=current.WithPlacements(current.Ships.Select(s=>assigned.TryGetValue(s.Id,out var changed)?changed:s));
                if(LevelSolver.Solve(candidate,probe).Status==LevelSolverStatus.Solved)return candidate;
            }
            return null;
        }
        internal static void Randomize<T>(T[] values,Random random)
        { for(var i=values.Length-1;i>0;i--) { var j=random.Next(i+1);var temp=values[i];values[i]=values[j];values[j]=temp; } }
        internal static ShipDirection Opposite(ShipDirection d) => d==ShipDirection.Up ? ShipDirection.Down : d==ShipDirection.Down ? ShipDirection.Up : d==ShipDirection.Left ? ShipDirection.Right : ShipDirection.Left;
    }
}
