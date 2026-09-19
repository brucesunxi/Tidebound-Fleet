using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class AlgorithmPrototypeAssetTests
    {
        private const string Folder = "Assets/Tidebound/Config/LevelPrototypes/Phase5R_Rebuild/";

        [TestCase("P5R_Rebuild_001", 7)]
        [TestCase("P5R_Rebuild_002", 80)]
        public void SavedV2SampleAndVersionedProofClearThroughMovementAndTransitModels(string id, int count)
        {
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + id + ".json");
            var sidecar = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + id + ".solution.json");
            Assert.That(layout, Is.Not.Null);
            Assert.That(sidecar, Is.Not.Null);
            var level = LevelJsonReader.Read(layout.text);
            var proof = LevelProofJson.Read(sidecar.text);
            var metadata = JObject.Parse(sidecar.text);
            Assert.That(level.SchemaVersion, Is.EqualTo(2));
            Assert.That(level.Ships.Length, Is.EqualTo(count));
            Assert.That((string)metadata["status"], Is.EqualTo("AlgorithmPrototypeOnly"));
            Assert.That((string)metadata["generatorVersion"], Is.EqualTo(ReverseLevelGenerator.Version));
            Assert.That((string)metadata["solverVersion"], Is.EqualTo(LevelSolver.Version));
            using (var session = ReverseLevelGeneratorTests.Create(level))
            using (var transit = new TransitSystem(session))
            {
                Assert.That(proof.Replay(id, session.InitialBoard).IsComplete, Is.True);
                var construction = LevelSolutionProof.Create(id, session.InitialBoard,
                    metadata["constructionOrder"].Values<string>());
                Assert.That(construction.Replay(id, session.InitialBoard).IsComplete, Is.True);
                var result = LevelSolver.Solve(session.InitialBoard);
                Assert.That(result.Status, Is.EqualTo(LevelSolverStatus.Solved));
                Assert.That(result.ShipIds.Count, Is.EqualTo(count));
                var exited = new List<string>();
                var entered = new List<string>();
                session.Events.Subscribe<ShipExitBoardEvent>(e => exited.Add(e.Ship.ShipId));
                session.Events.Subscribe<ShipEnterFleetEvent>(e => entered.Add(e.Ship.ShipId));
                var movement = new ShipMovementSystem(session);
                movement.StartPlaying();
                foreach (var shipId in result.ShipIds)
                {
                    var request = movement.TryBeginMove(shipId);
                    Assert.That(request.IsAccepted, Is.True);
                    Assert.That(request.Operation.WillExit, Is.True);
                    Assert.That(movement.CompleteTravel(request.Operation.OperationId), Is.EqualTo(ShipMoveAdvanceStatus.Applied));
                }
                transit.Advance(100);
                Assert.That(session.Board.ShipCount, Is.Zero);
                Assert.That(transit.IsEmpty, Is.True);
                Assert.That(session.Ships.All(x => x.State == ShipState.InFleet), Is.True);
                Assert.That(exited.Count, Is.EqualTo(count));
                Assert.That(exited.Distinct().Count(), Is.EqualTo(count));
                CollectionAssert.AreEqual(exited, entered);
            }
        }
    }
}
