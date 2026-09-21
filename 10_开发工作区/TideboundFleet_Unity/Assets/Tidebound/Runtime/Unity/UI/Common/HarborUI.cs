using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    public static class HarborUI
    {
        public static readonly Color Ink=new Color(.045f,.22f,.32f), Cream=new Color(1,.968f,.86f),
            Aqua=new Color(.84f,.95f,.94f), Gold=new Color(1,.79f,.22f), Wood=new Color(.55f,.27f,.10f);
        private static Font font;
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
            return t;
        }
        public static Image Surface(string name,Transform parent,Color color)
        {var r=Rect(name,parent);var i=r.gameObject.AddComponent<HarborImage>();i.color=color;return i;}
        public static Button Button(string name,Transform parent,string text,Action action,bool primary=false)
        {
            var i=Surface(name,parent,primary?Gold:Aqua);var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;
            var c=b.colors;c.highlightedColor=new Color(1,1,.93f);c.pressedColor=new Color(.77f,.85f,.86f);c.disabledColor=new Color(.75f,.78f,.77f,.7f);b.colors=c;
            b.navigation=new Navigation{mode=Navigation.Mode.Automatic};
            if(action!=null)b.onClick.AddListener(()=>action());
            var t=Label("Label",i.transform,text,16);Fill(t.rectTransform,6);return b;
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
            var track=Surface("ScrollTrack",root,new Color(.88f,.90f,.86f));track.raycastTarget=false;
            track.rectTransform.anchorMin=new Vector2(1,0);track.rectTransform.anchorMax=Vector2.one;
            track.rectTransform.pivot=new Vector2(1,.5f);track.rectTransform.offsetMin=new Vector2(-5,2);track.rectTransform.offsetMax=new Vector2(0,-2);
            var handle=Surface("Handle",track.transform,new Color(.24f,.68f,.78f));handle.raycastTarget=false;Fill(handle.rectTransform);
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle.rectTransform;bar.direction=Scrollbar.Direction.BottomToTop;
            bar.targetGraphic=handle;bar.interactable=false;scroll.verticalScrollbar=bar;return scroll;
        }
        public static void Focus(Button button)
        {if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button!=null?button.gameObject:null);}
    }
}
