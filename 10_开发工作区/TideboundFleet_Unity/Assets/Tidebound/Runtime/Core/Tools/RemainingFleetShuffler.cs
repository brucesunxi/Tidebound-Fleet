using System;
using System.Diagnostics;
using System.Linq;
using Tidebound.Board;
using Tidebound.Ship;
using Tidebound.LevelDesign;

namespace Tidebound.Tools
{
    /// <summary>Recovery shuffle: remaining identities occupy proven initial slots of equal length.
    /// Symmetry and within-length permutations propose candidates; the current-rule solver must approve.</summary>
    public static class RemainingFleetShuffler
    {
        public static BoardModel Propose(BoardModel current,BoardModel initial,int seed,LevelSolverOptions options,int attempts=8)
        {
            var random=new Random(seed);var timer=Stopwatch.StartNew();
            for(var attempt=0;attempt<attempts && timer.ElapsedMilliseconds<150;attempt++)
            {
                var flipX=random.Next(2)==1;var flipY=random.Next(2)==1;
                var slots=initial.Ships.Where(s=>current.TryGetShip(s.Id,out _)).ToArray();
                var assigned=current.Ships.ToDictionary(s=>s.Id,s=>s,StringComparer.Ordinal);
                foreach(var length in new[]{2,3})
                {
                    var targets=slots.Where(s=>s.Length==length).ToArray();
                    var ships=current.Ships.Where(s=>s.Length==length).ToArray();
                    for(var i=targets.Length-1;i>0;i--) { var j=random.Next(i+1);var tmp=targets[i];targets[i]=targets[j];targets[j]=tmp; }
                    for(var i=0;i<ships.Length;i++)
                    {
                        var slot=targets[i];var x=flipX ? current.Width-1-slot.Position.X : slot.Position.X;
                        var y=flipY ? current.Height-1-slot.Position.Y : slot.Position.Y;var direction=slot.Direction;
                        if(flipX) { if(direction==ShipDirection.Left)direction=ShipDirection.Right;else if(direction==ShipDirection.Right)direction=ShipDirection.Left; }
                        if(flipY) { if(direction==ShipDirection.Up)direction=ShipDirection.Down;else if(direction==ShipDirection.Down)direction=ShipDirection.Up; }
                        assigned[ships[i].Id]=ships[i].WithPlacement(new GridPosition(x,y),direction);
                    }
                }
                var candidate=current.WithPlacements(current.Ships.Select(s=>assigned[s.Id]));
                if(current.Ships.All(s=>candidate.GetShip(s.Id).Position.Equals(s.Position) && candidate.GetShip(s.Id).Direction==s.Direction))continue;
                if(LevelSolver.Solve(candidate,options).Status==LevelSolverStatus.Solved) return candidate;
            }
            return null;
        }
    }
}
