using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Decorated CTA presentation only. The Button, raycast rectangle and callbacks stay on the fixed root.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class HarborHeroButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
    {
        public enum Theme { Voyage, Draw }
        public Func<bool> AllowMotion;
        public Theme Style { get; private set; }
        public Text Title { get; private set; }
        public Text Subtitle { get; private set; }
        public RectTransform Visual => face;
        public RectTransform Decoration => decor.rectTransform;
        public bool IsPressed => held || submitUntil > Time.unscaledTime;
        Button button; RectTransform face; HarborSliceGraphic plate,coin;
        RawImage decor; HarborHeroLight rays,glint; CanvasGroup opacity;
        float phase,press,submitUntil; bool held,showCoin; Vector2 decorRest;
        Vector2 layoutSize=new Vector2(-1,-1);string layoutSubtitle;bool layoutChinese,layoutCoin;

        public static HarborHeroButton Apply(Button button,Theme style)
        {
            var existing=button.GetComponent<HarborHeroButton>();if(existing)return existing;
            var presentation=button.gameObject.AddComponent<HarborHeroButton>();presentation.Build(button,style);return presentation;
        }
        void Build(Button target,Theme style)
        {
            button=target;Style=style;button.transition=Selectable.Transition.None;
            var oldPress=GetComponent<HarborPressFeedback>();if(oldPress){oldPress.SetPressed(false);oldPress.enabled=false;}
            face=(RectTransform)transform.Find("Face");
            // Preserve the established label paths used by the home data binding.
            Title=face.Find("Label").GetComponent<Text>();var sub=face.Find("SubLabel");
            Subtitle=sub?sub.GetComponent<Text>():HarborApprovedArt.Text("SubLabel",face,"",16,HarborApprovedArt.Brown);
            for(var i=0;i<face.childCount;i++){var child=face.GetChild(i);if(child!=Title.transform&&child!=Subtitle.transform)child.gameObject.SetActive(false);}
            HarborUI.Fill(face);face.pivot=new Vector2(.5f,.5f);face.anchoredPosition=Vector2.zero;
            opacity=face.gameObject.AddComponent<CanvasGroup>();opacity.interactable=true;opacity.blocksRaycasts=false;
            plate=HarborUI.Rect("HeroGoldSurface",face).gameObject.AddComponent<HarborSliceGraphic>();
            plate.Texture=Resources.Load<Texture2D>("TideboundUI/HeroButtons/CTA_Gold_v3");
            // Original RGBA pixels are preserved. UV removes only transparent canvas padding.
            plate.UV=new Rect(52f/1942,188f/809,1836f/1942,470f/809);
            plate.Cuts=new Vector4(.155f,0,.155f,0);plate.raycastTarget=false;HarborUI.Fill(plate.rectTransform);plate.transform.SetAsFirstSibling();
            rays=HarborUI.Rect("ChestRays",face).gameObject.AddComponent<HarborHeroLight>();rays.Kind=HarborHeroLight.Shape.Rays;rays.raycastTarget=false;
            rays.color=new Color(1,.83f,.25f,.29f);rays.gameObject.SetActive(style==Theme.Draw);
            decor=HarborUI.Rect("HeroDecoration",face).gameObject.AddComponent<RawImage>();decor.raycastTarget=false;
            decor.texture=Resources.Load<Texture2D>("TideboundUI/HeroButtons/"+(style==Theme.Voyage?"Voyage_Decor":"Draw_Chest"));
            decor.uvRect=style==Theme.Voyage?new Rect(83f/1254,138f/1254,1080f/1254,956f/1254):new Rect(104f/1254,147f/1254,1050f/1254,973f/1254);
            glint=HarborUI.Rect("RimGlint",face).gameObject.AddComponent<HarborHeroLight>();glint.Kind=HarborHeroLight.Shape.Star;glint.raycastTarget=false;glint.color=new Color(1,1,.9f,.85f);
            coin=HarborApprovedArt.Create("PriceCoin","Coin_Anchor",face);coin.gameObject.SetActive(false);
            StyleLabel(Title,true);StyleLabel(Subtitle,false);Title.transform.SetAsLastSibling();Subtitle.transform.SetAsLastSibling();
            Layout();
        }
        static void StyleLabel(Text text,bool main)
        {
            text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=Vector2.zero;
            if(text is HarborText localized)localized.UseDisplayFont=true;
            text.font=HarborUI.DisplayFont;text.fontStyle=main&&UILanguage.IsChinese?FontStyle.Bold:FontStyle.Normal;text.color=new Color(.29f,.105f,.035f);
            text.resizeTextForBestFit=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            foreach(var effect in text.GetComponents<BaseMeshEffect>())effect.enabled=false;
            var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.96f,.73f,.97f);outline.effectDistance=Vector2.one*(main?.8f:.45f);
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.60f,.26f,.03f,.65f);shadow.effectDistance=new Vector2(0,main?-1.65f:-.8f);
        }
        public void SetContent(string title,string subtitle,bool priceCoin=false)
        {Title.text=title;Subtitle.text=subtitle;showCoin=priceCoin;coin.gameObject.SetActive(priceCoin);Layout();}
        void Layout()
        {
            if(!face)return;var size=((RectTransform)transform).rect.size;var w=size.x;var h=size.y;
            if(size==layoutSize&&layoutSubtitle==Subtitle.text&&layoutChinese==UILanguage.IsChinese&&layoutCoin==showCoin)return;
            layoutSize=size;layoutSubtitle=Subtitle.text;layoutChinese=UILanguage.IsChinese;layoutCoin=showCoin;
            plate.Border=new Vector4(h*.606f,0,h*.606f,0);plate.SetVerticesDirty();
            var iconWidth=Mathf.Min(h*1.10f,w*.29f);var aspect=decor.texture.width*decor.uvRect.width/(decor.texture.height*decor.uvRect.height);
            var iconHeight=iconWidth/aspect;decorRest=new Vector2(3,h*.19f);
            HarborUI.Place(decor.rectTransform,new Rect(decorRest.x,decorRest.y,iconWidth,iconHeight));
            var lightSize=iconWidth*1.17f;HarborUI.Place(rays.rectTransform,new Rect(3+(iconWidth-lightSize)/2,h*.20f+(iconHeight-lightSize)/2,lightSize,lightSize));
            rays.rectTransform.pivot=new Vector2(.5f,.5f);rays.rectTransform.anchoredPosition=new Vector2(3+iconWidth/2,h*.20f+iconHeight/2);
            var left=iconWidth+6;var textWidth=w-left-18;
            Title.fontStyle=UILanguage.IsChinese?FontStyle.Bold:FontStyle.Normal;
            Title.fontSize=UILanguage.IsChinese?Mathf.RoundToInt(h*.40f):Mathf.RoundToInt(h*.31f);
            Subtitle.fontSize=UILanguage.IsChinese?Mathf.RoundToInt(h*.215f):Mathf.RoundToInt(h*.20f);
            HarborUI.Place(Title.rectTransform,new Rect(left,h*.42f,textWidth,h*.48f));
            HarborUI.Place(Subtitle.rectTransform,new Rect(left,h*.14f,textWidth,h*.28f));
            if(showCoin)
            {
                var diameter=h*.24f;var total=Subtitle.preferredWidth+diameter+4;var x=left+(textWidth-total)/2;
                HarborUI.Place(coin.rectTransform,new Rect(x,h*.155f,diameter,diameter));
                HarborUI.Place(Subtitle.rectTransform,new Rect(x+diameter+4,h*.14f,Subtitle.preferredWidth+1,h*.28f));
            }
            HarborUI.Place(glint.rectTransform,new Rect(w-h*.39f,h*.79f,h*.21f,h*.21f));
        }
        void LateUpdate()
        {
            if(!face)return;Layout();var enabled=button.IsInteractable();if(!enabled){held=false;submitUntil=0;}
            var motion=enabled&&Application.isFocused&&(AllowMotion?.Invoke()!=false);
            if(motion)phase+=Time.unscaledDeltaTime;
            press=Mathf.MoveTowards(press,IsPressed?1:0,Time.unscaledDeltaTime*12);
            // A full slow breathing cycle is 2.8 s. Draw only animates its illustration and rays.
            var breathe=motion&&Style==Theme.Voyage&&!IsPressed?.0125f*(1-Mathf.Cos(phase*Mathf.PI*2/2.8f)):0;
            face.localScale=Vector3.one*(1+breathe-press*.03f);face.anchoredPosition=Vector2.down*(press*2);
            var bob=motion&&Style==Theme.Draw?Mathf.Sin(phase*Mathf.PI*2/3.2f)*1.6f:0;
            decor.rectTransform.anchoredPosition=decorRest+Vector2.up*bob;
            rays.rectTransform.localRotation=Quaternion.Euler(0,0,motion?phase*30:0);
            rays.gameObject.SetActive(Style==Theme.Draw&&enabled);
            glint.color=new Color(1,1,.90f,enabled?(motion?.60f+.22f*Mathf.Sin(phase*1.7f):.65f):0);
            opacity.alpha=enabled?1:.65f;plate.color=enabled?Color.white:new Color(.77f,.78f,.8f);
        }
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&button.IsInteractable())held=true;}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)held=false;}
        public void OnPointerExit(PointerEventData e){held=false;}
        public void OnSubmit(BaseEventData e){if(button.IsInteractable())submitUntil=Time.unscaledTime+.13f;}
        void ResetMotion(){held=false;submitUntil=press=phase=0;if(face){face.localScale=Vector3.one;face.anchoredPosition=Vector2.zero;}}
        void OnDisable()=>ResetMotion();
        void OnApplicationFocus(bool focus){if(!focus)ResetMotion();}
    }
}
