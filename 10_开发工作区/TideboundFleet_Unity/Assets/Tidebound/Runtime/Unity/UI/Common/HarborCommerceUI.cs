using System;
using Tidebound.Collection;
using UnityEngine;
using UnityEngine.UI;
namespace Tidebound.Unity.UI
{
    /// <summary>Small presentation helpers for the two approved commerce panels.</summary>
    public static class HarborCommerceUI
    {
        public static string L(string cn,string en)=>UILanguage.IsChinese?cn:en;
        public static void P(Component c,float x,float y,float w,float h)
        {var rect=(RectTransform)c.transform;if(rect.parent&&rect.parent.name=="Content")rect.anchorMin=rect.anchorMax=new Vector2(0,1);HarborUI.Place(rect,new Rect(x,y,w,h));
            var button=c.GetComponent<Button>();if(button){var art=button.GetComponentInChildren<HarborSliceGraphic>();if(art&&art.Texture&&art.Texture.name=="CTA_Gold"){art.Border=new Vector4(h*.46f,h*.23f,h*.46f,h*.25f);art.SetVerticesDirty();}}
        }
        public static Text Label(string name,Transform parent,string value,int size=17)
            =>HarborApprovedArt.Text(name,parent,value,size,HarborApprovedArt.Brown);
        public static HarborSliceGraphic Plate(string name,Transform parent,string asset="Card_Ivory")=>HarborApprovedArt.Create(name,asset,parent);
        public static Button Button(string name,Transform parent,string value,Action action,string art="Capsule_Cream",int size=18)
        {
            var r=HarborUI.Rect(name,parent);var face=HarborUI.Rect("Face",r);HarborUI.Fill(face);
            var image=Plate("Surface",face,art);HarborUI.Fill(image.rectTransform);if(art=="CTA_Gold")image.Border=new Vector4(30,17,30,20);
            var button=HarborApprovedArt.BindButton(r,face,image,action);var t=Label("Label",face,art=="Close_Coral"?"×":value,art=="Close_Coral"?32:size);if(art=="Close_Coral")
            {t.text="";var cross=HarborUI.Rect("Cross",face).gameObject.AddComponent<VisualSamples.SampleVectorGraphic>();cross.Kind=VisualSamples.SampleVectorGraphic.Shape.Cross;cross.color=Color.white;cross.raycastTarget=false;HarborUI.Fill(cross.rectTransform,5);}HarborUI.Fill(t.rectTransform);t.rectTransform.offsetMin=new Vector2(7,8);t.rectTransform.offsetMax=new Vector2(-7,-4);return button;
        }
        public static void Caption(Button b,string text)=>b.GetComponentInChildren<Text>().text=text;
        public static RectTransform Header(Transform parent,string text)
        {
            var raw=HarborUI.Art("WoodHeader",parent,"Skins/UI_HarborHeader_v1");raw.uvRect=new Rect(79f/2172,27f/724,2034f/2172,682f/724);
            var label=Label("Title",raw.transform,text,30);HarborUI.Fill(label.rectTransform);label.rectTransform.offsetMin=new Vector2(35,7);label.rectTransform.offsetMax=new Vector2(-35,-24);label.color=Color.white;return raw.rectTransform;
        }
        public static RectTransform Scroll(string name,Transform parent,out RectTransform content)
        {
            var r=HarborUI.Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.color=Color.clear;image.raycastTarget=true;r.gameObject.AddComponent<RectMask2D>();
            content=HarborUI.Rect("Content",r);content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);
            var scroll=r.gameObject.AddComponent<ScrollRect>();scroll.viewport=r;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=25;return r;
        }
        public static Color Rarity(SkinRarity rarity)=>new[]{new Color(.88f,.91f,.92f),new Color(.38f,.78f,.97f),new Color(.69f,.49f,.94f),new Color(1,.77f,.20f),new Color(.98f,.39f,.34f)}[(int)rarity];
        public static string RarityName(SkinRarity rarity)=>(UILanguage.IsChinese?new[]{"白色","蓝色","紫色","金色","红色"}:new[]{"White","Blue","Purple","Gold","Red"})[(int)rarity];
        public static RawImage Item(Transform parent,string id,Rect box)
        {
            var raw=HarborUI.Rect("Artwork_"+id,parent).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;raw.uvRect=new Rect(0,0,1,1);
            if(id.StartsWith("TF_SKIN")){var r=HarborAppearanceArt.Skin(id);raw.texture=Resources.Load<Texture2D>(r.resourcePath);var uv=HarborAppearanceArt.UV(r.uv);raw.uvRect=r.rotation==180?new Rect(uv.xMax,uv.yMax,-uv.width,-uv.height):uv;}
            else if(id.StartsWith("TF_TRAIL")){var r=HarborAppearanceArt.Trail(id);raw.texture=Resources.Load<Texture2D>(r.resourcePath);raw.uvRect=HarborAppearanceArt.UV(r.uv);}
            else if(id.StartsWith("TF_SHOWCASE")){raw.texture=ShowcaseArt.Load(id);raw.uvRect=ShowcaseArt.VisibleUV(id);}
            else raw.texture=HarborAppearanceArt.SceneTexture(id);
            Fit(raw,box);return raw;
        }
        public static void Fit(RawImage raw,Rect box)
        {
            var aspect=raw.texture?Mathf.Abs(raw.texture.width*raw.uvRect.width/(raw.texture.height*raw.uvRect.height)):1;
            var w=Mathf.Min(box.width,box.height*aspect);var h=w/aspect;P(raw,box.x+(box.width-w)/2,box.y+(box.height-h)/2,w,h);
        }
        // Aspect-fill backgrounds by cropping UVs. Item sprites continue to use Fit.
        public static void Cover(RawImage raw,Rect box,float verticalFocus=.5f)
        {
            var uv=raw.uvRect;var aspect=raw.texture?(float)raw.texture.width/raw.texture.height:1;
            var target=box.width/box.height;var visible=aspect*uv.width/uv.height;
            if(visible>target){var width=uv.height*target/aspect;uv.x+=(uv.width-width)*.5f;uv.width=width;}
            else {var height=uv.width*aspect/target;uv.y+=(uv.height-height)*verticalFocus;uv.height=height;}
            raw.uvRect=uv;P(raw,box.x,box.y,box.width,box.height);
        }
        public static RectTransform DisplayBoard(string name,Transform parent,Rect box,Texture2D texture,float verticalFocus=.5f)
        {
            var frame=Plate(name,parent,"Panel_Gold");frame.Border=new Vector4(12,12,12,12);P(frame,box.x,box.y,box.width,box.height);
            var clip=HarborUI.Rect("RoundedPicture",frame.transform);P(clip,4,4,box.width-8,box.height-8);
            var image=HarborUI.Rect("Background",clip).gameObject.AddComponent<HarborBackdropGraphic>();image.texture=texture;image.raycastTarget=false;
            Cover(image,new Rect(0,0,box.width-8,box.height-8),verticalFocus);return frame.rectTransform;
        }
        public static Texture2D CommerceBackground(string name)=>Resources.Load<Texture2D>("TideboundUI/Commerce/"+name);
        public static string RewardBackground(DrawPool pool,SkinRarity rarity)=>pool==DrawPool.Trail?"Display_Sea":pool==DrawPool.Showcase?"Reward_Purple":"Reward_"+new[]{"White","Blue","Purple","Gold","Red"}[(int)rarity];
        public static void Clear(Transform root)
        {for(var i=root.childCount-1;i>=0;i--){var child=root.GetChild(i);child.gameObject.SetActive(false);UnityEngine.Object.Destroy(child.gameObject);}}
    }
}
