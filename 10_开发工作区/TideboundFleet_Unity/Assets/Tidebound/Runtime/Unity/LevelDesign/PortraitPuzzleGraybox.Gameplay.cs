using Tidebound.Tools;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class PortraitPuzzleGraybox
    {
        private RawImage ocean,coin,levelPlaque;
        private Text battleCoins;
        private Text toolBudget;
        private readonly Text[] stockLabels=new Text[3];
        private readonly RawImage[] toolIcons=new RawImage[3];
        private readonly GameplayHudGraphic[] toolFaces=new GameplayHudGraphic[3];
        private readonly RectTransform[] stockBadges=new RectTransform[3];
        private void BuildOcean(Camera camera)
        {

            var r=Rect("ContinuousOceanBackground",presentation.transform);var c=r.gameObject.AddComponent<Canvas>();
            c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=40;c.sortingOrder=-100;
            ocean=GameplayArt.Image("UserSelectedOcean",r,"Open_Ocean_v1");GameplayArt.Stretch(ocean.rectTransform);
            foreach(var t in r.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
        }
        private void BuildGameplayControls()
        {
            topPanel.GetComponent<Image>().color=toolsPanel.GetComponent<Image>().color=battlePanel.GetComponent<Image>().color=Color.clear;
            topPanel.Find("Menu").gameObject.SetActive(false);topPanel.Find("Next").gameObject.SetActive(false);
            foreach(var name in new[]{"Restart","Hint","Auto"})toolsPanel.Find(name).gameObject.SetActive(false);
            levelPlaque=GameplayArt.Texture("LevelWoodPlaque",topPanel,"TideboundUI/Skins/UI_HarborHeader_v1");levelPlaque.transform.SetAsFirstSibling();battlePanel.SetAsFirstSibling();
            title.color=Color.white;title.fontSize=20;title.fontStyle=FontStyle.Bold;
            var titleShadow=title.gameObject.AddComponent<Outline>();titleShadow.effectColor=new Color(.37f,.22f,.1f);titleShadow.effectDistance=new Vector2(.9f,-.9f);
            coin=GameplayArt.Image("WalletCoin",topPanel,"Coin_Anchor");wallet.color=HarborUI.Ink;wallet.fontSize=17;wallet.fontStyle=FontStyle.Bold;
            var pill=HarborUI.Rect("WalletPearl",topPanel).gameObject.AddComponent<GameplayHudGraphic>();pill.Kind=GameplayHudGraphic.Style.Wallet;pill.raycastTarget=false;pill.transform.SetSiblingIndex(coin.transform.GetSiblingIndex());
            coin.transform.SetAsLastSibling();wallet.transform.SetAsLastSibling();
            battleCoins=Label("BattleCoins",topPanel,"",11);battleCoins.verticalOverflow=VerticalWrapMode.Overflow;battleCoins.horizontalOverflow=HorizontalWrapMode.Overflow;battleCoins.color=Color.white;battleCoins.fontStyle=FontStyle.Bold;
            var coinOutline=battleCoins.gameObject.AddComponent<Outline>();coinOutline.effectColor=new Color(.05f,.51f,.58f);coinOutline.effectDistance=new Vector2(.8f,-.8f);
            pause.GetComponentInChildren<Text>(true).enabled=false;pause.targetGraphic.enabled=false;
            var face=HarborUI.Rect("PearlPauseFace",pause.transform).gameObject.AddComponent<GameplayHudGraphic>();face.Kind=GameplayHudGraphic.Style.Pause;GameplayArt.Stretch(face.rectTransform);pause.targetGraphic=face;
            var statusOutline=status.gameObject.AddComponent<Outline>();statusOutline.effectColor=new Color(1,1,.94f,.95f);statusOutline.effectDistance=new Vector2(.6f,-.6f);
            toolBudget=Label("ToolBudget",toolsPanel,"",11);toolBudget.color=HarborUI.Ink;toolBudget.fontStyle=FontStyle.Bold;
            var names=new[]{"Rescue","Shuffle","Reverse"};
            var buttons=new[]{rescueButton,shuffleButton,reverseButton};
            for(var i=0;i<3;i++)
            {
                buttons[i].gameObject.SetActive(true);var label=buttons[i].transform.Find("Label").GetComponent<Text>();
                buttons[i].targetGraphic.enabled=false;
                toolFaces[i]=HarborUI.Rect("ApprovedGoldTool",buttons[i].transform).gameObject.AddComponent<GameplayHudGraphic>();toolFaces[i].Kind=GameplayHudGraphic.Style.Tool;GameplayArt.Stretch(toolFaces[i].rectTransform);toolFaces[i].transform.SetAsFirstSibling();buttons[i].targetGraphic=toolFaces[i];
                toolIcons[i]=GameplayArt.Image("ToolIcon",buttons[i].transform,"Icon_"+names[i]);
                stockBadges[i]=HarborUI.Rect("StockBadge",buttons[i].transform);var countFace=stockBadges[i].gameObject.AddComponent<GameplayHudGraphic>();countFace.Kind=GameplayHudGraphic.Style.Badge;countFace.raycastTarget=false;
                stockLabels[i]=Label("Stock",stockBadges[i],"",15);stockLabels[i].color=Color.white;stockLabels[i].fontStyle=FontStyle.Bold;stockLabels[i].verticalOverflow=VerticalWrapMode.Overflow;GameplayArt.Stretch(stockLabels[i].rectTransform);
                var stockEdge=stockLabels[i].gameObject.AddComponent<Outline>();stockEdge.effectColor=new Color(.58f,.20f,.07f);stockEdge.effectDistance=new Vector2(.6f,-.6f);
                label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=Vector2.zero;
                var textEdge=label.gameObject.AddComponent<Outline>();textEdge.effectColor=HarborUI.Ink;textEdge.effectDistance=new Vector2(1,-1);
                label.color=Color.white;label.fontSize=16;label.verticalOverflow=VerticalWrapMode.Overflow;label.fontStyle=FontStyle.Bold;label.transform.SetAsLastSibling();
            }
        }
        private void LayoutGameplayControls()
        {
            var w=Layout.Top.width;var h=Layout.Top.height;var u=w/390f;var compact=Layout.Compact;
            Place(levelPlaque.rectTransform,new Rect(w/2-84*u,h-(compact?42:49)*u,168*u,(compact?40:47)*u));
            Place(title.rectTransform,new Rect(w/2-66*u,h-(compact?42:46)*u,132*u,30*u));title.fontSize=Mathf.RoundToInt(19*u);
            Place(pause.GetComponent<RectTransform>(),new Rect(12*u,h-43*u,36*u,36*u));
            Place((RectTransform)topPanel.Find("WalletPearl"),new Rect(w-107*u,h-(compact?36:40)*u,96*u,(compact?27:31)*u));
            Place(coin.rectTransform,new Rect(w-110*u,h-(compact?36:41)*u,33*u,(compact?29:34)*u));
            Place(wallet.rectTransform,new Rect(w-80*u,h-(compact?37:40)*u,64*u,28*u));
            Place(battleCoins.rectTransform,new Rect(w-101*u,h-(compact?47:49)*u,94*u,14*u));
            Place(battlePanel,new Rect(0,0,w,h));
            var toolsH=Layout.Tools.height;var bh=(compact?54:64)*u;var bw=72*u;
            Place(status.rectTransform,new Rect(8*u,toolsH-11*u,w-16*u,13*u));status.fontSize=10;status.fontStyle=FontStyle.Bold;status.color=HarborUI.Ink;status.verticalOverflow=VerticalWrapMode.Overflow;status.horizontalOverflow=HorizontalWrapMode.Overflow;
            toolBudget.gameObject.SetActive(false);
            var buttons=new[]{rescueButton,shuffleButton,reverseButton};
            for(var i=0;i<3;i++)
            {
                var center=w*(.18f+i*.32f);
                Place(buttons[i].GetComponent<RectTransform>(),new Rect(center-bw/2,toolsH-13*u-bh,bw,bh));
                var iconH=(compact?34:42)*u;
                GameplayArt.Fit(toolIcons[i],new Rect((bw-iconH)/2,18*u,iconH,iconH),GameplayArt.Get("Icon_"+new[]{"Rescue","Shuffle","Reverse"}[i]).aspect);
                Place(buttons[i].transform.Find("Label").GetComponent<Text>().rectTransform,new Rect(2*u,6*u,bw-4*u,20*u));
                Place(stockBadges[i],new Rect(bw-14*u,bh-12*u,23*u,23*u));
            }
        }

        private void PresentGameplayControls()
        {
            var zh=UILanguage.IsChinese;
            title.text=zh?"第 "+(LevelIndex+1)+" 关":"LEVEL "+(LevelIndex+1);
            wallet.text=(saveService?.Coins??0).ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
            wallet.fontSize=wallet.text.Length>7?13:17;
            battleCoins.text=(zh?"本局 +":"This run +")+(world?.PendingCoins??0);
            toolBudget.text=tools.Enabled?(zh?"本局道具剩余 ":"TOOLS LEFT ")+tools.UsesLeft+"/"+ShipToolSystem.MaxUsesPerAttempt:(zh?"完成教学后开放道具":"Tools unlock after tutorial");
            status.text=IsPaused?(zh?"航行已暂停":"Paused"):tools.Selection!=ShipTool.None?(zh?"点击船只转向，再点道具可取消":"Tap a ship to turn. Tap tool to cancel."):
                progress.NeedsRescue?(zh?"暂时无法驶出，试试道具或重新开始":"No exit. Try a tool or restart."):
                notice=="No proven safe result. No use consumed."?(zh?"本次未找到安全洗牌，道具未消耗":"No safe shuffle found. No tool used."):
                notice=="Tool: Busy"&&IsBusy?(zh?"船只正在移动，请稍后使用道具":"Ship moving. Try the tool in a moment."):
                notice.StartsWith("Out of stock")?(zh?"道具不足，请返回港口补给":"Visit Supplies for more tools"):
                zh?"沿船头前进 · 本局道具 "+tools.UsesLeft+"/"+ShipToolSystem.MaxUsesPerAttempt:"Sail bow first · Tools "+tools.UsesLeft+"/"+ShipToolSystem.MaxUsesPerAttempt;
            var buttons=new[]{rescueButton,shuffleButton,reverseButton};var kinds=new[]{ShipTool.Rescue,ShipTool.Shuffle,ShipTool.Reverse};
            var labels=zh?new[]{"救援","洗牌","转向"}:new[]{"Rescue","Shuffle","Turn"};
            for(var i=0;i<3;i++)
            {
                buttons[i].transform.Find("Label").GetComponent<Text>().text=tools.Selection==kinds[i]?(zh?"取消":"Cancel"):labels[i];
                buttons[i].transform.Find("Label").GetComponent<Text>().fontSize=zh?13:12;
                stockLabels[i].text=tools.Remaining(kinds[i]).ToString();
                var color=buttons[i].interactable?Color.white:new Color(.65f,.72f,.74f,.8f);
                toolFaces[i].color=color;toolIcons[i].color=color;
            }
        }
    }
}
