using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>The approved compact settings and pause layouts, bound to existing navigation.</summary>
    public sealed class HarborSettingsPanel : MonoBehaviour
    {
        private bool inGame;
        private HarborAudio audioService;
        private Action close;
        private RectTransform content, frame, titlePlate, legalRoot, legalFrame, legalContent;
        private Text title, notice, legalTitle, legalBody;
        private ScrollRect legalScroll;
        private Button closeButton, legalBack;
        private Image musicTrack, soundTrack;
        private RectTransform musicThumb, soundThumb;
        private Vector2 size;
        public Button MusicButton { get; private set; }
        public Button SoundButton { get; private set; }
        public Button LanguageChinese { get; private set; }
        public Button LanguageEnglish { get; private set; }
        public Button RestartButton { get; private set; }
        public Button ExitButton { get; private set; }
        public Button ContinueButton { get; private set; }
        public Button SkipButton { get; private set; }
        public Button PrivacyButton { get; private set; }
        public Button TermsButton { get; private set; }
        public bool IsLegalOpen => legalRoot != null && legalRoot.gameObject.activeSelf;
        public string LegalText => legalBody != null ? legalBody.text : "";
        private bool privacy;
        private Button legalOrigin;

        public void Initialize(bool paused, HarborAudio audio, Action onClose, Action restart = null, Action exit = null)
        {
            inGame = paused; audioService = audio; close = onClose;
            var shade = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            shade.color = new Color(.015f,.11f,.18f,paused?.67f:.44f); shade.raycastTarget = true;
            if(shade is HarborImage old) { old.Radius=0;old.Border=0;old.Depth=0; }
            content=HarborUI.Rect("ApprovedSettings",transform);
            frame=HarborApprovedArt.Create("Frame","Panel_Gold",content).rectTransform;
            titlePlate=HarborUI.Header("Header",content);
            title=HarborApprovedArt.Text("Title",content,paused?"Pause":"Settings",27,Color.white);
            closeButton=MakeClose("Close",transform,()=>close?.Invoke());
            MusicButton=AudioRow("Music",content,SettingsIconKind.Music,()=>audioService.SetMusic(!audioService.MusicEnabled),out musicTrack,out musicThumb);
            SoundButton=AudioRow("Sound",content,SettingsIconKind.Sound,()=>audioService.SetSound(!audioService.SoundEnabled),out soundTrack,out soundThumb);
            if(!inGame)
            {
                var language=Flat("LanguageRow",content,new Color(.89f,.95f,.97f));
                var label=HarborApprovedArt.Text("LanguageLabel",language.transform,"Language",20,HarborApprovedArt.Ink);label.alignment=TextAnchor.MiddleLeft;
                LanguageChinese=SmallButton("LanguageChinese",language.transform,"中文",()=>UILanguage.SetPreference(LanguagePreference.Chinese));
                LanguageEnglish=SmallButton("LanguageEnglish",language.transform,"EN",()=>UILanguage.SetPreference(LanguagePreference.English));
                PrivacyButton=Link("Privacy",content,"Privacy Policy",()=>OpenLegal(true));
                TermsButton=Link("Terms",content,"Terms of Use",()=>OpenLegal(false));
                Flat("LegalDivider",content,new Color(.74f,.78f,.71f));
                BuildLegal();
            }
            else
            {
                ExitButton=ActionButton("Exit",content,"Exit game",new Color(.17f,.68f,.88f),SettingsIconKind.Home,exit);
                RestartButton=ActionButton("Restart",content,"Restart level",new Color(1,.78f,.14f),null,restart);
                ContinueButton=ActionButton("Continue",content,"Continue game",new Color(.18f,.74f,.34f),null,onClose);
                SkipButton=ActionButton("Skip",content,"Skip level",new Color(.98f,.5f,.18f),SettingsIconKind.Skip,null);
                SkipButton.interactable=false;
                notice=HarborApprovedArt.Text("Unavailable",content,"Available when ads are ready",12,new Color(1,.96f,.77f));
            }
            audioService.Changed+=Present;UILanguage.Changed+=Present;
            Present();
        }
        public void Layout(Rect safe)
        {
            HarborUI.Place((RectTransform)transform,safe);size=safe.size;
            var scale=Mathf.Min(1,size.x/390f);content.localScale=Vector3.one*scale;
            var h=size.y/scale;HarborUI.Place(content,new Rect((size.x-390*scale)/2,0,390,h));
            var height=inGame?238f:328f;var y=inGame?(h-454)/2+55:(h-328)/2+22;
            Top(frame,new Rect(26,y,338,height),h);Top(titlePlate,new Rect(48,y-65.4f,294,98),h);
            Top(title.rectTransform,new Rect(83,y-23,224,50),h);
            // Close is a direct child for existing focus/navigation callers; match the content transform.
            HarborUI.Place((RectTransform)closeButton.transform,new Rect(content.anchoredPosition.x+325*scale,(h-y-32)*scale,48*scale,48*scale));
            Top((RectTransform)MusicButton.transform,new Rect(48,y+(inGame?65:126),294,64),h);
            Top((RectTransform)SoundButton.transform,new Rect(48,y+(inGame?143:200),294,64),h);
            if(!inGame)
            {
                var row=(RectTransform)content.Find("LanguageRow");Top(row,new Rect(48,y+56,294,57),h);
                HarborUI.Place((RectTransform)row.Find("LanguageLabel"),new Rect(20,0,112,57));
                HarborUI.Place((RectTransform)LanguageChinese.transform,new Rect(136,9,63,38));
                HarborUI.Place((RectTransform)LanguageEnglish.transform,new Rect(208,9,63,38));
                Top((RectTransform)PrivacyButton.transform,new Rect(78,y+271,99,48),h);
                Top((RectTransform)TermsButton.transform,new Rect(213,y+271,103,48),h);
                Top((RectTransform)content.Find("LegalDivider"),new Rect(194.5f,y+286,1,21),h);
                LayoutLegal();
            }
            else
            {
                var by=y+266;Top((RectTransform)ExitButton.transform,new Rect(32,by,69,67),h);
                Top((RectTransform)RestartButton.transform,new Rect(113,by,164,67),h);
                Top((RectTransform)SkipButton.transform,new Rect(289,by,69,67),h);
                Top((RectTransform)ContinueButton.transform,new Rect(113,by+91,164,59),h);
                Top(notice.rectTransform,new Rect(26,by+163,338,30),h);
            }
        }
        private static void Top(RectTransform r,Rect area,float height) => HarborUI.Place(r,new Rect(area.x,height-area.y-area.height,area.width,area.height));
        public void Present()
        {
            if(audioService==null || MusicButton==null)return;
            PaintToggle(musicTrack,musicThumb,audioService.MusicEnabled);PaintToggle(soundTrack,soundThumb,audioService.SoundEnabled);
            if(LanguageChinese!=null)
            {
                LanguageChinese.GetComponent<HarborImage>().color=UILanguage.IsChinese?HarborUI.Gold:Color.white;
                LanguageEnglish.GetComponent<HarborImage>().color=UILanguage.IsChinese?Color.white:HarborUI.Gold;
            }
            foreach(var button in new[]{RestartButton,ContinueButton})if(button!=null)button.GetComponentInChildren<Text>().fontSize=UILanguage.IsChinese?21:18;
            if(PrivacyButton!=null){PrivacyButton.GetComponentInChildren<Text>().fontSize=UILanguage.IsChinese?14:12;TermsButton.GetComponentInChildren<Text>().fontSize=UILanguage.IsChinese?14:12;}
            if(IsLegalOpen)LoadLegal();
        }
        public void ShowSaveRetry(bool failed)
        {if(notice!=null)notice.text=failed?"Unable to save. Please retry.":"Available when ads are ready";}
        public void SetActionsAvailable(bool restart,bool exit)
        {if(RestartButton)RestartButton.interactable=restart;if(ExitButton)ExitButton.interactable=exit;}
        private void PaintToggle(Image rail,RectTransform thumb,bool value)
        {
            rail.color=value?new Color(.82f,.92f,.86f):new Color(.73f,.78f,.77f);
            HarborUI.Place(thumb,new Rect(value?44:2,1,34,34));thumb.GetComponent<HarborImage>().color=value?HarborUI.Gold:new Color(.8f,.85f,.81f);
        }
        private Button AudioRow(string name,Transform parent,SettingsIconKind kind,Action action,out Image rail,out RectTransform thumb)
        {
            var root=Flat(name,parent,new Color(1,.99f,.96f));
            var icon=HarborUI.Rect("Icon",root.transform).gameObject.AddComponent<HarborSettingsIcon>();icon.Kind=kind;icon.color=new Color(1,.78f,.2f);icon.raycastTarget=false;var edge=icon.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.66f,.43f,.13f);edge.effectDistance=new Vector2(1,-1);
            HarborUI.Place(icon.rectTransform,new Rect(12,17,31,31));
            var label=HarborApprovedArt.Text("Label",root.transform,name,20,HarborApprovedArt.Ink);label.alignment=TextAnchor.MiddleLeft;HarborUI.Place(label.rectTransform,new Rect(58,0,128,64));
            rail=Flat("Track",root.transform,new Color(.82f,.92f,.86f));((HarborImage)rail).Radius=19;((HarborImage)rail).Border=1;((HarborImage)rail).Depth=3;((HarborImage)rail).Edge=new Color(.7f,.51f,.28f);
            HarborUI.Place(rail.rectTransform,new Rect(199,13,80,38));
            var handle=Flat("Thumb",rail.transform,HarborUI.Gold);((HarborImage)handle).Radius=12;((HarborImage)handle).Border=2;((HarborImage)handle).Depth=3;((HarborImage)handle).Edge=new Color(1,.98f,.85f);thumb=handle.rectTransform;
            root.raycastTarget=true;var b=root.gameObject.AddComponent<Button>();b.targetGraphic=root;b.transition=Selectable.Transition.None;
            b.onClick.AddListener(()=>{action();});return b;
        }
        private static Image Flat(string name,Transform parent,Color color)
        {var image=HarborUI.Surface(name,parent,color);image.raycastTarget=false;var art=(HarborImage)image;art.Radius=15;art.Border=0;art.Kind=HarborSurfaceKind.Control;return image;}
        private Button SmallButton(string name,Transform parent,string label,Action action)
        {var face=Flat(name,parent,Color.white);face.raycastTarget=true;var t=HarborApprovedArt.Text("Label",face.transform,label,18,HarborApprovedArt.Ink);HarborUI.Fill(t.rectTransform);var b=face.gameObject.AddComponent<Button>();b.targetGraphic=face;b.onClick.AddListener(()=>{audioService.Click();action();});return b;}
        private Button Link(string name,Transform parent,string label,Action action)
        {
            var b=SmallButton(name,parent,label,action);b.GetComponent<Image>().color=Color.clear;
            var text=b.GetComponentInChildren<Text>();text.color=new Color(.3f,.46f,.52f);text.fontSize=UILanguage.IsChinese?14:12;
            var line=Flat("Underline",b.transform,text.color);HarborUI.Place(line.rectTransform,new Rect(19,11,62,1));return b;
        }
        private Button ActionButton(string name,Transform parent,string label,Color color,SettingsIconKind? kind,Action action)
        {
            var root=HarborUI.Rect(name,parent);root.gameObject.SetActive(false);var face=HarborUI.Rect("Face",root);HarborUI.Fill(face);
            var art=Flat("Art",face,color);HarborUI.Fill(art.rectTransform);var skin=(HarborImage)art;skin.Radius=16;skin.Border=3;skin.Depth=6;skin.Edge=new Color(1,.92f,.68f);
            var gloss=Flat("Gloss",face,new Color(1,1,1,.35f));((HarborImage)gloss).Radius=4;
            gloss.rectTransform.anchorMin=new Vector2(0,1);gloss.rectTransform.anchorMax=new Vector2(1,1);gloss.rectTransform.pivot=new Vector2(.5f,1);
            gloss.rectTransform.anchoredPosition=new Vector2(0,-7);gloss.rectTransform.sizeDelta=new Vector2(-16,7);
            var text=HarborApprovedArt.Text("Label",face,label,kind.HasValue?12:21,Color.white);var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=Color.Lerp(color,Color.black,.6f);outline.effectDistance=new Vector2(1,-1);
            if(kind.HasValue){var icon=HarborUI.Rect("Icon",face).gameObject.AddComponent<HarborSettingsIcon>();icon.Kind=kind.Value;icon.color=Color.white;icon.raycastTarget=false;HarborUI.Place(icon.rectTransform,new Rect(19,26,31,31));HarborUI.Place(text.rectTransform,new Rect(1,7,67,19));}
            else HarborUI.Fill(text.rectTransform,5);
            var button=HarborApprovedArt.BindButton(root,face,art,action==null?(Action)null:()=>{audioService.Click();action();});root.gameObject.SetActive(true);return button;
        }
        private Button MakeClose(string name,Transform parent,Action action)
        {
            var root=HarborUI.Rect(name,parent);var face=HarborUI.Rect("Face",root);HarborUI.Fill(face);
            var art=HarborApprovedArt.Create("Art","Close_Coral",face);HarborUI.Fill(art.rectTransform);
            var label=HarborApprovedArt.Text("Label",face,"×",28,Color.white);HarborUI.Fill(label.rectTransform);
            return HarborApprovedArt.BindButton(root,face,art,()=>{audioService.Click();action();});
        }
        private void BuildLegal()
        {
            legalRoot=HarborUI.Rect("LegalReader",transform);HarborUI.Fill(legalRoot);var blocker=legalRoot.gameObject.AddComponent<Image>();blocker.color=new Color(.01f,.09f,.14f,.78f);
            legalFrame=HarborApprovedArt.Create("Panel","Panel_Gold",legalRoot).rectTransform;
            legalTitle=HarborApprovedArt.Text("Title",legalRoot,"Privacy Policy",24,HarborApprovedArt.Ink);
            legalScroll=HarborUI.Scroll("Scroll",legalRoot,out legalContent);
            legalBody=HarborUI.Label("Body",legalContent,"",15);legalBody.alignment=TextAnchor.UpperLeft;legalBody.verticalOverflow=VerticalWrapMode.Overflow;legalBody.horizontalOverflow=HorizontalWrapMode.Wrap;
            var shadow=legalBody.GetComponent<Shadow>();if(shadow)Destroy(shadow);
            legalBack=ActionButton("Back",legalRoot,"Back",HarborUI.Gold,null,CloseLegal);legalRoot.gameObject.SetActive(false);
        }
        public void OpenLegal(bool isPrivacy)
        {privacy=isPrivacy;legalOrigin=isPrivacy?PrivacyButton:TermsButton;legalRoot.gameObject.SetActive(true);legalRoot.SetAsLastSibling();content.gameObject.SetActive(false);closeButton.gameObject.SetActive(false);LoadLegal();legalScroll.verticalNormalizedPosition=1;HarborUI.Focus(legalBack);}
        public void CloseLegal()
        {legalRoot.gameObject.SetActive(false);content.gameObject.SetActive(true);closeButton.gameObject.SetActive(true);HarborUI.Focus(legalOrigin);}
        private void LoadLegal()
        {
            legalTitle.text=privacy?"Privacy Policy":"Terms of Use";
            var asset=Resources.Load<TextAsset>("TideboundUI/Legal/"+(privacy?"Privacy":"Terms")+(UILanguage.IsChinese?"_zh":"_en"));
            legalBody.text=asset!=null?asset.text:"Document unavailable.";LayoutLegal();
        }
        private void LayoutLegal()
        {
            if(legalRoot==null)return;var w=size.x;var h=size.y;HarborUI.Place(legalFrame,new Rect(18,24,w-36,h-48));
            HarborUI.Place(legalTitle.rectTransform,new Rect(38,h-99,w-76,48));
            HarborUI.Place(legalScroll.GetComponent<RectTransform>(),new Rect(42,106,w-84,h-218));
            legalBody.fontSize=15;legalContent.sizeDelta=new Vector2(w-98,1);HarborUI.Place(legalBody.rectTransform,new Rect(0,0,w-98,1));
            var needed=Mathf.Max(h-218,legalBody.preferredHeight+20);legalContent.sizeDelta=new Vector2(w-98,needed);
            legalBody.rectTransform.anchorMin=legalBody.rectTransform.anchorMax=legalBody.rectTransform.pivot=new Vector2(0,1);legalBody.rectTransform.anchoredPosition=Vector2.zero;legalBody.rectTransform.sizeDelta=new Vector2(w-98,needed);
            HarborUI.Place((RectTransform)legalBack.transform,new Rect((w-230)/2,44,230,48));
        }
        private void OnEnable(){Present();}
        private void Update(){if(UnityEngine.Input.GetKeyDown(KeyCode.Escape)){if(IsLegalOpen)CloseLegal();else close?.Invoke();}}
        private void OnDisable(){if(legalRoot!=null&&IsLegalOpen)CloseLegal();}
        private void OnDestroy(){if(audioService!=null)audioService.Changed-=Present;UILanguage.Changed-=Present;}
    }
}
