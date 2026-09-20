using System;
using System.Linq;
using NUnit.Framework;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.LevelDesign;

namespace Tidebound.Tests
{
    public sealed class ToolBudgetTests
    {
        private static SavedGameRuntime Game()=>new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,LevelSolverTests.Ship("A",1,1,ShipDirection.Up)),3);
        [Test]
        public void SharedBudgetIsSavedWithEffectAndCannotBeRefilledByBuyingOrReloading()
        {
            var store=new MemoryPlayerSaveStore();store.Save(CoinShopTests.Earned(2));var save=new PlayerSaveService(store);
            using(var game=Game())
            {
                save.Start(game);save.Inventory.Grant("extra",4,4,4);
                using(var tools=new ShipToolSystem(game.Session,game.Movement,save.Inventory,seed:1))
                {
                    tools.Select(ShipTool.Reverse);Assert.That(tools.UseSelected("A"),Is.EqualTo(ToolUseStatus.Applied));
                    Assert.That(tools.Shuffle(),Is.EqualTo(ToolUseStatus.Applied));
                    tools.Select(ShipTool.Reverse);Assert.That(tools.UseSelected("A"),Is.EqualTo(ToolUseStatus.Applied));
                    for(var i=0;i<2;i++){tools.Select(ShipTool.Reverse);Assert.That(tools.UseSelected("A"),Is.EqualTo(ToolUseStatus.Applied));}
                    Assert.That(tools.UsesLeft,Is.Zero);var count=tools.Remaining(ShipTool.Rescue);
                    Assert.That(tools.Rescue(),Is.EqualTo(ToolUseStatus.UseLimitReached));Assert.That(tools.Remaining(ShipTool.Rescue),Is.EqualTo(count));
                    Assert.That(save.Snapshot.Attempt.ToolUses,Is.EqualTo(5));
                    Assert.That(save.BuyWithCoins(Guid.NewGuid().ToString("N"),"reverse_1",Tidebound.Unity.LevelDesign.CoinShopCatalogReader.LoadDefault()),Is.EqualTo(CoinPurchaseStatus.Purchased));
                    Assert.That(tools.Select(ShipTool.Reverse),Is.EqualTo(ToolUseStatus.UseLimitReached));
                }
                var loaded=new PlayerSaveService(store);
                using(var restored=SavedGameRuntime.Restore(loaded.Snapshot.Attempt))
                {
                    loaded.AttachRestored(restored);
                    using(var tools=new ShipToolSystem(restored.Session,restored.Movement,loaded.Inventory))Assert.That(tools.UsesLeft,Is.Zero);
                    var stock=loaded.Inventory.Count(ShipTool.Reverse);
                    using(var next=Game()){Assert.That(loaded.Restart(next),Is.True);Assert.That(next.Session.ToolUses,Is.Zero);Assert.That(loaded.Inventory.Count(ShipTool.Reverse),Is.EqualTo(stock));}
                }
            }
        }
        [Test]
        public void CancelInvalidTargetAndFailedShuffleDoNotConsumeBudget()
        {
            using(var game=Game())
            {
                var stock=new ToolInventory();stock.Grant("test",1,1,1);
                using(var tools=new ShipToolSystem(game.Session,game.Movement,stock,shuffleAttempts:0))
                {
                    tools.Select(ShipTool.Reverse);Assert.That(tools.UseSelected("missing"),Is.EqualTo(ToolUseStatus.InvalidTarget));tools.CancelSelection();
                    Assert.That(tools.Shuffle(),Is.EqualTo(ToolUseStatus.Unproven));Assert.That(game.Session.ToolUses,Is.Zero);Assert.That(tools.UsesLeft,Is.EqualTo(5));
                }
            }
        }
    }
}
