using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    public static class HarborUI
    {
        public static readonly Color Ink=HarborDesignTokens.Ink, Cream=HarborDesignTokens.PearlWarm,
            Aqua=HarborDesignTokens.OceanSoft, Gold=HarborDesignTokens.Gold, Wood=HarborDesignTokens.GoldDark;
        private static Font font;
        private static Font chineseDisplayFont, latinDisplayFont;
        public static Font DisplayFont => UILanguage.IsChinese
            ? (chineseDisplayFont != null ? chineseDisplayFont : (chineseDisplayFont=Resources.Load<Font>("TideboundUI/ResourceHanRoundedCN-Bold") ?? Font))
            : (latinDisplayFont != null ? latinDisplayFont : (latinDisplayFont=Resources.Load<Font>("TideboundUI/LilitaOne-Regular") ?? Font));
        public static Font Font => font != null ? font : (font=Resources.Load<Font>("TideboundUI/NotoSansCJKsc-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        public static RectTransform Rect(string name,Transform parent)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;return r;
        }
        public static void Place(RectTransform r,Rect area){r.anchoredPosition=area.position;r.sizeDelta=area.size;}
        public static void Fill(RectTransform r,float margin=0)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*margin;r.offsetMax=Vector2.one*-margin;}
        public static Text Label(string name,Transform parent,string text,int size=16)
        {
            var t=Rect(name,parent).gameObject.AddComponent<HarborText>();t.font=Font;t.fontSize=size;t.text=text;
            if(name.Contains("Title"))t.fontStyle=FontStyle.Bold;
            t.color=Ink;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.supportRichText=false;
            var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(HarborDesignTokens.OceanShadow.r,HarborDesignTokens.OceanShadow.g,HarborDesignTokens.OceanShadow.b,.28f);shadow.effectDistance=new Vector2(0,-1);shadow.useGraphicAlpha=true;
            return t;
        }
        public static Image Surface(string name,Transform parent,Color color)
        {var r=Rect(name,parent);var i=r.gameObject.AddComponent<HarborImage>();i.color=color;return i;}
        public static RectTransform Header(string name,Transform parent)
        {
            var image=Art(name,parent,"Skins/UI_HarborHeader_v1");return image.rectTransform;
        }
        public static void StyleClose(Button button)
        {
            var face=button.GetComponent<HarborImage>();face.Radius=25;face.color=new Color(.94f,.25f,.18f);
            var text=button.GetComponentInChildren<Text>();text.color=Color.white;text.fontSize=28;
        }
        public static RectTransform Popup(string name,Transform parent)
        {
            var r=Prefab("UI_Popup",parent,name);
            if(r==null)throw new InvalidOperationException("Missing UI_Popup prefab. Rebuild design system prefabs.");
            return r;
        }
        public static Button Control(string name,Transform parent,string text,Action action,bool card=false)
        {
            var r=Prefab(card?"UI_ItemCard":"UI_Tab",parent,name);
            if(r==null)throw new InvalidOperationException("Missing common UI control prefab. Rebuild design system prefabs.");
            var b=r.GetComponent<Button>();b.GetComponentInChildren<Text>().text=text;
            if(action!=null)b.onClick.AddListener(()=>action());return b;
        }
        public static void SetSelected(Button button,bool selected)
        {
            var surface=button.GetComponent<HarborImage>();if(surface==null)return;
            surface.Selected=selected;surface.color=selected?Color.Lerp(Cream,Aqua,.45f):Cream;surface.SetVerticesDirty();
        }
        public static void DisplayText(Text label,int size)
        {
            if(label is HarborText text)text.UseDisplayFont=true;
            label.font=DisplayFont;label.fontSize=size;label.fontStyle=FontStyle.Normal;
            label.verticalOverflow=VerticalWrapMode.Overflow;
        }
        public static Button Button(string name,Transform parent,string text,Action action,bool primary=false)
        {
            var b=CreateButtonPrefab(name,parent,primary);var image=b.GetComponent<HarborReliefImage>();image.color=image.FaceTexture!=null?Color.white:primary?Gold:Color.white;
            if(!primary)
            {
                // Text-only actions use the horizontal skin; keep the portrait skin for home icon entries.
                image.FaceTexture=Resources.Load<Texture2D>("TideboundUI/Skins/UI_HUD_Capsule_v1");
                image.SliceUV=new Vector4(.30f,.34f,.24f,.30f);image.SliceSize=new Vector4(22,12,18,10);
            }
            foreach(var decoration in new[]{"Face/WheelAccent","VoyageGlow"})
            {var item=b.transform.Find(decoration);if(item!=null)item.gameObject.SetActive(false);}
            var icon=b.transform.Find("Face/IconSlot");if(icon!=null)icon.gameObject.SetActive(false);
            var status=b.transform.Find("Face/Status");if(status!=null)status.gameObject.SetActive(false);
            var label=b.transform.Find("Face/Label")?.GetComponent<Text>();
            if(label==null){label=Label("Label",b.transform,text,16);Fill(label.rectTransform,6);}else{label.text=text;label.fontSize=16;label.resizeTextForBestFit=false;Fill(label.rectTransform,6);}
            if(label is HarborText localized){localized.UseDisplayFont=false;localized.font=Font;}
            if(action!=null)b.onClick.AddListener(()=>action());return b;
        }
        public static Button RaisedButton(string name,Transform parent,string text,Action action,bool primary=false)
        {
            var button=CreateButtonPrefab(name,parent,primary);var label=button.transform.Find("Face/Label")?.GetComponent<Text>();
            if(label!=null)label.text=text;if(action!=null)button.onClick.AddListener(()=>action());return button;
        }
        public static RectTransform Prefab(string resourceName,Transform parent,string instanceName=null)
        {
            var prefab=Resources.Load<GameObject>("TideboundUI/Prefabs/"+resourceName);
            if(prefab==null)return null;
            var instance=UnityEngine.Object.Instantiate(prefab,parent,false);if(!string.IsNullOrEmpty(instanceName))instance.name=instanceName;
            return instance.GetComponent<RectTransform>();
        }
        public static RawImage SetButtonArt(Button button,string asset)
        {
            var slot=button.transform.Find("Face/IconSlot");RawImage image;
            if(slot!=null){slot.gameObject.SetActive(true);image=slot.GetComponent<RawImage>();}
            else image=Art("Icon",button.transform.Find("Face")??button.transform,asset);
            image.texture=Resources.Load<Texture2D>("TideboundUI/"+asset);image.raycastTarget=false;
            var relief=button.GetComponent<HarborButtonRelief>();if(relief!=null)relief.SetFloatingIcon(image.rectTransform);return image;
        }
        public static Text SetButtonStatus(Button button,string value)
        {
            var slot=button.transform.Find("Face/Status");if(slot==null)return null;slot.gameObject.SetActive(!string.IsNullOrEmpty(value));
            var text=slot.GetComponent<Text>();text.text=value;return text;
        }
        public static RawImage Art(string name,Transform parent,string asset)
        {
            var r=Rect(name,parent);var image=r.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("TideboundUI/"+asset);
            image.raycastTarget=false;return image;
        }
        public static ScrollRect Scroll(string name,Transform parent,out RectTransform content)
        {
            var root=Rect(name,parent);var image=root.gameObject.AddComponent<Image>();image.color=new Color(1,1,1,.015f);
            var scroll=root.gameObject.AddComponent<ScrollRect>();root.gameObject.AddComponent<RectMask2D>();
            content=Rect("Content",root);content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);
            scroll.viewport=root;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=28;
            var track=Surface("ScrollTrack",root,new Color(.88f,.90f,.86f));track.raycastTarget=false;((HarborImage)track).Border=0;
            track.rectTransform.anchorMin=new Vector2(1,0);track.rectTransform.anchorMax=Vector2.one;
            track.rectTransform.pivot=new Vector2(1,.5f);track.rectTransform.offsetMin=new Vector2(-5,2);track.rectTransform.offsetMax=new Vector2(0,-2);
            var handle=Surface("Handle",track.transform,new Color(.24f,.68f,.78f));handle.raycastTarget=false;((HarborImage)handle).Border=0;Fill(handle.rectTransform);
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle.rectTransform;bar.direction=Scrollbar.Direction.BottomToTop;
            bar.targetGraphic=handle;bar.interactable=false;scroll.verticalScrollbar=bar;return scroll;
        }
        public static void Focus(Button button)
        {if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button!=null?button.gameObject:null);}

        private static Button CreateButtonPrefab(string name,Transform parent,bool primary)
        {
            var root=Prefab(primary?"UI_Button_Main":"UI_Button_Common",parent,name);
            if(root==null)
            {
                root=Rect(name,parent);var image=root.gameObject.AddComponent<HarborReliefImage>();image.Gold=primary;image.Radius=primary?HarborDesignTokens.RadiusLarge:HarborDesignTokens.RadiusMedium;image.Depth=primary?HarborDesignTokens.MainShadowDepth:HarborDesignTokens.ShadowDepth;
                var face=Rect("Face",root);Fill(face);var label=Label("Label",face,"",primary?28:15);label.fontStyle=FontStyle.Bold;Fill(label.rectTransform,6);
                var motion=root.gameObject.AddComponent<HarborButtonRelief>();motion.Content=face;
            }
            var button=root.GetComponent<Button>()??root.gameObject.AddComponent<Button>();var graphic=root.GetComponent<Graphic>();button.targetGraphic=graphic;button.transition=Selectable.Transition.None;
            button.navigation=new Navigation{mode=Navigation.Mode.Automatic};return button;
        }
    }
}
