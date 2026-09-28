using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Only the reviewed component family; this class never reads gameplay or preview state.</summary>
    public static class HarborApprovedArt
    {
        [Serializable] private sealed class Record {public string name;public float[] uv,cuts,border;}
        [Serializable] private sealed class Manifest {public Record[] assets;}
        private static Manifest manifest;
        public static readonly Color Ink=new Color(.035f,.24f,.36f),Brown=new Color(.44f,.23f,.10f);
        public static HarborSliceGraphic Create(string name,string asset,Transform parent)
        {
            var image=HarborUI.Rect(name,parent).gameObject.AddComponent<HarborSliceGraphic>();
            Set(image,asset);image.raycastTarget=false;return image;
        }
        public static void Set(HarborSliceGraphic image,string asset)
        {
            if(manifest==null)manifest=JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("TideboundUI/Approved/asset_manifest").text);
            var r=manifest.assets.First(x=>x.name==asset);
            image.Texture=Resources.Load<Texture2D>("TideboundUI/Approved/"+asset);
            image.UV=new Rect(r.uv[0],r.uv[1],r.uv[2],r.uv[3]);
            image.Cuts=new Vector4(r.cuts[0],r.cuts[1],r.cuts[2],r.cuts[3]);
            image.Border=new Vector4(r.border[0],r.border[1],r.border[2],r.border[3]);image.SetAllDirty();
        }
        public static Button BindButton(RectTransform root,RectTransform face,Graphic surface,Action action=null)
        {
            var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var button=root.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            var feedback=root.gameObject.AddComponent<HarborPressFeedback>();feedback.Face=face;feedback.Surface=surface;
            if(action!=null)button.onClick.AddListener(()=>action());return button;
        }
        public static Text Text(string name,Transform parent,string source,int size,Color color)
        {
            var text=HarborUI.Rect(name,parent).gameObject.AddComponent<HarborText>();text.UseDisplayFont=true;
            text.font=HarborUI.DisplayFont;text.text=source;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;
            text.raycastTarget=false;text.resizeTextForBestFit=false;text.supportRichText=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
        }
    }
}
