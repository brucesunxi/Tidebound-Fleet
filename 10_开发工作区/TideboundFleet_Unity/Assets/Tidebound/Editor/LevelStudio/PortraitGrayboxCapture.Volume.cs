using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Tidebound.Events;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private static IEnumerator CaptureVolumeFrames(PortraitPuzzleGraybox game)
        {
            game.EnableReviewMode();game.SetAssistancePreferences(true,true);yield return null;
            foreach(var size in Sizes.Take(3))
            {
                SetGameViewSize(size);var end=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<end)yield return null;
                if(Screen.width!=size.x || Screen.height!=size.y)throw new TimeoutException("Volume viewport did not settle.");
                game.SelectLevel(1);yield return null;yield return SaveResultFrame(game,$"Volume_Level2_{size.x}x{size.y}.png");
            }
            SetGameViewSize(new Vector2Int(390,844));yield return null;yield return null;
            game.SelectLevel(0);yield return null;yield return SaveResultFrame(game,"Volume_Level1_390x844.png");
            game.SelectLevel(9);yield return null;yield return SaveResultFrame(game,"Volume_LongShips_390x844.png");
            game.SetShipPrototypeMode(false);yield return SaveResultFrame(game,"Volume_FlatReference_390x844.png");game.SetShipPrototypeMode(true);
            game.NotifyUserActivity();
            typeof(PortraitPuzzleGraybox).GetMethod("TickAssistance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{0f});
            typeof(PortraitPuzzleGraybox).GetMethod("TickAssistance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{5.01f});
            yield return SaveResultFrame(game,"Volume_DirectExitHint_390x844.png");
            foreach(ShipDirection direction in Enum.GetValues(typeof(ShipDirection)))
            {
                game.SelectLevel(1);yield return null;
                var ship=game.Session.Board.Ships.First(s=>s.Direction==direction && game.Session.Board.QueryForwardPath(s.Id).CanExit);
                using(game.Session.Events.Subscribe<ShipExitBoardEvent>(e=>{if(e.Ship.ShipId==ship.Id)game.TogglePause();}))
                {
                    game.ClickShip(ship.Id);var end=Time.realtimeSinceStartup+8;
                    while(!game.IsPaused && Time.realtimeSinceStartup<end)yield return null;
                    if(!game.IsPaused)throw new TimeoutException("Volume lane handoff did not arrive.");
                }
                yield return SaveResultFrame(game,$"Volume_Lane_{direction}_390x844.png");
            }
            // An explicit visual sample sheet, not an account/equipment mutation.
            game.SelectLevel(9);yield return null;game.TogglePause();
            foreach(var v in game.GetComponentsInChildren<ShipPrototypeAppearance>())v.transform.parent.gameObject.SetActive(false);
            var resources=game.GetComponentInChildren<ShipPrototypeResources>();
            var gallery=new GameObject("VisualSamples");gallery.transform.SetParent(resources.transform,false);
            for(var row=0;row<6;row++)for(var col=0;col<4;col++)
            {
                var sample=new GameObject("Sample");sample.layer=30;sample.transform.SetParent(gallery.transform,false);
                sample.transform.position=new Vector3(2+col*3.3f,16-row*2.8f,0);sample.transform.rotation=Quaternion.Euler(0,0,col*90);
                var hull=resources.AddHull(sample.transform,row==5 ? 3 : 2,row%5);var shadow=resources.AddShadow(sample.transform,row==5 ? 3 : 2);
                hull.gameObject.layer=shadow.gameObject.layer=30;
            }
            yield return SaveResultFrame(game,"Volume_PaletteAndDirections_390x844.png");
            UnityEngine.Object.Destroy(gallery);
        }
    }
}
