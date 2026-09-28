using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Boss;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
 public sealed class ExitComboPlayModeTests
 {
  [UnityTest] public IEnumerator RealExitsDriveFlamesPauseSnapshotsAndExpiryWithoutChangingDamage()
  {
   var root=new GameObject("ComboLive_Test");var game=root.AddComponent<PortraitPuzzleGraybox>();
   var source=new PlayableLevelCatalog(new[]{"Combo"},_=>true,id=>new LevelData{SchemaVersion=2,LevelId=id,BossId="TF_KRAKEN_01",Width=16,Height=8,
    Ships=Enumerable.Range(0,14).Select(i=>new ShipPlacementData{Id="S"+i,TypeId="TF_BASE_SHIP",Position=new GridPosition(i,0),Direction=ShipDirection.Up,Length=2}).ToArray()});
   try
   {
    game.Initialize(source,new Tidebound.Unity.Ship.ShipMovementTiming(1000,.005f,.01f,.01f,.06f),new Tidebound.Lane.LaneTransitTiming(.4,.01,.01),useGameplayArt:true);
    yield return null;var combo=game.GetComponentInChildren<ComboBattleView>();
    Assert.That(combo.State.Count,Is.Zero);Assert.That(Resources.Load<Shader>("TideboundUI/ComboFlame").isSupported,Is.True);
    for(var i=0;i<12;i++)
    {game.ClickShip("S"+i);var end=Time.realtimeSinceStartup+3;while(game.IsBusy&&Time.realtimeSinceStartup<end)yield return null;yield return null;Assert.That(combo.State.Count,Is.EqualTo(i+1));Assert.That(combo.transform.Find("ComboBadge").gameObject.activeSelf,Is.EqualTo(i>=2));}
    var badge=(RectTransform)combo.transform.Find("ComboBadge");var flame=(RectTransform)combo.transform.Find("ComboFlame");
    Assert.That(flame.rect.height,Is.GreaterThan(badge.rect.height*1.8f),"Flame tips extend above the badge");
    Assert.That(combo.GetComponentsInChildren<Text>().Single(t=>t.name=="ComboCount").fontSize,Is.GreaterThanOrEqualTo(13));
    Assert.That(combo.transform.Find("RotatingComboStar").gameObject.activeSelf,Is.True);
    Assert.That(combo.transform.Find("SwingingComboAnchor").gameObject.activeSelf,Is.True);
    game.TogglePause();var clock=combo.AnimationTime;var left=combo.State.Remaining;var hp=game.Session.Boss.Hp;
    yield return new WaitForSecondsRealtime(.15f);Assert.That(combo.AnimationTime,Is.EqualTo(clock));Assert.That(combo.State.Remaining,Is.EqualTo(left));Assert.That(game.Session.Boss.Hp,Is.EqualTo(hp));
    game.TogglePause();yield return new WaitForSecondsRealtime(5.5f);
    Assert.That(combo.State.Count,Is.Zero);Assert.That(combo.State.TierFor("S11"),Is.EqualTo(3));
    Assert.That(game.Session.Boss.Hp,Is.EqualTo(20));Assert.That(combo.transform.Find("ComboBadge").gameObject.activeSelf,Is.False);
    Assert.That(combo.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),Is.True);
    game.Restart();yield return null;Assert.That(game.GetComponentInChildren<ComboBattleView>().State.Count,Is.Zero);
   }
   finally{Object.Destroy(root);}yield return null;
  }
 }
}
