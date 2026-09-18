using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;
using UnityEditor;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class DenseLevelSolutionTests
    {
        private const string SolutionPath = "Assets/Tidebound/Config/Levels/TestLevel_002.solution.json";

        [Test]
        public void DenseFixtureStartsWithEnoughDirectExitChoices()
        {
            using (var session = LevelConfigLoader.Load(LevelLoadingTests.DenseFixture()))
            {
                var directExitCount = session.InitialBoard.Ships.Count(ship =>
                    session.InitialBoard.QueryForwardPath(ship.Id).CanExit);

                Assert.That(directExitCount, Is.GreaterThanOrEqualTo(12));
            }
        }

        [Test]
        public void RecordedEightyShipSequenceClearsDenseFixtureWithoutBlockedMoves()
        {
            var solution = AssetDatabase.LoadAssetAtPath<TextAsset>(SolutionPath);
            Assert.That(solution, Is.Not.Null);
            var ids = JObject.Parse(solution.text)["shipIds"]?.Values<string>().ToArray();
            Assert.That(ids, Is.Not.Null);
            Assert.That(ids.Length, Is.EqualTo(80));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(80));

            using (var session = LevelConfigLoader.Load(LevelLoadingTests.DenseFixture()))
            {
                var board = session.InitialBoard;
                foreach (var shipId in ids)
                {
                    var path = board.QueryForwardPath(shipId);
                    Assert.That(path.CanExit, Is.True, shipId);
                    board = board.ApplyPathResult(path);
                }
                Assert.That(board.ShipCount, Is.Zero);
                Assert.That(board.OccupiedCellCount, Is.Zero);
            }
        }
    }
}
