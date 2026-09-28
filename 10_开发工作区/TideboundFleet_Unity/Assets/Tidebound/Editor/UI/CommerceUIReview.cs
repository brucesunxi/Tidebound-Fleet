using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.Collection;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using Tidebound.Unity.UI.VisualSamples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Tidebound.EditorTools
{
    [InitializeOnLoad]
    public static class CommerceUIReview
    {
        const string Key="Tidebound.Commerce.Review";static bool running;static double pollAt;
        static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        static string Work=>Path.Combine(Repo,"99_垃圾存储区/20260924_抽奖补给实施");
        static string Output=>Path.Combine(Repo,SessionState.GetBool(Key+"Hero",false)?"40_项目交接文档/验证记录/20260924_主按钮正式接入":SessionState.GetBool(Key+"Motion",false)?"40_项目交接文档/验证记录/20260924_按钮参考与首页摆动":SessionState.GetBool(Key+"Supplies",false)?"40_项目交接文档/验证记录/20260924_补给视觉修订":"40_项目交接文档/验证记录/20260924_抽奖视觉修订");
        static readonly List<string> checks=new List<string>();
        static CommerceUIReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        static void Status(string s){Directory.CreateDirectory(Work);File.WriteAllText(Path.Combine(Work,"capture_status.txt"),s);}
        static void Log(string s,string trace,LogType type){if((type==LogType.Error||type==LogType.Exception)&&SessionState.GetBool(Key,false)){Status("FAILED "+s);File.AppendAllText(Path.Combine(Work,"capture_errors.log"),s+"\n"+trace+"\n");SessionState.SetBool(Key,false);}}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(EditorApplication.timeSinceStartup>pollAt)
            {
                pollAt=EditorApplication.timeSinceStartup+1;var file=Path.Combine(Work,"capture_request.txt");
                if(File.Exists(file))
                {
                    var action=File.ReadAllText(file).Trim();File.Delete(file);
                    try
                    {
                        if(action=="reimport-backgrounds")
                        {
                            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop before reimport.");
                            foreach(var image in Directory.GetFiles("Assets/Tidebound/Resources/TideboundUI/Commerce","*.png"))AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceUpdate);
                            Status("BACKGROUNDS REIMPORTED");
                        }
                        else if(action=="capture"||action=="capture-supplies"||action=="capture-motion"||action=="capture-hero")
                        {
                            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop current run first.");
                            for(var i=0;i<EditorSceneManager.sceneCount;i++)if(EditorSceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene preserved.");
                            if(EditorSceneManager.GetActiveScene().path!=VisualSampleReview.ScenePath)SessionState.SetString(Key+"Scene",EditorSceneManager.GetActiveScene().path);
                            SessionState.SetBool(Key+"Hero",action=="capture-hero");SessionState.SetBool(Key+"Motion",action=="capture-motion");SessionState.SetBool(Key+"Supplies",action=="capture-supplies");EditorSceneManager.OpenScene(VisualSampleReview.ScenePath);SessionState.SetBool(Key,true);running=false;Status("STARTING");EditorApplication.EnterPlaymode();
                        }
                        else if(action=="stop"){SessionState.SetBool(Key,false);running=false;EditorApplication.ExitPlaymode();}
                        else if(action.StartsWith("restore")&&!EditorApplication.isPlaying){var path=action.StartsWith("restore:")?action.Substring(8):SessionState.GetString(Key+"Scene","");if(path!="")EditorSceneManager.OpenScene(path);Status("RESTORED USER SCENE");}
                    }
                    catch(Exception e){Status("FAILED "+e);}
                }
            }
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            EditorApplication.QueuePlayerLoopUpdate();EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"),false,null,false).Repaint();
            if(running)return;EditorApplication.isPaused=false;running=true;checks.Clear();var old=UnityEngine.Object.FindObjectOfType<VisualSampleGallery>();if(old)old.gameObject.SetActive(false);
            var game=new GameObject("CommerceReview_MemorySaveOnly").AddComponent<PortraitPuzzleGraybox>();game.StartCoroutine(Run(game));
        }
        static void Require(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        static IEnumerator Size(Vector2Int size)
        {
            typeof(PortraitGrayboxCapture).GetMethod("SetGameViewSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{size});
            var end=Time.realtimeSinceStartup+10;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<end)yield return null;
            Require(Screen.width==size.x&&Screen.height==size.y,"Game View "+size);yield return null;yield return null;
        }
        static IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(Output);yield return new WaitForEndOfFrame();var path=Path.Combine(Output,name+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);
            var end=Time.realtimeSinceStartup+5;while(!File.Exists(path)&&Time.realtimeSinceStartup<end)yield return null;Require(File.Exists(path),"Screenshot "+name);
        }
        static void Click(Button button)
        {
            Require(button&&button.IsInteractable(),"Interactable "+button?.name);
            var pos=RectTransformUtility.WorldToScreenPoint(null,((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
            var pointer=new PointerEventData(EventSystem.current){position=pos};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Raycast "+button.name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static Button ButtonIn(Component root,string name)=>root.GetComponentsInChildren<Button>(true).First(x=>x.name==name);
        static void HeroBounds(HarborHeroButton hero,string tag)
        {
            Canvas.ForceUpdateCanvases();
            foreach(var t in new[]{hero.Title,hero.Subtitle})
            {
                Require(t.rectTransform.rect.width+1>=t.preferredWidth,"Text fits width: "+tag+" "+t.text);
                var mesh=t.canvasRenderer.GetMesh();Require(mesh.vertexCount>0,"Text produces visible glyphs: "+tag+" "+t.text);
                var rootRect=((RectTransform)hero.transform).rect;
                Require(mesh.vertices.All(v=>{var pt=hero.transform.InverseTransformPoint(t.transform.TransformPoint(v));return pt.x>=rootRect.xMin-2&&pt.x<=rootRect.xMax+2&&pt.y>=rootRect.yMin-2&&pt.y<=rootRect.yMax+2;}),"Glyph fits button face: "+tag);
                UnityEngine.Object.Destroy(mesh);
            }
            var corners=new Vector3[4];hero.Decoration.GetWorldCorners(corners);
            Require(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),"Decoration in viewport: "+tag);
            Require(hero.GetComponentInChildren<HarborSliceGraphic>().Texture.width>1000,"New texture kept at native size: "+tag);
        }
        static IEnumerator ReviewHero(PortraitPuzzleGraybox game,PlayerSaveService service,PlayableLevelCatalog catalog)
        {
            foreach(var locale in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                UILanguage.SetPreference(locale,false);yield return Size(size);game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);game.SetReducedEntryMotion(false);yield return null;yield return null;
                var prefix=locale+"_"+size.x+"x"+size.y;
                var home=game.GetComponentsInChildren<HarborHeroButton>().First(x=>x.Style==HarborHeroButton.Theme.Voyage);
                HeroBounds(home,prefix+" Home");yield return Capture(prefix+"_Home");
                var button=home.GetComponent<Button>();var root=(RectTransform)home.transform;var original=root.position;var dimensions=root.rect.size;
                var start=Time.unscaledTime;var min=2f;var max=0f;var frame=0;
                while(Time.unscaledTime-start<3)
                {
                    yield return null;min=Mathf.Min(min,home.Visual.localScale.x);max=Mathf.Max(max,home.Visual.localScale.x);
                    if(locale==LanguagePreference.Chinese&&size.x==390&&Time.unscaledTime-start>frame*.15f&&frame<20)yield return Capture("HomeMotion_"+(frame++).ToString("D2"));
                }
                Require(min>=.999f&&max<=1.026f&&max-min>.022f,"Breathing stays within 1 to 1.025 "+prefix);
                Require(root.position==original&&root.rect.size==dimensions,"Breathing does not move click target "+prefix);
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.15f);
                Require(home.IsPressed&&home.Visual.localScale.x<1&&root.position==original,"Press changes visual only "+prefix);
                if(locale==LanguagePreference.Chinese&&size.x==390)yield return Capture("Home_Pressed");
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);yield return null;Require(!home.IsPressed,"Exit cancels press "+prefix);
                button.interactable=false;yield return new WaitForSecondsRealtime(.15f);Require(!home.IsPressed,"Disable clears press "+prefix);
                if(locale==LanguagePreference.Chinese&&size.x==390)yield return Capture("Home_Disabled");button.interactable=true;
                game.SetReducedEntryMotion(true);yield return null;yield return null;Require(home.Visual.localScale==Vector3.one,"Reduced motion stops breathing "+prefix);game.SetReducedEntryMotion(false);
                game.OpenCollection();var collection=game.GetComponentsInChildren<CollectionPanel>(true).First(x=>x.ApprovedGallery);var gallery=collection.ApprovedGallery;gallery.OpenDrawPopup();var draw=gallery.CommerceDraw;
                foreach(var pool in new[]{DrawPool.Skin,DrawPool.Trail,DrawPool.Showcase})
                {
                    draw.SelectPool(pool);yield return null;yield return null;var hero=draw.GetComponentInChildren<HarborHeroButton>();HeroBounds(hero,prefix+pool);
                    yield return Capture(prefix+"_Draw_"+pool);
                }
                var drawHero=draw.GetComponentInChildren<HarborHeroButton>();var drawButton=drawHero.GetComponent<Button>();var angle=drawHero.transform.Find("Face/ChestRays").localEulerAngles.z;var dp=drawHero.Decoration.anchoredPosition;var drawRect=(RectTransform)drawButton.transform;var position=drawRect.position;start=Time.unscaledTime;frame=0;
                while(Time.unscaledTime-start<3.2f)
                {
                    yield return null;
                    if(locale==LanguagePreference.Chinese&&size.x==390&&Time.unscaledTime-start>frame*.16f&&frame<20)yield return Capture("DrawMotion_"+(frame++).ToString("D2"));
                }
                Require(drawHero.Visual.localScale==Vector3.one&&drawRect.position==position,"Draw caption and root stay stable "+prefix);
                Require(Mathf.Abs(Mathf.DeltaAngle(angle,drawHero.transform.Find("Face/ChestRays").localEulerAngles.z))>30,"Chest light actually rotates "+prefix);
                game.SetReducedEntryMotion(true);yield return null;yield return null;var stopped=drawHero.Decoration.anchoredPosition;yield return new WaitForSecondsRealtime(.2f);Require(drawHero.Decoration.anchoredPosition==stopped,"Reduced motion stops chest bob "+prefix);game.SetReducedEntryMotion(false);
                if(locale==LanguagePreference.Chinese&&size.x==390)
                {
                    draw.SelectPool(DrawPool.Trail);yield return null;var before=service.Coins;Click(drawButton);yield return null;
                    Require(draw.Page=="result"&&service.Coins==before-300,"One click draws and charges once without confirmation");
                    draw.BeginDraw();Require(service.Coins==before-300,"Repeated callback cannot charge twice");
                    yield return Capture("Draw_Result");draw.ReturnToPool();drawHero=draw.GetComponentInChildren<HarborHeroButton>();drawButton=drawHero.GetComponent<Button>();drawButton.interactable=false;yield return null;yield return Capture("Draw_Disabled");drawButton.interactable=true;
                }
                draw.SelectPool(DrawPool.Scene);yield return null;Require(!drawHero.gameObject.activeSelf,"Closed scene pool does not display paid CTA");
                draw.gameObject.SetActive(false);gallery.Popup.gameObject.SetActive(true);game.CloseCollection();Require(service.Snapshot.Attempt==null,"Visual inspection never starts gameplay "+prefix);
            }
            // Exercise the real start/continue callbacks with a separate level-one memory store.
            var fresh=new PlayerSaveService(new MemoryPlayerSaveStore());game.Initialize(catalog,saveService:fresh,campaign:true,useHomeNavigation:true);yield return null;yield return null;
            var startButton=game.GetComponentsInChildren<HarborHeroButton>().First(x=>x.Style==HarborHeroButton.Theme.Voyage).GetComponent<Button>();Click(startButton);yield return null;
            Require(!game.IsHomeOpen&&game.Session!=null&&fresh.Snapshot.Attempt!=null,"New button starts real isolated attempt");var id=fresh.Snapshot.Attempt.AttemptId;game.ReturnHome();yield return null;yield return null;
            yield return Capture("Home_Continue");Click(startButton);yield return null;Require(fresh.Snapshot.Attempt.AttemptId==id,"Continue resumes same attempt");game.ReturnHome();
        }
        static IEnumerator ReviewMotion(PortraitPuzzleGraybox game,PlayerSaveService service,PlayableLevelCatalog catalog)
        {
            UILanguage.SetPreference(LanguagePreference.Chinese,false);
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                yield return Size(size);game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);yield return null;yield return null;
                var ship=game.GetComponentsInChildren<CollectionShipPreview>().First(x=>x.UsesIllustration);
                Require(Mathf.Approximately(ship.MotionSpeed,1.15f)&&Mathf.Approximately(ship.RockAmplitude,1.5f)&&Mathf.Approximately(ship.BobAmplitude,1.25f),"Home-only motion multipliers "+size);
                var art=(RectTransform)ship.transform.Find("ShipRender");var min=100f;var max=-100f;var start=Time.unscaledTime;var frame=0;
                var samples=new List<string>{"time,angle,bobY"};
                while(Time.unscaledTime-start<4.6f)
                {
                    yield return null;var angle=Mathf.DeltaAngle(0,art.localEulerAngles.z);min=Mathf.Min(min,angle);max=Mathf.Max(max,angle);
                    samples.Add((Time.unscaledTime-start).ToString("F4")+","+angle.ToString("F4")+","+art.offsetMin.y.ToString("F4"));
                    if(size.x==390&&Time.unscaledTime-start>frame*.25f&&frame<19){yield return Capture("HomeMotion_"+(frame++).ToString("D2"));}
                }
                Require(min < -1.15f&&max > 1.15f&&min>=-1.21f&&max<=1.21f,"Observed home swing amplitude "+size+" "+min+" / "+max);
                Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"motion_"+size.x+"x"+size.y+".csv"),samples);
                yield return Capture("Home_"+size.x+"x"+size.y);Require(service.Snapshot.Attempt==null,"Motion preview does not start gameplay");
            }
        }
        static IEnumerator ReviewSupplies(PortraitPuzzleGraybox game,PlayerSaveService service,PlayableLevelCatalog catalog)
        {
            foreach(var locale in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                UILanguage.SetPreference(locale,false);yield return Size(size);game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);yield return null;yield return null;
                game.OpenShop();yield return null;yield return null;var shop=game.ShopPanel;var prefix=locale+"_"+size.x+"x"+size.y;
                Require(shop&&shop.gameObject.activeInHierarchy,"Supply opens from real home "+prefix);
                Require(shop.GetComponentsInChildren<HarborBackdropGraphic>().Count()==4,"Four textured icon backgrounds "+prefix);
                foreach(var label in shop.GetComponentsInChildren<Text>().Where(t=>t.name=="ProductName"))
                {Require(label.fontStyle==FontStyle.Bold,"Bold product name "+label.text);Require(label.preferredWidth<=label.rectTransform.rect.width+1,"Product name fits "+label.text);}
                Require(shop.GetComponentsInChildren<HarborSliceGraphic>().Count(x=>x.name=="StockBadge")==3,"Three inventory badges "+prefix);
                Require(shop.GetComponentsInChildren<HarborSliceGraphic>().Count(x=>x.name=="PriceCoin")==4,"Four independent price coins "+prefix);
                yield return Capture(prefix+"_Supplies");
                if(locale==LanguagePreference.Chinese&&size.x==390)
                {
                    Click(ButtonIn(shop,"Select_reverse_1"));yield return null;yield return Capture(prefix+"_ConfirmSupply");var before=service.Coins;
                    Click(ButtonIn(shop,"ConfirmPurchase"));Require(service.Coins==before-200,"Existing purchase price retained");shop.ConfirmPurchase();Require(service.Coins==before-200,"Repeat confirmation cannot double charge");shop.ShowProducts();yield return null;
                }
                var scroll=shop.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=0;yield return null;yield return Capture(prefix+"_SuppliesBottom");
                Click(ButtonIn(shop,"Select_tools_bundle_1"));yield return null;Require(shop.SelectedProductId=="tools_bundle_1","Last product accessible "+prefix);shop.ShowProducts();yield return null;
                Click(ButtonIn(shop,"CashTab"));yield return null;yield return Capture(prefix+"_CoinPacks");
                Click(ButtonIn(shop,"CloseShop"));Require(!game.IsAcquisitionOpen,"Close returns to game "+prefix);Require(service.Snapshot.Attempt==null,"No gameplay started "+prefix);
            }
        }
        static IEnumerator Run(PortraitPuzzleGraybox game)
        {
            var data=new PlayerSaveData{CurrentLevel=21,HighestClearedLevel=20};
            data.Settlements=Enumerable.Range(1,20).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="REVIEW_L"+n,LevelNumber=n,Kind="Victory",Day="2026-09-24",EconomyVersion=BattleCoinRules.CurrentVersion,BattleCoins=n==1?7:80,FirstClearCoins=100}).ToArray();
            data.Coins=data.Settlements.Sum(x=>(long)x.BattleCoins+x.FirstClearCoins);data.Validate();var store=new MemoryPlayerSaveStore();store.Save(data);var service=new PlayerSaveService(store);service.CollectAppearance(Guid.NewGuid().ToString("N"),"FirstBlue");
            var folder=Phase5RTenLevelExport.Folder;var manifest=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/manifest.json");var files=Directory.GetFiles(folder,"*.json").ToDictionary(Path.GetFileName,p=>(Func<string>)(()=>File.ReadAllText(p)));var catalog=PlayableLevelCatalog.FromManifest(manifest.text,files);
            var language=UILanguage.Preference;
            if(SessionState.GetBool(Key+"Hero",false)){yield return ReviewHero(game,service,catalog);UILanguage.SetPreference(language,false);File.WriteAllLines(Path.Combine(Output,"runtime-checks.txt"),checks);Status("PASSED "+checks.Count+" hero checks. Memory store only.");SessionState.SetBool(Key,false);yield break;}
            if(SessionState.GetBool(Key+"Motion",false)){yield return ReviewMotion(game,service,catalog);UILanguage.SetPreference(language,false);File.WriteAllLines(Path.Combine(Output,"motion-checks.txt"),checks);Status("PASSED "+checks.Count+" motion checks. Memory store only.");SessionState.SetBool(Key,false);yield break;}
            if(SessionState.GetBool(Key+"Supplies",false)){yield return ReviewSupplies(game,service,catalog);UILanguage.SetPreference(language,false);File.WriteAllLines(Path.Combine(Output,"runtime-checks.txt"),checks);Status("PASSED "+checks.Count+" supply checks. Memory store only.");SessionState.SetBool(Key,false);yield break;}
            foreach(SkinRarity rarity in Enum.GetValues(typeof(SkinRarity)))Require(HarborCommerceUI.CommerceBackground(HarborCommerceUI.RewardBackground(DrawPool.Skin,rarity)).height>=1024,"Reward texture full resolution: "+rarity);
            foreach(var locale in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                UILanguage.SetPreference(locale,false);yield return Size(size);game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);yield return null;yield return null;
                game.OpenCollection();var collection=game.GetComponentsInChildren<CollectionPanel>(true).First(x=>x.ApprovedGallery);var gallery=collection.ApprovedGallery;gallery.OpenDrawPopup();var draw=gallery.CommerceDraw;yield return null;yield return null;
                foreach(var pool in new[]{DrawPool.Skin,DrawPool.Trail,DrawPool.Showcase,DrawPool.Scene})
                {
                    draw.SelectPool(pool);yield return null;yield return null;var prefix=locale+"_"+size.x+"x"+size.y;
                    yield return Capture(prefix+"_Draw_"+pool);
                    var hero=draw.GetComponentsInChildren<RawImage>().First(x=>pool==DrawPool.Scene?x.name=="Background":x.name.StartsWith("Artwork_"));Require(!hero.canvasRenderer.cull,"Hero art visible through scroll mask "+prefix+pool);
                    var primary=ButtonIn(draw,"DrawOnce");var corners=new Vector3[4];primary.GetComponent<RectTransform>().GetWorldCorners(corners);Require(corners.All(x=>x.x>=0&&x.x<=Screen.width&&x.y>=0&&x.y<=Screen.height),"CTA in viewport "+prefix+pool);
                    if(pool==DrawPool.Scene){Require(!primary.gameObject.activeSelf&&!primary.interactable,"Scene pool cannot charge");Require(draw.GetComponentsInChildren<Button>().Any(b=>b.name=="BrowseScenes"),"Scene collection link visible");}
                    var backdrop=draw.GetComponentInChildren<HarborBackdropGraphic>();Require(backdrop&&backdrop.texture&&!backdrop.canvasRenderer.cull,"Backdrop rendered "+prefix+pool);
                    Require(draw.GetComponentInChildren<ShowcaseSectionRule>()!=null,"Centered decorated heading "+pool);
                }
                draw.SelectPool(DrawPool.Trail);yield return null;Click(ButtonIn(draw,"Recharge"));yield return null;Require(game.ShopPanel.ActiveTab==1,"Wallet plus opens coin packs");
                yield return Capture(locale+"_"+size.x+"x"+size.y+"_CoinPacks");game.CloseAcquisition();yield return null;Require(draw.gameObject.activeInHierarchy&&draw.Pool==DrawPool.Trail,"Supply close returns to same draw pool");
                Click(ButtonIn(draw,"Recharge"));yield return null;Click(ButtonIn(game.ShopPanel,"ToolsTab"));yield return null;yield return Capture(locale+"_"+size.x+"x"+size.y+"_Supplies");game.CloseAcquisition();
                if(locale==LanguagePreference.Chinese&&size.x==390)
                {
                    draw.SelectPool(DrawPool.Trail);var balance=service.Coins;draw.BeginDraw();yield return null;Require(draw.Page=="result"&&service.Coins==balance-300,"Trail grant before result");draw.BeginDraw();Require(service.Coins==balance-300,"Repeated draw does not charge twice");yield return Capture("Chinese_390x844_TrailReward");Click(ButtonIn(draw,"UseReward"));Require(service.SelectedTrailId!=AppearanceCatalog.DefaultTrail,"Use granted trail saves selection");
                    draw.SelectPool(DrawPool.Showcase);draw.BeginDraw();yield return null;yield return Capture("Chinese_390x844_ShowcaseReward");Click(ButtonIn(draw,"UseReward"));Require(service.SelectedShowcaseId!=ShowcaseCatalog.DefaultId,"Use granted showcase saves selection");
                    draw.SelectPool(DrawPool.Skin);draw.BeginDraw();yield return null;yield return Capture("Chinese_390x844_SkinReward");
                    draw.ReturnToPool();Require(!draw.GetComponentsInChildren<Button>(true).Any(b=>b.name=="Rules"),"Rules entry removed");draw.ShowAll();yield return null;yield return Capture("Chinese_390x844_AllRewards");
                    var scroll=draw.GetComponentInChildren<ScrollRect>();var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,scroll.transform.position)};
                    ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.beginDragHandler);pointer.position+=Vector2.up*180;pointer.delta=Vector2.up*180;ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.dragHandler);ExecuteEvents.Execute(scroll.gameObject,pointer,ExecuteEvents.endDragHandler);scroll.StopMovement();Require(scroll.content.anchoredPosition.y>0,"Pool list responds to pointer drag");
                    scroll.verticalNormalizedPosition=0;yield return null;yield return Capture("Chinese_390x844_AllRewardsBottom");draw.ReturnToPool();
                    Click(ButtonIn(draw,"Recharge"));yield return null;game.ShopPanel.SelectProduct("reverse_1");yield return null;yield return Capture("Chinese_390x844_ConfirmSupply");balance=service.Coins;game.ShopPanel.ConfirmPurchase();Require(service.Coins==balance-200,"Supply debits real memory wallet");game.ShopPanel.ConfirmPurchase();Require(service.Coins==balance-200,"Supply callback idempotent");game.CloseAcquisition();
                }
                gallery.CommerceDraw.gameObject.SetActive(false);gallery.Popup.gameObject.SetActive(true);gallery.SelectTab(1);gallery.TryTrail(3);yield return null;
                Require(!gallery.TrailVoyage.enabled&&gallery.TrailVoyage.Boats.All(b=>!b.gameObject.activeSelf)&&gallery.TrailVoyage.Ribbons.All(r=>!r.gameObject.activeSelf),"No trail animation in collection dialogs");
                Require(!gallery.TrailPause.gameObject.activeSelf&&!gallery.TrailRestart.gameObject.activeSelf,"No playback controls in dialogs");
                if(locale==LanguagePreference.Chinese&&size.x==390)yield return Capture("Chinese_390x844_CollectionStaticTrail");
                gallery.CloseTrailPreview();game.CloseCollection();Require(service.Snapshot.Attempt==null,"UI browsing never starts gameplay");
            }
            UILanguage.SetPreference(language,false);File.WriteAllLines(Path.Combine(Output,"runtime-checks.txt"),checks);Status("PASSED "+checks.Count+" runtime checks. Memory store only.");SessionState.SetBool(Key,false);
        }
    }
}
