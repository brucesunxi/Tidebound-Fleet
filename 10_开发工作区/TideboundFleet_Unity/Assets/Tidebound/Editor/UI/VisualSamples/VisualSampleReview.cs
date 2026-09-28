using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.Unity.UI.VisualSamples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    /// <summary>Scoped authoring and real Game View capture for the 2026-09-23 visual samples only.</summary>
    [InitializeOnLoad]
    public static partial class VisualSampleReview
    {
        public const string ScenePath="Assets/Tidebound/Scenes/VisualSamples/UI_VisualSamples_20260923.unity";
        private const string ArtPath="Assets/Tidebound/Resources/TideboundUI/VisualSamples";
        private const string PrefabPath="Assets/Tidebound/Resources/TideboundUI/VisualSamples/Prefabs";
        private const string Key="Tidebound.VisualSamples.20260923";
        private static string Repo => Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Work => Path.Combine(Repo,"99_垃圾存储区/20260923_Unity视觉小样");
        private static string Output => Path.Combine(Repo,"40_项目交接文档/验证记录/"+(SessionState.GetBool(Key+".Trails",false)?"20260923_拖尾动态小样":SessionState.GetBool(Key+".Skins",false)?"20260923_船体皮肤小样":SessionState.GetBool(Key+".Draw",false)?"20260923_统一抽奖入口":SessionState.GetBool(Key+".Scenes",false)?"20260923_场景弹窗小样":"20260923_形象获取分区"));
        private static double pollAt,deadline;
        private static EditorWindow gameView;
        private static bool capturing;
        private static readonly List<string> checks=new List<string>();
        static VisualSampleReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=OnLog;}
        private static void OnLog(string message,string trace,LogType type)
        {if((type==LogType.Error||type==LogType.Exception)&&trace.Contains("VisualSample")){Directory.CreateDirectory(Work);File.AppendAllText(Path.Combine(Work,"sample_errors.log"),message+"\n"+trace+"\n");}}
        private static void Status(string text){Directory.CreateDirectory(Work);File.WriteAllText(Path.Combine(Work,"editor_status.txt"),text);}
        private static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(EditorApplication.timeSinceStartup>pollAt)
            {
                pollAt=EditorApplication.timeSinceStartup+1;
                var path=Path.Combine(Work,"request.txt");
                if(File.Exists(path))
                {
                    var request=File.ReadAllText(path).Trim();File.Delete(path);
                    try
                    {
                        if(request.StartsWith("capture")){SessionState.SetBool(Key+".Skins",request=="capture-skins");SessionState.SetBool(Key+".Trails",request=="capture-trails");}
                        if(request=="build")Build();
                        else if(request=="capture-trails"){SessionState.SetBool(Key+".Draw",false);SessionState.SetBool(Key+".Scenes",false);SessionState.SetBool(Key+".Groups",false);Capture();}
                        else if(request=="capture-skins"){SessionState.SetBool(Key+".Draw",false);SessionState.SetBool(Key+".Scenes",false);SessionState.SetBool(Key+".Groups",false);Capture();}
                        else if(request=="capture"){SessionState.SetBool(Key+".Groups",false);Capture();}
                        else if(request=="capture-groups"){SessionState.SetBool(Key+".Groups",true);Capture();}
                        else if(request=="capture-scenes"){SessionState.SetBool(Key+".Scenes",true);SessionState.SetBool(Key+".Groups",false);Capture();}
                        else if(request=="capture-draw"){SessionState.SetBool(Key+".Draw",true);SessionState.SetBool(Key+".Scenes",false);SessionState.SetBool(Key+".Groups",false);Capture();}
                        else if(request=="test-edit"||request=="test-play")
                        {
                            var runner=Type.GetType("Tidebound.Tests.ShowcaseReviewRunner, Tidebound.Tests.EditMode",true);
                            runner.GetMethod("Run",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{request.Substring(5)});
                        }
                        else if(request=="view-a"||request=="view-b")
                        {var g=UnityEngine.Object.FindObjectOfType<VisualSampleGallery>();if(g)g.ShowPopup(request=="view-b");}
                        else if(request=="inspect-input")
                        {
                            var g=UnityEngine.Object.FindObjectOfType<VisualSampleGallery>();var es=EventSystem.current;var hits=new List<RaycastResult>();
                            if(es)es.RaycastAll(new PointerEventData(es){position=Input.mousePosition},hits);
                            Status("Playing="+EditorApplication.isPlaying+" Focused="+Application.isFocused+" Mouse="+Input.mousePosition+" Input="+(es?es.currentInputModule?.name:"none")+" Hits="+string.Join(",",hits.Select(x=>x.gameObject.name))+" Popup="+(g&&g.PopupOpen));
                        }
                        else if(request=="focus")SetSize(new Vector2Int(390,844));
                        else if(request=="stop"&&UnityEngine.Object.FindObjectOfType<VisualSampleGallery>()){SessionState.SetBool(Key,false);capturing=false;deadline=0;EditorApplication.ExitPlaymode();Status("STOPPED");}
                    }
                    catch(Exception e){Status("FAILED: "+e);Debug.LogException(e);}
                }
            }
            if(!SessionState.GetBool(Key,false))return;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
            if(EditorApplication.timeSinceStartup>deadline){SessionState.SetBool(Key,false);Status("FAILED: capture timeout");return;}
            EditorApplication.QueuePlayerLoopUpdate();gameView?.Repaint();
            if(!EditorApplication.isPlaying||capturing)return;
            var gallery=UnityEngine.Object.FindObjectOfType<VisualSampleGallery>();if(!gallery)return;
            capturing=true;gallery.StartCoroutine(CaptureFrames(gallery));
        }
        [MenuItem("Tools/Tidebound/Visual Samples/Build and Open Samples")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop the preview before rebuilding.");
            for(var i=0;i<EditorSceneManager.sceneCount;i++)if(EditorSceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("An open scene has unsaved changes; preserved without opening another scene.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));Directory.CreateDirectory(PrefabPath);Directory.CreateDirectory(Output);
            foreach(var path in Directory.GetFiles(ArtPath,"*.png"))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            }
            var materialPath=ArtPath+"/Silhouette.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(materialPath))AssetDatabase.CreateAsset(new Material(AssetDatabase.LoadAssetAtPath<Shader>(ArtPath+"/SampleSilhouette.shader")),materialPath);
            foreach(var path in Directory.GetFiles(ArtPath+"/Scenes","*.png"))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            }
            foreach(var path in Directory.GetFiles(ArtPath+"/ShipSkins","*.png"))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
            }
            foreach(var path in Directory.GetFiles(ArtPath+"/Trails","*.png"))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
            }
            var sceneMaterial=ArtPath+"/Scenes/SceneLocked.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(sceneMaterial))AssetDatabase.CreateAsset(new Material(AssetDatabase.LoadAssetAtPath<Shader>(ArtPath+"/Scenes/SceneLocked.shader")),sceneMaterial);
            const string showcasePath="Assets/Tidebound/Resources/TideboundUI/Showcase";
            // Only this supplied batch: preserve alpha and original dimensions without changing old imports.
            for(var number=5;number<=25;number++)
            {
                var path=showcasePath+"/Showcase_H"+number.ToString("D2")+"_v1.png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            var lockedPath=showcasePath+"/ShowcaseLocked.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(lockedPath))AssetDatabase.CreateAsset(new Material(AssetDatabase.LoadAssetAtPath<Shader>(showcasePath+"/ShowcaseLocked.shader")),lockedPath);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("PreviewCamera",typeof(Camera)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.42f,.58f);camera.orthographic=true;camera.tag="MainCamera";
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var root=new GameObject("VisualSamples_OnlyPreviewData");var gallery=root.AddComponent<VisualSampleGallery>();gallery.Build(File.ReadAllText(ArtPath+"/asset_manifest.json"));
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath+"/UI_VisualSampleGallery.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.Main.gameObject,PrefabPath+"/UI_SampleMainButton.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.Popup.gameObject,PrefabPath+"/UI_SampleCollectionLayout.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.Cards[0].Root.gameObject,PrefabPath+"/UI_SampleCollectionCard.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.TrailCards[0].Root.gameObject,PrefabPath+"/UI_SampleTrailCard.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.TrailViewer.gameObject,PrefabPath+"/UI_SampleTrailMotion.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.SkinCards[0].Root.gameObject,PrefabPath+"/UI_SampleSkinCard.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.SceneCards[0].Root.gameObject,PrefabPath+"/UI_SampleSceneCard.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.SceneDraw.gameObject,PrefabPath+"/UI_SampleDrawEntry.prefab");
            PrefabUtility.SaveAsPrefabAsset(gallery.DrawPopup.gameObject,PrefabPath+"/UI_SampleDrawPopup.prefab");
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();Selection.activeGameObject=root;
            SetSize(new Vector2Int(390,844));Status("BUILT: "+ScenePath);
            File.WriteAllText(Path.Combine(Work,"editor_diagnostics.txt"),"Log: "+Application.consoleLogPath+"\nShader: "+string.Join("\n",ShaderUtil.GetShaderMessages(AssetDatabase.LoadAssetAtPath<Shader>(ArtPath+"/SampleSilhouette.shader")).Select(x=>x.message)));
        }
        [MenuItem("Tools/Tidebound/Visual Samples/Capture Three Viewports")]
        public static void Capture()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop the preview before starting a fresh capture.");
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)
            {
                for(var i=0;i<EditorSceneManager.sceneCount;i++)if(EditorSceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Open scene changes preserved.");
                EditorSceneManager.OpenScene(ScenePath);
            }
            Directory.CreateDirectory(Output);checks.Clear();capturing=false;deadline=0;SessionState.SetBool(Key,true);SetSize(new Vector2Int(390,844));Status("CAPTURING");EditorApplication.EnterPlaymode();
        }
        private static IEnumerator CaptureFrames(VisualSampleGallery g)
        {
            if(SessionState.GetBool(Key+".Trails",false)){yield return CaptureTrailFrames(g);yield break;}
            if(SessionState.GetBool(Key+".Skins",false)){yield return CaptureSkinFrames(g);yield break;}
            if(SessionState.GetBool(Key+".Draw",false)){yield return CaptureDrawFrames(g);yield break;}
            if(SessionState.GetBool(Key+".Scenes",false)){yield return CaptureSceneFrames(g);yield break;}
            if(SessionState.GetBool(Key+".Groups",false)){yield return CaptureGroupFrames(g);yield break;}
            yield return null;yield return null;
            g.SetPreviewProgress(0);g.SetArtworkInspection(false);
            Require(g.Cards.Length==25&&Enumerable.Range(0,g.Cards.Length).Count(g.IsPreviewOwned)==1,"Real roster contains 25 unique ships; new player owns exactly one");
            Require(g.Cards.Select(c=>c.Ship.texture).Distinct().Count()==25,"Every card uses distinct imported artwork");
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                SetSize(size);var until=Time.realtimeSinceStartup+12;
                while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
                Require(Screen.width==size.x&&Screen.height==size.y,"Exact Game View "+size);
                g.ApplyLayout();
                foreach(var english in new[]{false,true})
                {
                    g.SetEnglish(english);g.ShowPopup(false);yield return Settle();
                    yield return Save("A_"+(english?"EN":"CN")+"_"+size.x+"x"+size.y+"_GameView.png");
                    g.ShowPopup(true);g.Scroll.verticalNormalizedPosition=1;yield return Settle();
                    Require(Tidebound.Collection.ShowcaseCatalog.Groups[1].CatalogIndices.Take(4).Select(i=>g.Cards[i].Root.anchoredPosition.y).Distinct().Count()==1,"Four columns "+size+" "+english);
                    foreach(var t in new[]{g.MainLabel,g.LevelLabel,g.EntryLabel,g.PopupTitle,g.Statistics,g.Cards[0].Name,g.Cards[0].Status})
                        Require(t.preferredHeight<=t.rectTransform.rect.height+1,"Text height "+t.name+" "+size+" "+english);
                    Require(g.Cards.All(c=>c.Name.preferredHeight<=c.Name.rectTransform.rect.height+1),"All 25 real ship names fit at "+size+" "+english);
                    yield return Save("B_"+(english?"EN":"CN")+"_"+size.x+"x"+size.y+"_GameView.png");
                }
                if(size.y==640)
                {
                    g.SetEnglish(false);var title=g.Header.position;var tabs=g.Tabs.position;
                    g.Scroll.verticalNormalizedPosition=0;yield return Settle();
                    Require(title==g.Header.position&&tabs==g.Tabs.position,"Header and tabs remain fixed at list bottom");
                    var cardCorners=new Vector3[4];var viewportCorners=new Vector3[4];g.Cards[Tidebound.Collection.ShowcaseCatalog.Groups.Last().CatalogIndices.Last()].Root.GetWorldCorners(cardCorners);g.Viewport.GetWorldCorners(viewportCorners);
                    Require(cardCorners[0].y>=viewportCorners[0].y-1,"Final row clears viewport bottom and fixed tabs");
                    yield return Save("B_CN_360x640_Scrolled_GameView.png");
                }
            }
            SetSize(new Vector2Int(390,844));yield return Settle();g.ApplyLayout();g.SetEnglish(false);g.ShowPopup(false);yield return Settle();
            var mainBounds=g.Main.rect;var mainPosition=g.Main.position;
            RequireRaycast(g.MainButton,"Primary CTA center receives pointer input");
            RequireRaycast(g.EntryButton,"Collection entry center receives pointer input");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(g.MainButton.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return Settle();
            Require(g.Main.position==mainPosition&&g.Main.rect==mainBounds,"Press keeps hit area and layout stable");
            yield return Save("A_CN_390x844_Pressed_GameView.png");
            ExecuteEvents.Execute(g.MainButton.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(g.MainButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.PreviewClicks==1,"CTA preview callback only");
            g.RefreshText();g.MainButton.interactable=false;yield return Settle();ExecuteEvents.Execute(g.MainButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.PreviewClicks==1,"Disabled CTA ignores click");
            yield return Save("A_CN_390x844_Disabled_GameView.png");g.MainButton.interactable=true;
            ExecuteEvents.Execute(g.EntryButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.PopupOpen,"Collection entry opens preview popup");
            g.Scroll.verticalNormalizedPosition=1;yield return Settle();
            RequireRaycast(g.CloseButton,"Close center receives pointer input above modal blocker");
            RequireRaycast(g.Cards[1].Button,"Locked card receives acquisition hint input");
            ExecuteEvents.Execute(g.Cards[1].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.SelectedCard==0,"Fresh player cannot select a milestone ship");
            Require(g.Cards.Skip(1).All(c=>c.Ship.material==Tidebound.Unity.UI.ShowcaseArt.LockedMaterial),"All 24 unowned cards use alpha-based locked silhouettes");
            g.SetPreviewProgress(10);
            var drag=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=new Vector2(180,380),pressPosition=new Vector2(180,380)};
            var dragRoot=ExecuteEvents.GetEventHandler<IDragHandler>(g.Cards[4].Button.gameObject);
            Require(dragRoot==g.Viewport.gameObject,"Card drag routes to the independent ScrollRect");
            ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.beginDragHandler);
            var contentBefore=g.Content.anchoredPosition;var headerBefore=g.Header.position;drag.position+=Vector2.up*130;drag.delta=Vector2.up*130;
            ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.dragHandler);ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.endDragHandler);g.Scroll.StopMovement();
            Require(g.Content.anchoredPosition!=contentBefore&&g.Header.position==headerBefore,"EventSystem drag moves list while header stays fixed");
            g.Scroll.verticalNormalizedPosition=1;yield return Settle();
            ExecuteEvents.Execute(g.Cards[1].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.SelectedCard==1&&!g.Cards[0].Check.gameObject.activeSelf&&g.Cards[1].Check.gameObject.activeSelf,"Owned card selection updates all states");
            ExecuteEvents.Execute(g.Cards[6].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.SelectedCard==1,"Locked card does not select");
            g.SelectCard(3);var badgeCorners=new Vector3[4];var listCorners=new Vector3[4];g.Cards[3].Check.GetWorldCorners(badgeCorners);g.Viewport.GetWorldCorners(listCorners);
            Require(badgeCorners[2].x<=listCorners[2].x+.1f,"Using badge stays inside list clipping");
            var raycast=new PointerEventData(EventSystem.current){position=new Vector2(10,Screen.height-20)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(raycast,hits);Require(hits.Count>0&&hits[0].gameObject.name=="InputBlocker","Modal backdrop blocks underlying input");
            ExecuteEvents.Execute(g.CategoryButtons[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.SelectedTab==0&&g.SkinContent.gameObject.activeSelf&&!g.EmptyCategory.gameObject.activeSelf,"Tab state and scoped content stay synchronized");
            ExecuteEvents.Execute(g.CategoryButtons[2].gameObject,pointer,ExecuteEvents.pointerClickHandler);g.SelectCard(0);yield return Settle();
            yield return Save("B_CN_390x844_Level10Preview_ThreeStates_GameView.png");
            ExecuteEvents.Execute(g.CloseButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(!g.PopupOpen,"Close returns to sample A");
            g.SetPreviewProgress(0);g.ShowPopup(true);g.SetArtworkInspection(true);
            for(var page=0;page<3;page++)
            {g.Scroll.verticalNormalizedPosition=1-page*.5f;yield return Settle();yield return Save("Roster_ArtworkInspection_"+(page+1)+"_390x844_GameView.png");}
            Require(Enumerable.Range(0,g.Cards.Length).Count(g.IsPreviewOwned)==1,"Artwork inspection never grants ownership");
            g.SetArtworkInspection(false);g.Scroll.verticalNormalizedPosition=1;yield return Settle();
            yield return CaptureComparisons(g);
            File.WriteAllText(Path.Combine(Output,"interaction_checks.txt"),string.Join("\n",checks));
            SessionState.SetBool(Key,false);capturing=false;Status("COMPLETE: "+checks.Count+" focused checks; real Game View screenshots. Left in sample B for visual review.");
        }
        private static IEnumerator Settle(){Canvas.ForceUpdateCanvases();yield return null;yield return null;yield return null;Canvas.ForceUpdateCanvases();}
        private static void RequireRaycast(Button button,string description)
        {
            var rect=(RectTransform)button.transform;var p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,description);
        }
        private static void Require(bool pass,string description)
        {checks.Add((pass?"PASS ":"FAIL ")+description);if(!pass){File.WriteAllText(Path.Combine(Output,"interaction_checks.txt"),string.Join("\n",checks));Status("FAILED: "+description);SessionState.SetBool(Key,false);throw new InvalidOperationException(description);}}
        private static IEnumerator Save(string filename)
        {
            yield return new WaitForEndOfFrame();var path=Path.Combine(Output,filename);if(File.Exists(path))File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);var until=Time.realtimeSinceStartup+10;while(!File.Exists(path)&&Time.realtimeSinceStartup<until)yield return null;
            Require(File.Exists(path),"Game View capture "+filename);
        }
        private static void SetSize(Vector2Int size)
        {
            var assembly=typeof(Editor).Assembly;var type=assembly.GetType("UnityEditor.GameViewSizes",true);
            var instance=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);var group=type.GetProperty("currentGroup").GetValue(instance);
            var gt=group.GetType();var count=(int)gt.GetMethod("GetTotalCount").Invoke(group,null);var selected=-1;
            for(var i=0;i<count;i++){var item=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});var t=item.GetType();if((int)t.GetProperty("width").GetValue(item)==size.x&&(int)t.GetProperty("height").GetValue(item)==size.y&&t.GetProperty("sizeType").GetValue(item).ToString()=="FixedResolution"){selected=i;break;}}
            if(selected<0){var mode=Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType",true),"FixedResolution");var item=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize",true),mode,size.x,size.y,"Tidebound Visual Sample");gt.GetMethod("AddCustomSize").Invoke(group,new[]{item});selected=count;}
            var vt=assembly.GetType("UnityEditor.GameView",true);gameView=EditorWindow.GetWindow(vt);gameView.Show();gameView.Focus();
            vt.GetProperty("selectedSizeIndex").SetValue(gameView,0);vt.GetMethod("SizeSelectionCallback").Invoke(gameView,new object[]{selected,null});
            vt.BaseType.GetProperty("targetSize",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gameView,(Vector2)size);
            vt.BaseType.GetMethod("SetFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gameView,new object[]{true});gameView.Repaint();
        }
    }
}
