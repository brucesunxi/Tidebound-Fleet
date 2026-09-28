using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tidebound.Unity.UI.VisualSamples;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    public static partial class VisualSampleReview
    {
        // This renders a labelled comparison document. It never supplies pixels to a product UI component.
        private static IEnumerator CaptureComparisons(VisualSampleGallery g)
        {
            var textures=new List<Texture2D>();
            var home=ReadTexture(Path.Combine(Repo,"30_设计与素材库/UI设计/20260921_主页终版目标图/Home_FinalTarget_EN_v2.png"),textures);
            var targetB=ReadTexture(Path.Combine(Repo,"30_设计与素材库/外部参考/20260921_收藏弹窗四稿/皮肤弹窗-形象.png"),textures);
            var sampleA=ReadTexture(Path.Combine(Output,"A_CN_390x844_GameView.png"),textures);
            var sampleB=ReadTexture(Path.Combine(Output,"B_CN_390x844_GameView.png"),textures);
            g.PreviewCanvas.gameObject.SetActive(false);
            var board=new GameObject("ComparisonDocument_NotUIAsset",typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var bg=VisualSampleGallery.Rect("Background",board.transform).gameObject.AddComponent<Image>();VisualSampleGallery.Fill(bg.rectTransform);bg.color=new Color(.92f,.94f,.96f);
            SetSize(new Vector2Int(824,470));yield return Settle();
            CompareLabel(board.transform,"目标稿对应区域",16,8,390,30,19);
            CompareLabel(board.transform,"Unity Game View 对应区域",418,8,390,30,19);
            CompareLabel(board.transform,"统一按 390 宽页面尺度展示 · 左侧为目标原图裁切，右侧为运行截图裁切",16,425,792,34,14);
            var s=390f/home.width;
            Crop(board.transform,home,new Rect(148,88,195,86),new Vector2(112,66),s);
            Crop(board.transform,sampleA,new Rect(69,47,105,44),new Vector2(514,66),1);
            Crop(board.transform,home,new Rect(18,592,178,191),new Vector2(116,147),s);
            Crop(board.transform,sampleA,new Rect(15,292,87,100),new Vector2(518,147),1);
            Crop(board.transform,home,new Rect(143,1357,568,217),new Vector2(78,290),s);
            Crop(board.transform,sampleA,new Rect(62,617,267,100),new Vector2(480,290),1);
            yield return Settle();yield return Save("Comparison_A_Components_390Scale.png");
            UnityEngine.Object.Destroy(board);yield return null;
            board=new GameObject("ComparisonDocument_NotUIAsset",typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            bg=VisualSampleGallery.Rect("Background",board.transform).gameObject.AddComponent<Image>();VisualSampleGallery.Fill(bg.rectTransform);bg.color=new Color(.92f,.94f,.96f);
            SetSize(new Vector2Int(824,950));yield return Settle();
            CompareLabel(board.transform,"目标形象页 · 原图",16,8,390,34,19);
            CompareLabel(board.transform,"小样 B · Unity Game View",418,8,390,34,19);
            Crop(board.transform,targetB,new Rect(0,0,targetB.width,targetB.height),new Vector2(16,51),390f/targetB.width);
            Crop(board.transform,sampleB,new Rect(0,0,390,844),new Vector2(418,51),1);
            CompareLabel(board.transform,"两列同为 390 宽；目标图本身是较短裁图，保留原比例，不强行拉高。",16,904,792,30,14);
            yield return Settle();yield return Save("Comparison_B_390Width.png");
            UnityEngine.Object.Destroy(board);foreach(var texture in textures)UnityEngine.Object.Destroy(texture);
            SetSize(new Vector2Int(390,844));yield return Settle();g.PreviewCanvas.gameObject.SetActive(true);g.ApplyLayout();yield return Settle();
        }
        private static Texture2D ReadTexture(string path,List<Texture2D> textures)
        {var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));texture.filterMode=FilterMode.Bilinear;textures.Add(texture);return texture;}
        private static void Crop(Transform parent,Texture2D texture,Rect pixels,Vector2 position,float scale)
        {
            var image=VisualSampleGallery.Rect("UnmodifiedImageCrop",parent).gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            image.uvRect=new Rect(pixels.x/texture.width,1-(pixels.y+pixels.height)/texture.height,pixels.width/texture.width,pixels.height/texture.height);
            VisualSampleGallery.Place(image.rectTransform,position.x,position.y,pixels.width*scale,pixels.height*scale);
        }
        private static void CompareLabel(Transform parent,string content,float x,float y,float width,float height,int size)
        {var label=VisualSampleGallery.Rect("DocumentCaption",parent).gameObject.AddComponent<Text>();label.font=Resources.Load<Font>("TideboundUI/ResourceHanRoundedCN-Bold");label.text=content;label.fontSize=size;label.color=new Color(.055f,.17f,.24f);label.alignment=TextAnchor.MiddleCenter;VisualSampleGallery.Place(label.rectTransform,x,y,width,height);}
    }
}
