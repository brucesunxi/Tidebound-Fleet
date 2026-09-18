using System.Collections;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Lane;
using Tidebound.Ship;
using Tidebound.Unity.Lane;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class LaneTransitViewPlayModeTests
    {
        [UnityTest]
        public IEnumerator RealViewFollowsLaneAndHidesAfterFleetEntry()
        {
            using (var session = CreateSession())
            using (var transit = new TransitSystem(session))
            {
                var gameObject = new GameObject("LaneShip_A");
                try
                {
                    var view = gameObject.AddComponent<ShipLaneView>();
                    view.ConfigureShipId("A");
                    using (var controller = new LaneTransitController(
                        transit, new StraightPathProvider(), new[] { view }))
                    {
                        var movement = new ShipMovementSystem(session);
                        movement.StartPlaying();
                        var request = movement.TryBeginMove("A");
                        movement.CompleteTravel(request.Operation.OperationId);

                        controller.Advance(0.60d);
                        Assert.That(gameObject.transform.position.x, Is.EqualTo(5f).Within(0.001f));
                        Assert.That(gameObject.transform.localScale.x, Is.EqualTo(0.45f).Within(0.001f));

                        controller.Advance(0.75d);
                        Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InFleet));
                        Assert.That(gameObject.activeSelf, Is.False);
                    }
                }
                finally
                {
                    Object.Destroy(gameObject);
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealViewStaysFixedWhileSessionIsPaused()
        {
            using (var session = CreateSession())
            using (var transit = new TransitSystem(session))
            {
                var gameObject = new GameObject("LaneShip_A");
                try
                {
                    var view = gameObject.AddComponent<ShipLaneView>();
                    view.ConfigureShipId("A");
                    using (var controller = new LaneTransitController(
                        transit, new StraightPathProvider(), new[] { view }))
                    {
                        var movement = new ShipMovementSystem(session);
                        movement.StartPlaying();
                        var request = movement.TryBeginMove("A");
                        movement.CompleteTravel(request.Operation.OperationId);
                        controller.Advance(0.25d);
                        var position = gameObject.transform.position;

                        movement.Pause();
                        controller.Advance(5d);
                        Assert.That(gameObject.transform.position, Is.EqualTo(position));
                        movement.Resume();
                        controller.Advance(0.25d);
                        Assert.That(gameObject.transform.position, Is.Not.EqualTo(position));
                    }
                }
                finally
                {
                    Object.Destroy(gameObject);
                }
            }
            yield return null;
        }

        private sealed class StraightPathProvider : ILanePathProvider
        {
            public Vector3 FleetIngressPosition => new Vector3(10f, 0f, 2f);
            public LaneWorldPath CreatePath(LaneRoute route, Vector3 startPosition) =>
                new LaneWorldPath(startPosition, new Vector3(10f, 0f, 0f));
        }

        private static GameSession CreateSession()
        {
            var level = new LevelData
            {
                SchemaVersion = 1,
                LevelId = "LaneViewPlayMode",
                Width = 5,
                Height = 4,
                BossId = "TF_KRAKEN_01",
                Ships = new[]
                {
                    new ShipPlacementData
                    {
                        Id = "A",
                        TypeId = "TF_SPEEDBOAT",
                        Position = new GridPosition(0, 2),
                        Direction = ShipDirection.Right
                    }
                }
            };
            return LevelSessionFactory.Create(
                level,
                new[] { new ShipDefinition("TF_SPEEDBOAT", 2, 10) },
                new[] { new BossDefinition("TF_KRAKEN_01") });
        }
    }
}
