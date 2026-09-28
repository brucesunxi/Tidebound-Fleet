using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Alpha bounds are measured offline; source images are retained unchanged.</summary>
    public static class GameplayArt
    {
        [Serializable] public sealed class Record { public string id; public float[] uv; public float aspect; }
        [Serializable] private sealed class Catalog { public Record[] items; }
        private static Catalog catalog;
        public static Record Get(string id)
        {
            if(catalog==null)catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("TideboundUI/Gameplay/art_catalog").text);
            return catalog.items.FirstOrDefault(x=>x.id==id);
        }
        public static RawImage Image(string name,Transform parent,string id)
        {
            var image=HarborUI.Rect(name,parent).gameObject.AddComponent<RawImage>();
            Set(image,id);image.raycastTarget=false;return image;
        }
        public static void Set(RawImage image,string id,bool mirror=false)
        {
            var record=Get(id);image.texture=Resources.Load<Texture2D>("TideboundUI/Gameplay/"+id);
            var uv=record==null?new Rect(0,0,1,1):HarborAppearanceArt.UV(record.uv);
            image.uvRect=mirror?new Rect(uv.xMax,uv.y,-uv.width,uv.height):uv;
        }
        public static void Fit(RawImage image,Rect bounds,float aspect)
        {
            var size=new Vector2(Mathf.Min(bounds.width,bounds.height*aspect),Mathf.Min(bounds.height,bounds.width/aspect));
            HarborUI.Place(image.rectTransform,new Rect(bounds.center-size/2,size));
        }
        public static RawImage Texture(string name,Transform parent,string resource)
        {
            var image=HarborUI.Rect(name,parent).gameObject.AddComponent<RawImage>();
            image.texture=Resources.Load<Texture2D>(resource);image.raycastTarget=false;return image;
        }
        public static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=Vector2.one*.5f;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
