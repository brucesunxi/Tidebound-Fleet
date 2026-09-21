using System;
using System.Collections;
using System.Reflection;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using UnityEngine;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private static IEnumerator CaptureHomeFrames(PortraitPuzzleGraybox game)
        {
            var catalog = ResultCatalog(game);
            foreach (var size in new[] { new Vector2Int(360, 640), new Vector2Int(390, 844), new Vector2Int(430, 932) })
            {
                SetGameViewSize(size); var deadline = Time.realtimeSinceStartup + 10;
                while ((Screen.width != size.x || Screen.height != size.y) && Time.realtimeSinceStartup < deadline) yield return null;
                if (Screen.width != size.x || Screen.height != size.y) throw new TimeoutException("Home viewport did not settle.");
                game.Initialize(catalog, saveService: new PlayerSaveService(new MemoryPlayerSaveStore()), campaign: true, useHomeNavigation: true);
                yield return null; yield return SaveFrame($"C1_Home_{size.x}x{size.y}.png");
                if (game.Session != null) throw new InvalidOperationException("Browsing home created an attempt.");
            }
            SetGameViewSize(new Vector2Int(390, 844)); yield return null; yield return null;
            game.Initialize(catalog, saveService: new PlayerSaveService(ResultStore(catalog, 2)), campaign: true, useHomeNavigation: true);
            game.ContinueFromHome(); game.ContinueFromResult(); yield return null;
            if (!game.IsHomeOpen || !game.IsCollectionOpen) throw new InvalidOperationException("First blue did not route through home.");
            yield return SaveFrame("C1_HomeCollection_390x844.png");
            game.CollectionView.SelectTransaction("FirstBlue"); game.CollectionView.ConfirmTransaction(); game.CloseCollection();
            game.ContinueFromHome(); game.ContinueFromResult(); game.ToggleMenu(); yield return null;
            if (game.LevelIndex != 2 || game.Session.Board.ShipCount != 80) throw new InvalidOperationException("Level 3 navigation failed.");
            yield return SaveFrame("C1_Level3Menu_390x844.png");
            game.Initialize(catalog, saveService: new PlayerSaveService(ResultStore(catalog, 10)), campaign: true, useHomeNavigation: true);
            game.SaveService.Collect(Guid.NewGuid().ToString("N"), "FirstBlue");
            game.ContinueFromHome(); game.ContinueFromResult(); yield return null;
            if (game.IsHomeOpen || !game.IsResultReadable) throw new InvalidOperationException("Last level did not retain its result.");
            yield return SaveFrame("C1_Level10Unavailable_390x844.png");
            AssistanceFixture(game);
            typeof(PortraitPuzzleGraybox).GetMethod("TickAssistance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, new object[] { 0f });
            game.enabled = false; yield return null;
            if (!game.IsDeadlockOpen || game.IsPaused) throw new InvalidOperationException("Deadlock notice is not passive.");
            yield return SaveFrame("C1_DeadlockNotice_390x844.png");
        }
    }
}
