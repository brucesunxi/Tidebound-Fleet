using System;
using System.Collections;
using NUnit.Framework;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class ShipMovementViewPlayModeTests
    {
        [UnityTest]
        public IEnumerator TravelReachesTargetAndCompletesExactlyOnce()
        {
            var gameObject = new GameObject("ShipMovementView_Test");
            try
            {
                var view = gameObject.AddComponent<ShipMovementView>();
                view.ConfigureShipId("A");
                var calls = 0;
                var target = new Vector3(2f, 0f, 3f);
                view.PlayTravel(target, 0.05f, () => calls++);

                yield return WaitUntil(() => calls == 1);
                Assert.That(gameObject.transform.position, Is.EqualTo(target));
                Assert.That(calls, Is.EqualTo(1));
                yield return null;
                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator PauseFreezesTravelAndResumeContinuesFromTheSameProgress()
        {
            var gameObject = new GameObject("ShipMovementView_PauseTest");
            try
            {
                var view = gameObject.AddComponent<ShipMovementView>();
                view.ConfigureShipId("A");
                var completed = false;
                view.PlayTravel(new Vector3(4f, 0f, 0f), 0.15f, () => completed = true);
                yield return null;
                view.SetPaused(true);
                var frozen = gameObject.transform.position;
                yield return null;
                yield return null;
                yield return null;
                Assert.That(gameObject.transform.position, Is.EqualTo(frozen));
                Assert.That(completed, Is.False);

                view.SetPaused(false);
                yield return WaitUntil(() => completed);
                Assert.That(gameObject.transform.position, Is.EqualTo(new Vector3(4f, 0f, 0f)));
            }
            finally
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator BlockedFeedbackUsesOnlyLateralOffsetAndReturnsToAnchor()
        {
            var gameObject = new GameObject("ShipMovementView_FeedbackTest");
            try
            {
                var view = gameObject.AddComponent<ShipMovementView>();
                view.ConfigureShipId("A");
                var anchor = new Vector3(1f, 0f, 2f);
                gameObject.transform.position = anchor;
                var completed = false;
                var sawLateralMovement = false;
                view.PlayBlockedFeedback(Vector3.right * 0.2f, 0.10f, () => completed = true);
                while (!completed)
                {
                    var position = gameObject.transform.position;
                    sawLateralMovement |= Mathf.Abs(position.x - anchor.x) > 0.001f;
                    Assert.That(position.y, Is.EqualTo(anchor.y).Within(0.0001f));
                    Assert.That(position.z, Is.EqualTo(anchor.z).Within(0.0001f));
                    yield return null;
                }

                Assert.That(sawLateralMovement, Is.True);
                Assert.That(gameObject.transform.position, Is.EqualTo(anchor));
            }
            finally
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        private static IEnumerator WaitUntil(Func<bool> predicate)
        {
            var deadline = Time.realtimeSinceStartup + 2f;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, "Animation did not complete before the test timeout.");
        }
    }
}
