using Tidebound.Unity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    public static class ApprovedHomePrefabs
    {
        [MenuItem("Tools/Tidebound/Approved UI/Rebuild Home Components")]
        public static void Build()
        {
            var root=Root("UI_HomeApprovedMain");var face=HarborUI.Rect("Face",root.transform);HarborUI.Fill(face);
            var gold=HarborApprovedArt.Create("GoldSurface","CTA_Gold",face);HarborUI.Fill(gold.rectTransform);
            HarborApprovedArt.BindButton((RectTransform)root.transform,face,gold);
            var label=HarborApprovedArt.Text("Label",face,"Start Voyage",29,HarborApprovedArt.Ink);Anchor(label.rectTransform,new Vector2(0,.43f),Vector2.one,new Vector2(14,0),new Vector2(-14,-8));
            var sub=HarborApprovedArt.Text("SubLabel",face,"Level 1",17,HarborApprovedArt.Ink);Anchor(sub.rectTransform,Vector2.zero,new Vector2(1,.49f),new Vector2(16,15),new Vector2(-16,0));
            Save(root);
            root=Root("UI_HomeApprovedEntry");face=HarborUI.Rect("Face",root.transform);HarborUI.Fill(face);
            var pearl=HarborUI.Art("PearlBase",face,"Skins/UI_HomeEntry_Pearl_v2");HarborUI.Fill(pearl.rectTransform);pearl.uvRect=new Rect(54f/1254,66f/1254,1145f/1254,1139f/1254);
            HarborApprovedArt.BindButton((RectTransform)root.transform,face,pearl);
            HarborUI.Art("IconSlot",face,"Icon_Collection_v1");
            label=HarborApprovedArt.Text("Label",face,"Collection",17,HarborApprovedArt.Ink);HarborUI.Place(label.rectTransform,new Rect(2,5,76,28));
            Save(root);
            root=Root("UI_HomeApprovedCoins");var cream=HarborApprovedArt.Create("CreamSurface","Capsule_Cream",root.transform);HarborUI.Fill(cream.rectTransform);
            var coin=HarborApprovedArt.Create("IconSlot","Coin_Anchor",root.transform);HarborUI.Place(coin.rectTransform,new Rect(4,3,34,34));
            label=HarborApprovedArt.Text("Value",root.transform,"0",21,HarborApprovedArt.Ink);Anchor(label.rectTransform,Vector2.zero,Vector2.one,new Vector2(40,3),new Vector2(-9,-3));
            Save(root);AssetDatabase.SaveAssets();
        }
        private static GameObject Root(string name)=>HarborUI.Rect(name,null).gameObject;
        private static void Anchor(RectTransform r,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi){r.anchorMin=min;r.anchorMax=max;r.offsetMin=lo;r.offsetMax=hi;}
        private static void Save(GameObject root){PrefabUtility.SaveAsPrefabAsset(root,HarborUIPrefabBuilder.Folder+"/"+root.name+".prefab");Object.DestroyImmediate(root);}
    }
}
