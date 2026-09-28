using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Same relief, gold edge, gloss and press feedback as the approved pause actions.</summary>
    public static class HarborVictoryButton
    {
        public static Button Create(string name,Transform parent,string label,Color color,SettingsIconKind? kind,Action action)
        {
            var root=HarborUI.Rect(name,parent);root.gameObject.SetActive(false);
            var face=HarborUI.Rect("Face",root);HarborUI.Fill(face);
            var art=HarborUI.Surface("Art",face,color);HarborUI.Fill(art.rectTransform);art.raycastTarget=false;
            var skin=(HarborImage)art;skin.Kind=HarborSurfaceKind.Control;skin.Radius=15;skin.Border=3;skin.Depth=6;skin.Edge=new Color(1,.92f,.68f);
            var gloss=HarborUI.Surface("Gloss",face,new Color(1,1,1,.40f));gloss.raycastTarget=false;((HarborImage)gloss).Radius=4;((HarborImage)gloss).Border=0;
            gloss.rectTransform.anchorMin=new Vector2(0,1);gloss.rectTransform.anchorMax=new Vector2(1,1);gloss.rectTransform.pivot=new Vector2(.5f,1);gloss.rectTransform.anchoredPosition=new Vector2(0,-7);gloss.rectTransform.sizeDelta=new Vector2(-16,7);
            var text=HarborApprovedArt.Text("Label",face,label,kind.HasValue?12:25,Color.white);
            var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=Color.Lerp(color,Color.black,.6f);outline.effectDistance=new Vector2(1,-1);
            if(kind.HasValue)
            {
                var icon=HarborUI.Rect("Icon",face).gameObject.AddComponent<HarborSettingsIcon>();icon.Kind=kind.Value;icon.color=Color.white;icon.raycastTarget=false;
                var edge=icon.gameObject.AddComponent<Outline>();edge.effectColor=Color.Lerp(color,Color.black,.6f);edge.effectDistance=new Vector2(1,-1);
                icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(.5f,.5f);icon.rectTransform.pivot=Vector2.one*.5f;icon.rectTransform.anchoredPosition=new Vector2(0,7);icon.rectTransform.sizeDelta=new Vector2(31,31);
                text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=new Vector2(1,0);text.rectTransform.pivot=new Vector2(.5f,0);text.rectTransform.anchoredPosition=new Vector2(0,3);text.rectTransform.sizeDelta=new Vector2(0,19);
            }
            else HarborUI.Fill(text.rectTransform,4);
            var button=HarborApprovedArt.BindButton(root,face,art,action);root.gameObject.SetActive(true);return button;
        }
    }
}
