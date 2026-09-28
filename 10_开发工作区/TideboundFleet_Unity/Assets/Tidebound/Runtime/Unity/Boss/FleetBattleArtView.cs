using System.Linq;
using System.Collections.Generic;
using Tidebound.Core;
using Tidebound.Collection;
using Tidebound.Combat;
using Tidebound.Unity.UI;
using Tidebound.Unity.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.Boss
{
    /// <summary>Paints live combat tokens. No cosmetic callback awards damage, coins, or victory.</summary>
    public sealed class FleetBattleArtView : MonoBehaviour
    {
        private GameSession session;private FleetCombatSystem combat;
        private RectTransform root,track;
        private GameplayHudGraphic health;
        public ComboBattleView Combo {get;private set;}
        private RawImage boss,previousBoss;private Text hp;
        private double stateChangedAt;private string previousState;
        public string BossKind=>bossKind;public string DisplayedState=>bossState;
        public static string KindForLevel(int level)=>new[]{"Octopus","Crab","Manta","Shark","Turtle"}[Mathf.Clamp((level-1)/20,0,4)];
        public static string StateForHealth(float ratio)=>ratio>.60f?"Healthy":ratio>.25f?"Tense":"Weak";
        public static float SideInset(string kind)=>kind=="Crab"?9:kind=="Manta"?8:kind=="Shark"?6:kind=="Turtle"?10:7;
        private readonly RawImage[] ships=new RawImage[5];private readonly Text[] counts=new Text[5];
        private readonly RectTransform[] badges=new RectTransform[5];
        private readonly string[] artIds=new string[5];
        private readonly Dictionary<string,CombatSparkGraphic> shots=new Dictionary<string,CombatSparkGraphic>();
        private string bossKind,bossState;private int defaultSlot;
        private CombatSparkGraphic hitGlow;
        public int ProjectileCount=>shots.Count;
        public int DisplayedDefaultCount {get;private set;}
        public void Initialize(GameSession session,FleetCombatSystem combat,int level)
        {
            this.session=session;this.combat=combat;root=(RectTransform)transform;
            bossKind=KindForLevel(level);
            boss=GameplayArt.Image("RegionalSeaMonster",root,"BossWide/"+bossKind+"_Healthy");
            previousBoss=GameplayArt.Image("PreviousMonsterState",root,"BossWide/"+bossKind+"_Healthy");previousBoss.enabled=false;
            defaultSlot=combat.Fleet.StandardGroups.FirstOrDefault(g=>AppearanceCatalog.VisualSkin(g.SkinId)=="TF_SKIN_K01")?.SlotIndex ??
                Enumerable.Range(0,5).FirstOrDefault(i=>!combat.Fleet.StandardGroups.Any(g=>g.SlotIndex==i));
            for(var i=0;i<5;i++)
            {
                var group=combat.Fleet.StandardGroups.FirstOrDefault(g=>g.SlotIndex==i);
                var id=group==null?(i==defaultSlot?"K01":null):AppearanceCatalog.VisualSkin(group.SkinId).Replace("TF_SKIN_","");
                artIds[i]=id==null?null:"Side/"+id;
                if(id!=null)
                {
                    ships[i]=GameplayArt.Image("FleetShip"+i,root,artIds[i]);
                    if(ships[i].texture==null)
                    {
                        // Two purchased skins lack side art. Their own normalized top art is a truthful fallback.
                        var record=HarborAppearanceArt.Skin(group.SkinId);ships[i].texture=Resources.Load<Texture2D>(record.resourcePath);
                        ships[i].uvRect=HarborAppearanceArt.UV(record.uv);
                    }
                    else GameplayArt.Set(ships[i],artIds[i],i>=3);
                }
                var plate=HarborUI.Surface("FleetCount"+i,root,new Color(.015f,.29f,.39f,.8f));plate.raycastTarget=false;((HarborImage)plate).Border=0;badges[i]=plate.rectTransform;
                counts[i]=Text("Count",badges[i],11,Color.white);GameplayArt.Stretch(counts[i].rectTransform);
            }
            hitGlow=HarborUI.Rect("HitSplash",root).gameObject.AddComponent<CombatSparkGraphic>();hitGlow.Hit=true;hitGlow.raycastTarget=false;hitGlow.enabled=false;
            track=HarborUI.Rect("BossHealthFrame",root);health=track.gameObject.AddComponent<GameplayHudGraphic>();health.Kind=GameplayHudGraphic.Style.Health;health.raycastTarget=false;
            var comboRoot=HarborUI.Rect("ComboPresentation",root);GameplayArt.Stretch(comboRoot);
            Combo=comboRoot.gameObject.AddComponent<ComboBattleView>();Combo.Initialize(session,combat,defaultSlot);
            hp=Text("BossHealthText",track,12,Color.white);hp.fontStyle=FontStyle.Bold;GameplayArt.Stretch(hp.rectTransform);
            var outline=hp.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.3f,.04f,.13f,.85f);outline.effectDistance=new Vector2(.6f,-.6f);
        }
        public void Present()
        {
            var w=root.rect.width;var h=root.rect.height;var sw=w/5;
            var layout=new GameplayBattleLayout(w,h);var u=layout.Unit;
            Combo.Present();
            var ratio=(float)session.Boss.Hp/session.Boss.InitialHp;
            var state=StateForHealth(ratio);
            if(state!=bossState)
            {
                previousState=bossState;previousBoss.texture=boss.texture;previousBoss.uvRect=boss.uvRect;
                bossState=state;stateChangedAt=combat.Time;GameplayArt.Set(boss,"BossWide/"+bossKind+"_"+state);
            }
            var blend=previousState==null?1:Mathf.Clamp01((float)(combat.Time-stateChangedAt)/.18f);
            var recent=(float)(combat.Time-combat.LastHitAt);var strike=recent>=0&&recent<.24f;
            var pulse=strike?Mathf.Sin(recent*25)*(1-recent/.24f):0;
            PresentBoss(boss,bossState,layout,blend,pulse);
            previousBoss.enabled=blend<1;
            if(previousBoss.enabled)PresentBoss(previousBoss,previousState,layout,1-blend,pulse);
            Set(track,layout.Health);health.Fraction=ratio;health.SetVerticesDirty();
            var label=UILanguage.IsChinese?new[]{"绯潮章鱼王","珊瑚巨钳蟹","雷鳍蝠鲼王","锤头鲨霸主","礁岩龙龟"}[Mathf.Clamp(System.Array.IndexOf(new[]{"Octopus","Crab","Manta","Shark","Turtle"},bossKind),0,4)]:bossKind;
            hp.text=label+"  "+session.Boss.Hp+" / "+session.Boss.InitialHp;hp.fontSize=Mathf.RoundToInt((UILanguage.IsChinese?10.5f:10)*u);
            hp.rectTransform.offsetMin=Vector2.zero;hp.rectTransform.offsetMax=Vector2.zero;
            var bossBounds=layout.Boss(SideInset(bossKind),GameplayArt.Get("BossWide/"+bossKind+"_"+bossState).aspect);
            var uv=bossKind=="Octopus"?new Vector2(.57f,.54f):bossKind=="Shark"?new Vector2(.73f,.61f):bossKind=="Turtle"?new Vector2(.63f,.66f):new Vector2(.5f,.6f);
            var impact=new Vector2(bossBounds.x+bossBounds.width*uv.x,bossBounds.y+bossBounds.height*(1-uv.y));
            for(var i=0;i<5;i++)
            {
                var group=combat.Fleet.StandardGroups.FirstOrDefault(g=>g.SlotIndex==i);var n=group?.ArrivedCount??0;
                var isDefault=group==null||AppearanceCatalog.VisualSkin(group.SkinId)=="TF_SKIN_K01";
                if(i==defaultSlot && isDefault)n+=combat.Fleet.Support.ArrivedCount;
                if(i==defaultSlot)DisplayedDefaultCount=isDefault?n:combat.Fleet.Support.ArrivedCount;
                counts[i].text=artIds[i]==null?(UILanguage.IsChinese?"未装备":"Empty"):"× "+n;
                if(i==defaultSlot&&!isDefault&&combat.Fleet.Support.ArrivedCount>0)counts[i].text+=" +A01×"+combat.Fleet.Support.ArrivedCount;
                Set(badges[i],layout.Count(i));counts[i].fontSize=Mathf.RoundToInt(10*u);
                if(ships[i]!=null)
                {
                    var a=GameplayArt.Get(artIds[i]);GameplayArt.Fit(ships[i],layout.Fleet(i),a?.aspect??.6f);
                    ships[i].rectTransform.anchoredPosition+=new Vector2(0,Mathf.Sin((float)combat.Time*1.8f+i)*.6f*u);
                    ships[i].color=new Color(1,1,1,n>0?1:.9f);
                }
            }
            foreach(var token in combat.Attacks)
            {
                if(token.Stage!=AttackStage.InFlight)continue;
                if(!shots.TryGetValue(token.AttackId,out var shot))
                {shot=HarborUI.Rect("CannonShot_"+token.Ship.ShipId,root).gameObject.AddComponent<CombatSparkGraphic>();shot.raycastTarget=false;shots.Add(token.AttackId,shot);}
                var slot=token.Group.IsSupport?defaultSlot:token.Group.SlotIndex;
                var start=new Vector2((slot+.5f)*sw,layout.FleetBottom+layout.FleetHeight*.75f);var end=impact;
                var t=token.FlightProgress(combat.Time);var p=Vector2.Lerp(start,end,t)+Vector2.up*Mathf.Sin(t*Mathf.PI)*16;
                var tier=Combo.State.TierFor(token.Ship.ShipId);var size=new Vector2(10+tier*3,20+tier*5);
                Set(shot.rectTransform,new Rect(p-size/2,size));shot.Tier=tier;shot.Phase=(float)combat.Time;shot.SetVerticesDirty();
                shot.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg-90);
            }
            var active=combat.Attacks.Where(t=>t.Stage==AttackStage.InFlight).Select(t=>t.AttackId).ToHashSet();
            foreach(var key in shots.Keys.Where(k=>!active.Contains(k)).ToArray()){Destroy(shots[key].gameObject);shots.Remove(key);}
            hitGlow.enabled=strike;if(strike){var size=(26+Combo.LastHitTier*5)*u;Set(hitGlow.rectTransform,new Rect(impact-Vector2.one*size/2,Vector2.one*size));hitGlow.Tier=Combo.LastHitTier;hitGlow.Phase=recent;hitGlow.color=new Color(1,1,.7f,1-recent/.24f);hitGlow.SetVerticesDirty();}
        }
        private void PresentBoss(RawImage image,string state,GameplayBattleLayout layout,float alpha,float pulse)
        {
            var bounds=layout.Boss(SideInset(bossKind),GameplayArt.Get("BossWide/"+bossKind+"_"+state).aspect);
            Set(image.rectTransform,bounds);
            // Rotate around the center, keeping the shared bottom waterline during state changes.
            image.rectTransform.pivot=Vector2.one*.5f;image.rectTransform.anchoredPosition=bounds.center+Vector2.up*Mathf.Sin((float)combat.Time*1.7f)*.6f*layout.Unit;
            image.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Sin((float)combat.Time*1.2f)*.34f);
            image.rectTransform.localScale=new Vector3(1+pulse*.012f,1-pulse*.012f,1);
            image.color=new Color(1,1,1,alpha);
        }
        private static Text Text(string name,Transform parent,int size,Color color)
        {var t=HarborUI.Rect(name,parent).gameObject.AddComponent<HarborText>();t.font=HarborUI.Font;t.fontSize=size;t.color=color;t.verticalOverflow=VerticalWrapMode.Overflow;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        private static void Set(RectTransform r,Rect b)=>HarborUI.Place(r,b);
    }
}
