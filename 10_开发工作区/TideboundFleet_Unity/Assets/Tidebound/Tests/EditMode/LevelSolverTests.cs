using System;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class LevelSolverTests
    {
        [Test]
        public void CompleteRayKeepsFarBlockersAndDeduplicatesLongFootprints()
        {
            var board = Board(9, 4, Ship("A", 0, 1, ShipDirection.Right),
                Ship("B", 3, 1, ShipDirection.Right, 3), Ship("C", 7, 0, ShipDirection.Up));
            var graph = BoardDependencyAnalyzer.Analyze(board);
            CollectionAssert.AreEqual(new[] { "B", "C" }, graph.GetNode("A").AllBlockerShipIds);
            Assert.That(graph.GetNode("A").BlockerShipId, Is.EqualTo("B"));
            Assert.That(graph.GetNode("A").TravelDistance, Is.EqualTo(1));
            Assert.That(graph.CompleteDependencyDepth, Is.EqualTo(3));
            var after = board.ApplyPathResult(board.QueryForwardPath("B"));
            // B advances; it remains a blocker, and the original snapshot never changes.
            CollectionAssert.AreEqual(new[] { "B", "C" }, BoardDependencyAnalyzer.Analyze(after).GetNode("A").AllBlockerShipIds);
            CollectionAssert.AreEqual(new[] { "B", "C" }, graph.GetNode("A").AllBlockerShipIds);
        }

        [Test]
        public void PartialAdvanceCreatesANewBlockerForAnotherShip()
        {
            var board = Board(6, 5, Ship("A", 0, 2, ShipDirection.Right),
                Ship("B", 4, 1, ShipDirection.Up), Ship("C", 2, 0, ShipDirection.Up));
            var oldGraph = BoardDependencyAnalyzer.Analyze(board);
            Assert.That(oldGraph.GetNode("C").CanExit, Is.True);
            var next = board.ApplyPathResult(board.QueryForwardPath("A"));
            var graph = BoardDependencyAnalyzer.Analyze(next);
            Assert.That(next.GetShip("A").Position, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(graph.GetNode("C").CanExit, Is.False);
            CollectionAssert.AreEqual(new[] { "A" }, graph.GetNode("C").AllBlockerShipIds);
            Assert.That(oldGraph.GetNode("C").CanExit, Is.True);
        }

        [Test]
        public void NoExitCycleCanBeSolvedByPartialAdvanceAndTheSameShipMayAppearTwice()
        {
            var board = EscapableCycle();
            var graph = BoardDependencyAnalyzer.Analyze(board);
            Assert.That(graph.InitialExitCount, Is.Zero);
            Assert.That(graph.InitialMoveCount, Is.GreaterThan(0));
            Assert.That(graph.CompleteCycles.Count, Is.GreaterThan(0));
            Assert.That(graph.CompleteDependencyDepth, Is.Null);
            var result = LevelSolver.Solve(board);
            Assert.That(result.Status, Is.EqualTo(LevelSolverStatus.Solved), result.Reason);
            Assert.That(result.Method, Is.EqualTo("BreadthFirstSearch"));
            Assert.That(result.Optimality, Is.EqualTo(SolutionOptimality.Proven));
            // Four exits are necessary; no initial exit exists, so at least one extra move is necessary.
            Assert.That(result.ShipIds.Count, Is.EqualTo(5));
            Assert.That(result.ShipIds.Distinct().Count(), Is.EqualTo(4));
            var proof = LevelSolutionProof.Create("Cycle", board, result.ShipIds);
            Assert.That(proof.Steps[0].Outcome, Is.EqualTo(ForwardPathOutcome.Blocked));
            Assert.That(proof.Replay("Cycle", board).IsComplete, Is.True);
            Assert.That(board.ShipCount, Is.EqualTo(4));
        }

        [Test]
        public void ClosedZeroTravelCycleIsAProvenDeadlock()
        {
            var board = Board(6, 6, Ship("A", 1, 3, ShipDirection.Right), Ship("B", 3, 4, ShipDirection.Down),
                Ship("C", 4, 2, ShipDirection.Left), Ship("D", 2, 1, ShipDirection.Up));
            var result = LevelSolver.Solve(board);
            Assert.That(result.Status, Is.EqualTo(LevelSolverStatus.Deadlocked));
            Assert.That(result.ShipIds, Is.Empty);
            Assert.That(result.Optimality, Is.EqualTo(SolutionOptimality.Unproven));
        }

        [Test]
        public void ExhaustedMovableStatesCanProveADeadlockWithoutAnInitialHardLock()
        {
            var board = Board(6, 3, Ship("A", 0, 0, ShipDirection.Right), Ship("B", 5, 0, ShipDirection.Left));
            Assert.That(BoardDependencyAnalyzer.Analyze(board).HardLockedCycleCount, Is.Zero);
            Assert.That(BoardDependencyAnalyzer.Analyze(board).InitialMoveCount, Is.EqualTo(2));
            var result = LevelSolver.Solve(board);
            Assert.That(result.Status, Is.EqualTo(LevelSolverStatus.Deadlocked));
            Assert.That(result.Method, Is.EqualTo("BreadthFirstSearch"));
            Assert.That(result.VisitedStateCount, Is.GreaterThan(1));
        }

        [TestCase(1, 10000)]
        [TestCase(10000, 8)]
        public void ASearchBudgetLimitIsNotADeadlock(int states, int cells)
        {
            var result = LevelSolver.Solve(EscapableCycle(), new LevelSolverOptions(states, cells));
            Assert.That(result.Status, Is.EqualTo(LevelSolverStatus.LimitReached));
            Assert.That(result.ShipIds, Is.Empty);
            Assert.That(result.VisitedStateCount, Is.LessThanOrEqualTo(states));
            Assert.That(result.StoredFootprintCells, Is.LessThanOrEqualTo(cells));
        }

        [Test]
        public void DirectExitSolutionMeetsThePerShipLowerBoundWithoutStateSearch()
        {
            var board = Board(5, 4, Ship("A", 0, 1, ShipDirection.Right), Ship("B", 3, 0, ShipDirection.Up));
            var result = LevelSolver.Solve(board, new LevelSolverOptions(maxVisitedStates: 1));
            Assert.That(result.Status, Is.EqualTo(LevelSolverStatus.Solved));
            Assert.That(result.Method, Is.EqualTo("DirectExitLowerBound"));
            Assert.That(result.Optimality, Is.EqualTo(SolutionOptimality.Proven));
            CollectionAssert.AreEqual(new[] { "B", "A" }, result.ShipIds);
        }

        [Test]
        public void EmptyCurrentBoardHasAZeroStepOptimalSolution()
        {
            var board = Board(3, 3, Ship("A", 0, 0, ShipDirection.Up));
            board = board.ApplyPathResult(board.QueryForwardPath("A"));
            var result = LevelSolver.Solve(board);
            Assert.That(result.Status, Is.EqualTo(LevelSolverStatus.Solved));
            Assert.That(result.ShipIds, Is.Empty);
            Assert.That(result.Optimality, Is.EqualTo(SolutionOptimality.Proven));
        }

        [Test]
        public void InvalidRulesExitModeDataAndBudgetsAreNotReportedAsDeadlocks()
        {
            var board = EscapableCycle();
            Assert.That(LevelSolver.Solve((BoardModel)null).Status, Is.EqualTo(LevelSolverStatus.Invalid));
            Assert.That(LevelSolver.Solve(board, new LevelSolverOptions(rulesVersion: "OldStationaryRule")).Status, Is.EqualTo(LevelSolverStatus.Invalid));
            Assert.That(LevelSolver.Solve(board, new LevelSolverOptions(exitMode: "TopOnly")).Status, Is.EqualTo(LevelSolverStatus.Invalid));
            Assert.That(LevelSolver.Solve(board, new LevelSolverOptions(maxVisitedStates: 0)).Status, Is.EqualTo(LevelSolverStatus.Invalid));
            Assert.That(LevelSolver.Solve(new LevelData(), null, null).Status, Is.EqualTo(LevelSolverStatus.Invalid));
        }

        [Test]
        public void CanonicalStateKeyIsOrderIndependentButChangesWithPositionAndDirection()
        {
            var a = Ship("A;:1", 0, 1, ShipDirection.Right);
            var b = Ship("B;:2", 3, 0, ShipDirection.Up);
            var board = Board(5, 4, a, b);
            Assert.That(LevelStateIdentity.CanonicalKey(Board(5, 4, b, a)), Is.EqualTo(LevelStateIdentity.CanonicalKey(board)));
            Assert.That(LevelStateIdentity.CanonicalKey(board.ApplyPathResult(board.QueryForwardPath(a.Id))),
                Is.Not.EqualTo(LevelStateIdentity.CanonicalKey(board)));
            var turned = Board(5, 4, Ship("A;:1", 1, 1, ShipDirection.Left), b);
            Assert.That(turned.OccupiedCellCount, Is.EqualTo(board.OccupiedCellCount));
            Assert.That(LevelStateIdentity.CanonicalKey(turned), Is.Not.EqualTo(LevelStateIdentity.CanonicalKey(board)));
        }

        [TestCase("rules")]
        [TestCase("exit")]
        [TestCase("version")]
        [TestCase("level")]
        [TestCase("layout")]
        [TestCase("step")]
        [TestCase("incomplete")]
        public void ProofRejectsStaleOrTamperedContent(string changed)
        {
            var board = EscapableCycle();
            var original = LevelSolutionProof.Create("L", board, LevelSolver.Solve(board).ShipIds);
            var steps = original.Steps.ToArray();
            if (changed == "step")
                steps[0] = new LevelSolutionStep(steps[0].ShipId, steps[0].From, new GridPosition(99, 99),
                    steps[0].Outcome, steps[0].BeforeHash, steps[0].AfterHash);
            if (changed == "incomplete") steps = steps.Take(1).ToArray();
            var proof = new LevelSolutionProof(changed == "version" ? 0 : original.ProofVersion,
                changed == "rules" ? "OldRule" : original.RulesVersion, changed == "exit" ? "TopOnly" : original.ExitMode,
                changed == "level" ? "Other" : original.LevelId,
                changed == "layout" ? "stale" : original.LayoutFingerprint, steps);
            Assert.That(proof.Replay("L", board).IsValid, Is.False);
        }

        [Test]
        public void ProofJsonRoundTripsRepeatedIdsAndRejectsMalformedScalars()
        {
            var board = EscapableCycle();
            var proof = LevelSolutionProof.Create("L", board, LevelSolver.Solve(board).ShipIds);
            var json = LevelProofJson.Write(proof);
            Assert.That(LevelProofJson.Read(json).Replay("L", board).IsComplete, Is.True);
            Assert.Throws<LevelFormatException>(() => LevelProofJson.Read(json.Replace("\"proofVersion\": 1", "\"proofVersion\": \"1\"")));
            Assert.Throws<LevelFormatException>(() => LevelProofJson.Read(json.Replace("\"outcome\": \"Blocked\"", "\"outcome\": \"0\"")));
            Assert.Throws<LevelFormatException>(() => LevelProofJson.Read(json + "{}"));
        }

        internal static BoardModel EscapableCycle() => Board(6, 5,
            Ship("A", 0, 2, ShipDirection.Right), Ship("B", 4, 3, ShipDirection.Down),
            Ship("C", 5, 0, ShipDirection.Left), Ship("D", 1, 0, ShipDirection.Up));
        internal static BoardModel Board(int width, int height, params ShipPlacementData[] ships)
        {
            using (var session = ShipMovementSystemTests.CreateSession(width, height, ships)) return session.InitialBoard;
        }
        internal static ShipPlacementData Ship(string id, int x, int y, ShipDirection direction, int length = 2) =>
            new ShipPlacementData { Id = id, TypeId = "TF_BASE_SHIP", Length = length,
                Position = new GridPosition(x, y), Direction = direction };
    }
}
