using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    /// <summary>Deterministically builds the reviewed common UI prefab library.</summary>
    public static class HarborUIPrefabBuilder
    {
        public const string Folder="Assets/Tidebound/Resources/TideboundUI/Prefabs";

        [MenuItem("Tidebound/UI/Rebuild Design System Prefabs")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/Tidebound/Resources/TideboundUI");
            EnsureFolder(Folder);
            Save(BuildCommonButton(),"UI_Button_Common");
            Save(BuildMainButton(),"UI_Button_Main");
            Save(BuildResourceHud(),"UI_HUD_Resource");
            Save(BuildShipDisplay(),"UI_Ship_Display");
            Save(BuildMainMenu(),"UI_MainMenu");
            Save(BuildWidget("UI_Tab",HarborSurfaceKind.Tab),"UI_Tab");
            Save(BuildWidget("UI_ItemCard",HarborSurfaceKind.Card),"UI_ItemCard");
            var popup=Root("UI_Popup");var panel=popup.AddComponent<HarborImage>();
            panel.Kind=HarborSurfaceKind.Panel;panel.color=HarborDesignTokens.PearlWarm;panel.Radius=26;panel.Depth=6;
            Save(popup,"UI_Popup");
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }

        private static GameObject BuildWidget(string name,HarborSurfaceKind kind)
        {
            var root=Root(name);var surface=root.AddComponent<HarborImage>();surface.Kind=kind;
            surface.color=HarborDesignTokens.PearlWarm;surface.Radius=kind==HarborSurfaceKind.Card?19:14;
            var button=root.AddComponent<Button>();button.targetGraphic=surface;
            var colors=button.colors;colors.highlightedColor=new Color(.94f,.99f,1);colors.pressedColor=new Color(.83f,.94f,1);colors.disabledColor=new Color(.74f,.80f,.83f,.72f);colors.fadeDuration=.08f;button.colors=colors;
            var face=Child("Face",root.transform);Fill(face);
            var label=Text("Label",face,"",15,FontStyle.Normal);Fill(label.rectTransform,8);
            root.AddComponent<HarborControlState>();return root;
        }

        private static GameObject BuildCommonButton()
        {
            var root=Root("UI_Button_Common");
            var relief=root.AddComponent<HarborReliefImage>();relief.Radius=HarborDesignTokens.RadiusMedium;relief.Depth=HarborDesignTokens.ShadowDepth;
            relief.FaceTexture=Skin("UI_HomeEntry_Pearl_v2");
            relief.SliceUV=new Vector4(.26f,.36f,.26f,.26f);relief.SliceSize=new Vector4(19,29,19,19);
            var button=root.AddComponent<Button>();button.targetGraphic=relief;button.transition=Selectable.Transition.None;
            var face=Child("Face",root.transform);Fill(face);
            var icon=Child("IconSlot",face);icon.gameObject.AddComponent<RawImage>().raycastTarget=false;
            Place(icon,new Rect(7,31,70,60));
            var label=Text("Label",face,"",12,FontStyle.Normal);Place(label.rectTransform,new Rect(5,12,74,18));label.resizeTextForBestFit=true;label.resizeTextMinSize=10;label.resizeTextMaxSize=12;
            var status=Text("Status",face,"",10,FontStyle.Normal);Place(status.rectTransform,new Rect(18,22,48,12));status.color=HarborDesignTokens.Ink;status.gameObject.SetActive(false);
            var motion=root.AddComponent<HarborButtonRelief>();motion.Content=face;motion.FloatingIcon=icon;
            return root;
        }

        private static GameObject BuildMainButton()
        {
            var root=Root("UI_Button_Main");
            var relief=root.AddComponent<HarborReliefImage>();relief.Gold=true;relief.Radius=HarborDesignTokens.RadiusLarge;relief.Depth=HarborDesignTokens.MainShadowDepth;
            relief.FaceTexture=Skin("UI_MainButton_Gold_v1");
            relief.SliceUV=new Vector4(.23f,.36f,.23f,.28f);relief.SliceSize=new Vector4(66,35,66,27);
            var button=root.AddComponent<Button>();button.targetGraphic=relief;button.transition=Selectable.Transition.None;
            var glow=Child("VoyageGlow",root.transform);var glowImage=glow.gameObject.AddComponent<RawImage>();glowImage.texture=relief.FaceTexture;glowImage.color=new Color(1,.78f,.12f,.18f);glowImage.raycastTarget=false;Fill(glow,-9);
            var face=Child("Face",root.transform);Fill(face);
            var wheel=Child("WheelAccent",face);var wheelImage=wheel.gameObject.AddComponent<RawImage>();wheelImage.texture=Skin("UI_MainButton_Wheel_v1");wheelImage.raycastTarget=false;Place(wheel,new Rect(7,26,62,62));
            var label=Text("Label",face,"",27,FontStyle.Normal);label.rectTransform.anchorMin=new Vector2(0,.42f);label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(66,0);label.rectTransform.offsetMax=new Vector2(-27,-22);
            var sub=Text("SubLabel",face,"",13,FontStyle.Normal);sub.rectTransform.anchorMin=Vector2.zero;sub.rectTransform.anchorMax=new Vector2(1,.47f);sub.rectTransform.offsetMin=new Vector2(66,30);sub.rectTransform.offsetMax=new Vector2(-27,0);
            var motion=root.AddComponent<HarborButtonRelief>();motion.Content=face;
            var pulse=root.AddComponent<HarborMainActionPulse>();pulse.Glow=glowImage;pulse.Emblem=wheel;
            return root;
        }

        private static GameObject BuildResourceHud()
        {
            var root=Root("UI_HUD_Resource");
            var relief=root.AddComponent<HarborReliefImage>();relief.Radius=24;relief.Depth=4;relief.raycastTarget=false;relief.FaceTexture=Skin("UI_HUD_Capsule_v1");relief.SliceUV=new Vector4(.30f,.34f,.24f,.30f);relief.SliceSize=new Vector4(32,16,25,14);
            var icon=Child("IconSlot",root.transform);icon.gameObject.AddComponent<HarborEmblem>().raycastTarget=false;Place(icon,new Rect(8,10,34,34));
            var value=Text("Value",root.transform,"0",19,FontStyle.Normal);Place(value.rectTransform,new Rect(42,9,67,36));
            return root;
        }

        private static GameObject BuildShipDisplay()
        {
            var root=Root("UI_Ship_Display");root.AddComponent<CollectionShipPreview>();return root;
        }

        private static GameObject BuildMainMenu()
        {
            var root=Root("UI_MainMenu");
            var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            root.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            root.AddComponent<GraphicRaycaster>();
            var background=Child("Background",root.transform);var art=background.gameObject.AddComponent<RawImage>();art.raycastTarget=true;Fill(background);
            background.gameObject.AddComponent<HarborBackgroundTreatment>();
            var overlay=Child("BackgroundFocusOverlay",root.transform);overlay.gameObject.AddComponent<HarborFocusOverlay>().raycastTarget=false;Fill(overlay);
            Child("TopHUD",root.transform);Child("PlayerShipView",root.transform);Child("LeftMenu",root.transform);Child("RightMenu",root.transform);
            Child("MainActionButton",root.transform);Child("Notice",root.transform);
            return root;
        }

        private static GameObject Root(string name)
        {
            var value=new GameObject(name,typeof(RectTransform));var rect=value.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;return value;
        }
        private static RectTransform Child(string name,Transform parent)
        {var child=Root(name).GetComponent<RectTransform>();child.SetParent(parent,false);return child;}
        private static HarborText Text(string name,Transform parent,string value,int size,FontStyle style)
        {
            var text=Child(name,parent).gameObject.AddComponent<HarborText>();text.UseDisplayFont=true;text.font=HarborUI.DisplayFont;text.fontSize=size;text.fontStyle=style;text.text=value;text.color=HarborDesignTokens.Ink;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            // CJK fonts have taller ascent/descender metrics than their visible glyphs.
            // These are single-line slots with explicit art padding: do not silently discard a whole line.
            text.verticalOverflow=VerticalWrapMode.Overflow;
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(1,1,1,.42f);shadow.effectDistance=new Vector2(0,1);shadow.useGraphicAlpha=true;
            return text;
        }
        private static Texture2D Skin(string name)=>Resources.Load<Texture2D>("TideboundUI/Skins/"+name);
        private static void Place(RectTransform target,Rect rect){target.anchoredPosition=rect.position;target.sizeDelta=rect.size;}
        private static void Fill(RectTransform target,float margin=0){target.anchorMin=Vector2.zero;target.anchorMax=Vector2.one;target.offsetMin=Vector2.one*margin;target.offsetMax=Vector2.one*-margin;}
        private static void Save(GameObject root,string name)
        {PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+name+".prefab");Object.DestroyImmediate(root);}
        private static void EnsureFolder(string path)
        {
            var parts=path.Split('/');var current=parts[0];
            for(var i=1;i<parts.Length;i++){var next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}
        }
    }
}
