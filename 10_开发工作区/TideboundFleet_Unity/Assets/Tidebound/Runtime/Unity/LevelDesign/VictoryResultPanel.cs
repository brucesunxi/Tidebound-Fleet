using System;
using Tidebound.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed class VictoryResultPanel : MonoBehaviour
    {
        private Font font;
        private Text title, subtitle, first, battle, total, chapter, target;
        private RectTransform flagship, track, fill;
        private Button next, back, retry, motion;
        private bool overview, homeNavigation;
        public bool IsOverview => overview;
        public void Initialize(Font value,Action proceed,Action returnOrResult,Action retrySave,Action toggleMotion,bool homeNavigation=false)
        {
            this.homeNavigation=homeNavigation;
            font=value;title=Label("ResultTitle",28);subtitle=Label("ResultSubtitle",16);
            flagship=Rect("Flagship",transform);var hull=Box("Hull",flagship,new Color(.22f,.62f,.75f));
            Place(hull,new Rect(10,0,60,20));Place(Box("Deck",flagship,new Color(.7f,.88f,.91f)),new Rect(28,20,24,14));
            Place(Box("Mast",flagship,Color.white),new Rect(40,34,3,20));Place(Box("Flag",flagship,new Color(1,.78f,.22f)),new Rect(43,42,15,12));
            first=Label("FirstClear",18);battle=Label("BattleCoins",18);total=Label("TotalCoins",24);
            chapter=Label("Chapter",18);target=Label("NextTarget",16);
            track=Box("ProgressTrack",transform,new Color(.08f,.20f,.27f));fill=Box("ProgressFill",track,new Color(.22f,.76f,.78f));
            next=Button("NextLevel",proceed);back=Button("BackOrResult",returnOrResult);retry=Button("RetryResultSave",retrySave);
            motion=Button("ResultMotion",toggleMotion);
        }
        public void Present(VictoryResult value,bool showOverview,bool readable,float progress,bool reduced)
        {
            overview=showOverview;
            title.text=overview ? "Voyage progress" : "VICTORY!";
            subtitle.text="Level "+value.LevelNumber+" cleared"+(overview ? "" : " - rewards saved");
            first.text="First clear     +"+value.FirstClearCoins; battle.text="Battle coins     +"+value.BattleCoins;
            total.text="Total     +"+value.TotalCoins;
            first.gameObject.SetActive(!overview);battle.gameObject.SetActive(!overview);total.gameObject.SetActive(!overview);
            flagship.gameObject.SetActive(true);chapter.gameObject.SetActive(!homeNavigation);track.gameObject.SetActive(!homeNavigation);target.gameObject.SetActive(showOverview || progress>=value.Progress-.00001f);
            chapter.text="Chapter "+value.ChapterNumber+"    "+value.ChapterCompleted+" / "+value.ChapterSize;
            target.text=value.HasNext ? "Next destination: Level "+value.NextLevel : "All available levels complete";
            next.gameObject.SetActive(true);next.interactable=readable && (homeNavigation || value.HasNext);
            next.GetComponentInChildren<Text>().text=homeNavigation || value.HasNext ? "Next level" : "Content complete";
            back.gameObject.SetActive(true);back.interactable=readable;
            back.GetComponentInChildren<Text>().text=homeNavigation ? "Return home" : overview ? "View result" : "Return to voyage";
            retry.gameObject.SetActive(false);motion.gameObject.SetActive(true);
            motion.GetComponentInChildren<Text>().text="Motion: "+(reduced ? "Reduced" : "Full");
            Place(fill,new Rect(0,0,track.rect.width*Mathf.Clamp01(progress),12));
        }
        public void Pending(bool failed)
        {
            overview=false;title.text="VICTORY";subtitle.text=failed ? "Reward save failed. Please retry." : "Saving your rewards...";
            first.gameObject.SetActive(false);battle.gameObject.SetActive(false);total.gameObject.SetActive(false);
            flagship.gameObject.SetActive(true);chapter.gameObject.SetActive(false);track.gameObject.SetActive(false);target.gameObject.SetActive(false);
            next.gameObject.SetActive(false);back.gameObject.SetActive(false);motion.gameObject.SetActive(false);
            retry.gameObject.SetActive(failed);retry.GetComponentInChildren<Text>().text="Retry saving rewards";
        }
        public void Practice()
        {
            Pending(false);title.text="Practice complete";subtitle.text="Account unavailable - no rewards saved";
            next.gameObject.SetActive(true);next.interactable=true;next.GetComponentInChildren<Text>().text="Replay practice";
            back.gameObject.SetActive(homeNavigation);back.interactable=true;back.GetComponentInChildren<Text>().text="Return home";
        }
        public void SetNotice(string value) => subtitle.text=value;
        public void Layout(Rect area)
        {
            Place((RectTransform)transform,area);var w=area.width;var y=area.height/2;
            Place(title.rectTransform,new Rect(16,y+210,w-32,40));Place(subtitle.rectTransform,new Rect(16,y+161,w-32,44));
            Place(flagship,new Rect((w-80)/2,y+89,80,54));
            Place(first.rectTransform,new Rect(20,y+40,w-40,32));Place(battle.rectTransform,new Rect(20,y+4,w-40,32));
            Place(total.rectTransform,new Rect(20,y-42,w-40,40));Place(chapter.rectTransform,new Rect(20,y-90,w-40,32));
            Place(track,new Rect(28,y-112,w-56,12));Place(target.rectTransform,new Rect(16,y-156,w-32,36));
            Place((RectTransform)next.transform,new Rect(24,y-218,w-48,48));Place((RectTransform)back.transform,new Rect(24,y-274,w-48,48));
            Place((RectTransform)motion.transform,new Rect(w-136,8,128,48));Place((RectTransform)retry.transform,new Rect(24,y-86,w-48,48));
        }
        private static RectTransform Rect(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;return r;}
        private static RectTransform Box(string name,Transform parent,Color color)
        {var r=Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
        private Text Label(string name,int size)
        {var text=Rect(name,transform).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;}
        private Button Button(string name,Action action)
        {
            var rect=Box(name,transform,new Color(.13f,.30f,.39f));rect.GetComponent<Image>().raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener(()=>action());
            var text=Label(name+"Label",16);text.transform.SetParent(rect,false);text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;return button;
        }
        private static void Place(RectTransform r,Rect rect){r.anchoredPosition=rect.position;r.sizeDelta=rect.size;}
    }
}
