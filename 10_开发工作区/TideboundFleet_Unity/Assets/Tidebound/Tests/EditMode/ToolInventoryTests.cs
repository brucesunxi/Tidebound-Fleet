using System;
using System.IO;
using NUnit.Framework;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using Tidebound.Ship;
using Tidebound.Core;

namespace Tidebound.Tests
{
    public sealed class ToolInventoryTests
    {
        private static int Total(ToolInventory inventory) => inventory.Count(ShipTool.Rescue)+inventory.Count(ShipTool.Shuffle)+inventory.Count(ShipTool.Reverse);
        [Test]
        public void MilestonesAreSparseAndPersistAcrossRestartAndReload()
        {
            var store=new MemoryToolInventoryStore();var inventory=new ToolInventory(store);
            inventory.ReachLevel(2);Assert.That(Total(inventory),Is.Zero);
            inventory.ReachLevel(3);Assert.That(Total(inventory),Is.EqualTo(3));inventory.TrySpend(ShipTool.Rescue);
            for(var n=0;n<3;n++){inventory=new ToolInventory(store);inventory.ReachLevel(3);Assert.That(inventory.Count(ShipTool.Rescue),Is.Zero);}
            inventory.ReachLevel(9);Assert.That(Total(inventory),Is.EqualTo(2));inventory.ReachLevel(10);Assert.That(Total(inventory),Is.EqualTo(3));
            inventory=new ToolInventory(store);inventory.ReachLevel(10);Assert.That(Total(inventory),Is.EqualTo(3));
            inventory.ReachLevel(20);Assert.That(Total(inventory),Is.EqualTo(4));inventory.ReachLevel(2);Assert.That(Total(inventory),Is.EqualTo(4));
        }
        [Test]
        public void ConfirmedRewardReceiptIsIdempotentAcrossReload()
        {
            var store=new MemoryToolInventoryStore();var stock=new ToolInventory(store);
            Assert.That(stock.Grant("confirmed-ad:1",2,0,0),Is.True);stock=new ToolInventory(store);
            Assert.That(stock.Grant("confirmed-ad:1",2,0,0),Is.False);Assert.That(stock.Count(ShipTool.Rescue),Is.EqualTo(2));
            Assert.That(stock.Grant("verified-order:1",1,1,1),Is.True);Assert.That(Total(stock),Is.EqualTo(5));
        }
        private sealed class FailingStore : IToolInventoryStore
        {
            public bool FailRead,FailWrite;
            private ToolInventoryData saved;
            public ToolInventoryData Load() {if(FailRead)throw new IOException();return saved?.Copy();}
            public void Save(ToolInventoryData data) {if(FailWrite)throw new IOException();saved=data.Copy();}
        }
        [Test]
        public void FailedSaveCannotSpendGrantOrApplyToolAndCanRetry()
        {
            var store=new FailingStore();var stock=new ToolInventory(store);stock.ReachLevel(3);store.FailWrite=true;
            Assert.That(stock.TrySpend(ShipTool.Rescue),Is.False);Assert.That(stock.Grant("reward",1,1,1),Is.False);Assert.That(Total(stock),Is.EqualTo(3));
            using(var s=ShipMovementSystemTests.CreateSession(4,4,LevelSolverTests.Ship("A",0,0,ShipDirection.Up)))
            {var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,stock))
             {var board=s.Board;Assert.That(t.Rescue(),Is.EqualTo(ToolUseStatus.StorageUnavailable));Assert.That(s.Board,Is.SameAs(board));
              t.Select(ShipTool.Reverse);Assert.That(t.UseSelected("A"),Is.EqualTo(ToolUseStatus.StorageUnavailable));Assert.That(s.Board,Is.SameAs(board));}}
            store.FailWrite=false;Assert.That(stock.Grant("reward",1,1,1),Is.True);Assert.That(stock.TrySpend(ShipTool.Rescue),Is.True);
        }
        [Test]
        public void UnreadableStockDoesNotRegiftOrBlockCorePlay()
        {
            var stock=new ToolInventory(new FailingStore{FailRead=true});stock.ReachLevel(20);Assert.That(stock.IsAvailable,Is.False);Assert.That(Total(stock),Is.Zero);
            using(var s=ShipMovementSystemTests.CreateSession(4,4,LevelSolverTests.Ship("A",0,0,ShipDirection.Up)))
            {var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,stock))
             {Assert.That(t.Rescue(),Is.EqualTo(ToolUseStatus.StorageUnavailable));Assert.That(m.TryBeginMove("A").IsAccepted,Is.True);}}
        }
        [Test]
        public void FileInventoryRoundTripsAndCorruptionDoesNotOverwriteTheOriginal()
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundInventoryTest_"+Guid.NewGuid().ToString("N"));var path=Path.Combine(folder,"stock.json");
            try
            {
                var inventory=new ToolInventory(new ToolInventoryFileStore(path));inventory.ReachLevel(3);inventory.TrySpend(ShipTool.Rescue);
                inventory=new ToolInventory(new ToolInventoryFileStore(path));inventory.ReachLevel(3);Assert.That(inventory.Count(ShipTool.Rescue),Is.Zero);
                Assert.That(File.Exists(path+".bak"),Is.True);Assert.That(File.Exists(path+".tmp"),Is.False);
                File.WriteAllText(path,"corrupt");inventory=new ToolInventory(new ToolInventoryFileStore(path));inventory.ReachLevel(30);
                Assert.That(inventory.IsAvailable,Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo("corrupt"));
            }
            finally {if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
    }
}
