using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tidebound.Tests
{
    public sealed class LevelLoadingTests
    {
        internal const string FixturePath = "Assets/Tidebound/Config/Levels/TestLevel_001.asset";
        internal static LevelConfigSO Fixture()
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(FixturePath);
            Assert.That(config, Is.Not.Null, "The committed SO fixture must import with all references intact.");
            return config;
        }

        [Test]
        public void CommittedFixtureLoadsSevenLv1BoatsAndSeventyHp()
        {
            using (var session = LevelConfigLoader.Load(Fixture()))
            {
                Assert.That(session.LevelId, Is.EqualTo("TestLevel_001"));
                Assert.That(session.Width, Is.EqualTo(4)); Assert.That(session.Height, Is.EqualTo(6));
                Assert.That(session.Ships.Count, Is.EqualTo(7));
                Assert.That(session.Ships.Select(x => x.Id).Distinct().Count(), Is.EqualTo(7));
                foreach (var ship in session.Ships)
                {
                    Assert.That(ship.TypeId, Is.EqualTo("TF_SPEEDBOAT"));
                    Assert.That(ship.Length, Is.EqualTo(2)); Assert.That(ship.Damage, Is.EqualTo(10));
                    Assert.That(ship.State, Is.EqualTo(ShipState.Idle));
                }
                Assert.That(session.Boss.BossId, Is.EqualTo("TF_KRAKEN_01"));
                Assert.That(session.Boss.InitialHp, Is.EqualTo(70)); Assert.That(session.Boss.Hp, Is.EqualTo(70));
                Assert.That(session.State, Is.EqualTo(GameState.Prepare));
                Assert.That(session.InitialBoard.OccupiedCellCount, Is.EqualTo(14));
                Assert.That(session.InitialBoard.GetShipId(new GridPosition(3, 4)), Is.EqualTo("S005"));
                Assert.That(session.InitialBoard.GetShipId(new GridPosition(0, 5)), Is.Null);
            }
        }

        [Test]
        public void RuntimeEditsCannotLeakIntoAnotherLoadOrAuthoredAssets()
        {
            var config = Fixture(); var jsonBefore = config.LevelJson.text;
            var dirtyBefore = EditorUtility.IsDirty(config);
            using (var a = LevelConfigLoader.Load(config))
            using (var b = LevelConfigLoader.Load(config))
            {
                Assert.That(a.SessionId, Is.Not.EqualTo(b.SessionId));
                Assert.That(a.Ships[0], Is.Not.SameAs(b.Ships[0]));
                Assert.That(a.Boss, Is.Not.SameAs(b.Boss));
                a.Ships[0].Position = new GridPosition(2, 3); a.Ships[0].Direction = ShipDirection.Down;
                a.Ships[0].State = ShipState.InFleet; a.Boss.Hp = 17; a.State = GameState.Paused;
                Assert.That(b.Ships[0].Position, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(b.Ships[0].Direction, Is.EqualTo(ShipDirection.Right));
                Assert.That(b.Ships[0].State, Is.EqualTo(ShipState.Idle));
                Assert.That(b.Boss.Hp, Is.EqualTo(70)); Assert.That(b.State, Is.EqualTo(GameState.Prepare));
                Assert.That(a.Boss.InitialHp, Is.EqualTo(70));
                Assert.That(config.GetShipConfigs()[0].DamageLv1, Is.EqualTo(10));
                Assert.That(config.LevelJson.text, Is.EqualTo(jsonBefore));
                Assert.That(EditorUtility.IsDirty(config), Is.EqualTo(dirtyBefore));
            }
        }

        [Test]
        public void ParsedDocumentIsCopiedAndShipStateChangesNeverRecomputeBossHp()
        {
            var config = Fixture(); var level = LevelJsonReader.Read(config.LevelJson.text);
            var definitions = config.GetShipConfigs().Select(x => x.CreateDefinition()).ToArray();
            var bosses = config.GetBossConfigs().Select(x => x.CreateDefinition()).ToArray();
            using (var a = LevelSessionFactory.Create(level, definitions, bosses))
            using (var b = LevelSessionFactory.Create(level, definitions, bosses))
            {
                level.Ships[0].Position = new GridPosition(99, 99); level.Ships[0].Id = "changed";
                level.Ships = new ShipPlacementData[0]; level.Width = 99;
                foreach (var ship in a.Ships) ship.State = ShipState.InFleet;
                Assert.That(a.Ships[0].Id, Is.EqualTo("S001")); Assert.That(a.Ships.Count, Is.EqualTo(7));
                Assert.That(a.Ships[0].Position, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(a.Width, Is.EqualTo(4)); Assert.That(a.Boss.Hp, Is.EqualTo(70));
                Assert.That(b.Ships[0].State, Is.EqualTo(ShipState.Idle));
            }
        }

        [Test]
        public void PresentationScaleAndPrefabAreNotLogicalInputs()
        {
            var original = Fixture(); var level = Object.Instantiate(original);
            var ship = Object.Instantiate(original.GetShipConfigs()[0]);
            var visual = Object.Instantiate(ship.Visual);
            try
            {
                var visualFields = new SerializedObject(visual);
                visualFields.FindProperty("localScale").vector3Value = new Vector3(1000, 0.01f, 500);
                visualFields.ApplyModifiedPropertiesWithoutUndo();
                var shipFields = new SerializedObject(ship);
                shipFields.FindProperty("visual").objectReferenceValue = visual; shipFields.ApplyModifiedPropertiesWithoutUndo();
                var levelFields = new SerializedObject(level);
                levelFields.FindProperty("shipConfigs").GetArrayElementAtIndex(0).objectReferenceValue = ship;
                levelFields.ApplyModifiedPropertiesWithoutUndo();
                using (var session = LevelConfigLoader.Load(level))
                {
                    Assert.That(session.InitialBoard.OccupiedCellCount, Is.EqualTo(14));
                    Assert.That(session.Ships.All(x => x.Length == 2), Is.True);
                    Assert.That(session.Boss.Hp, Is.EqualTo(70));
                }
            }
            finally { Object.DestroyImmediate(level); Object.DestroyImmediate(ship); Object.DestroyImmediate(visual); }
        }

        [Test]
        public void ConfigChangesOnlyAffectSubsequentLoads()
        {
            var original = Fixture(); var level = Object.Instantiate(original);
            var ship = Object.Instantiate(original.GetShipConfigs()[0]);
            try
            {
                var levelFields = new SerializedObject(level);
                levelFields.FindProperty("shipConfigs").GetArrayElementAtIndex(0).objectReferenceValue = ship;
                levelFields.ApplyModifiedPropertiesWithoutUndo();
                using (var before = LevelConfigLoader.Load(level))
                {
                    var shipFields = new SerializedObject(ship); shipFields.FindProperty("damageLv1").intValue = 20;
                    shipFields.ApplyModifiedPropertiesWithoutUndo();
                    using (var after = LevelConfigLoader.Load(level))
                    {
                        Assert.That(before.Boss.InitialHp, Is.EqualTo(70)); Assert.That(before.Boss.Hp, Is.EqualTo(70));
                        Assert.That(before.Ships[0].Damage, Is.EqualTo(10)); Assert.That(after.Boss.Hp, Is.EqualTo(140));
                    }
                }
            }
            finally { Object.DestroyImmediate(level); Object.DestroyImmediate(ship); }
        }

        [TestCase("levelJson")]
        [TestCase("shipConfigs")]
        [TestCase("bossConfigs")]
        public void MissingUnityReferencesFailClearly(string field)
        {
            var copy = Object.Instantiate(Fixture());
            try
            {
                var serialized = new SerializedObject(copy); var property = serialized.FindProperty(field);
                if (property.isArray) property.GetArrayElementAtIndex(0).objectReferenceValue = null;
                else property.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (field == "levelJson") Assert.Throws<LevelFormatException>(() => LevelConfigLoader.Load(copy));
                else Assert.Throws<LevelValidationException>(() => LevelConfigLoader.Load(copy));
            }
            finally { Object.DestroyImmediate(copy); }
        }
    }
}
