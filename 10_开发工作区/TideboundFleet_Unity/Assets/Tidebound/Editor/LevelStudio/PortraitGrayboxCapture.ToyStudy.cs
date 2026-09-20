using System;
using System.Collections;
using System.IO;
using System.Linq;
using Tidebound.Unity.LevelDesign;
using UnityEngine;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private static IEnumerator CaptureToyStudyFrames(PortraitPuzzleGraybox game)
        {
            game.EnableReviewMode();game.SetAssistancePreferences(false,true);
            foreach(var size in Sizes.Take(3))
            {
                SetGameViewSize(size);var end=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<end)yield return null;
                if(Screen.width!=size.x || Screen.height!=size.y)throw new TimeoutException("Study viewport did not settle.");
                game.SelectLevel(2);yield return null;game.TogglePause();
                var resources=game.GetComponentInChildren<ShipPrototypeResources>();resources.SetToyStudy(true);
                yield return SaveResultFrame(game,$"B1_Level3_{size.x}x{size.y}.png");
                if(size.x==390){resources.SetToyStudy(false);yield return SaveResultFrame(game,"B1_V1Comparison_390x844.png");}
            }
            SetGameViewSize(new Vector2Int(390,844));yield return null;yield return null;
            game.SelectLevel(9);yield return null;game.TogglePause();
            game.GetComponentInChildren<ShipPrototypeResources>().SetToyStudy(true);
            yield return SaveResultFrame(game,"B1_Level10_LongShips_390x844.png");
            SaveToySheet(false);SaveToySheet(true);
        }
        // Separate review camera. Never rotates the gameplay camera or loads the player's account.
        private static void SaveToySheet(bool oblique)
        {
            var root=new GameObject("B1_StudySheet");var resources=root.AddComponent<ShipPrototypeResources>();resources.Initialize();resources.SetToyStudy(true);
            var cameraObject=new GameObject("B1_StudyCamera");var camera=cameraObject.AddComponent<Camera>();
            var texture=new RenderTexture(1200,oblique?600:900,24);Texture2D readback=null;
            var previous=RenderTexture.active;
            try
            {
                camera.orthographic=true;camera.orthographicSize=oblique?2.6f:4.9f;camera.aspect=oblique?2f:4f/3f;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.70f,.86f,.87f);
                camera.cullingMask=1<<29;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.targetTexture=texture;
                camera.transform.position=oblique?new Vector3(0,6,-9):new Vector3(0,0,-12);
                camera.transform.LookAt(new Vector3(0,0,-.25f),Vector3.up);
                for(var row=0;row<3;row++)for(var col=0;col<(oblique?1:4);col++)
                {
                    var sample=new GameObject(row==0?"Default":row==1?"BlueCandidate":"FixedLong");sample.transform.SetParent(root.transform,false);
                    sample.transform.position=oblique?new Vector3((row-1)*3.3f,0,0):new Vector3((col-1.5f)*3.1f,(1-row)*3.1f,0);
                    sample.transform.rotation=Quaternion.Euler(0,0,oblique?-28:col*90);
                    var length=row==2?3:2;
                    resources.AddHull(sample.transform,length,row==1?1:0).gameObject.layer=29;
                    resources.AddShadow(sample.transform,length).gameObject.layer=29;
                }
                camera.Render();RenderTexture.active=texture;
                readback=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
                readback.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);readback.Apply();
                Directory.CreateDirectory(OutputDirectory);File.WriteAllBytes(Path.Combine(OutputDirectory,oblique?"B1_ObliqueStudy.png":"B1_FourDirections.png"),readback.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;camera.enabled=false;camera.targetTexture=null;root.SetActive(false);texture.Release();
                UnityEngine.Object.Destroy(readback);UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(root);
            }
        }
    }
}
