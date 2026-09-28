using System;
using System.Linq;
using Tidebound.Combat;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Unity.UI;
using Tidebound.Unity.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.Boss
{
    /// <summary>Approved v3 flame presentation. Uses the paused transit clock; all images ignore input.</summary>
    public sealed class ComboBattleView : MonoBehaviour
    {
        public ExitComboState State {get;private set;}
        public int LastHitTier {get;private set;}
        public double AnimationTime=>combat?.Time??0;
        private GameSession session;private FleetCombatSystem combat;
        private IDisposable exitSubscription,endSubscription,hitSubscription;
        private RectTransform badge,timer,remaining;private Text label;
        private HarborVictoryGraphic star;private HarborEmblem anchor;
        private ComboEmberGraphic embers;
        private RawImage fire;private readonly RawImage[] hulls=new RawImage[5];
        private Material bannerMaterial,hullMaterial;
        private int previousTier,defaultSlot;private bool ended;
        public void Initialize(GameSession game,FleetCombatSystem battle,int supportSlot=0)
        {
            session=game;combat=battle;defaultSlot=supportSlot;State=new ExitComboState(game.SessionId);
            var shader=Resources.Load<Shader>("TideboundUI/ComboFlame");
            bannerMaterial=new Material(shader);hullMaterial=new Material(shader);hullMaterial.SetFloat("_Mode",1);
            for(var i=0;i<5;i++)hulls[i]=Flame("FleetFlame"+i,hullMaterial);
            fire=Flame("ComboFlame",bannerMaterial);
            embers=HarborUI.Rect("FloatingEmbers",transform).gameObject.AddComponent<ComboEmberGraphic>();GameplayArt.Stretch(embers.rectTransform);embers.raycastTarget=false;
            badge=HarborUI.Rect("ComboBadge",transform);
            var face=badge.gameObject.AddComponent<GameplayHudGraphic>();face.Kind=GameplayHudGraphic.Style.Combo;face.raycastTarget=false;
            label=HarborUI.Label("ComboCount",badge,"",19);label.verticalOverflow=VerticalWrapMode.Overflow;label.horizontalOverflow=HorizontalWrapMode.Overflow;label.color=new Color(1,1,.87f);label.fontStyle=FontStyle.Bold;GameplayArt.Stretch(label.rectTransform);
            var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.54f,.20f,.04f);outline.effectDistance=new Vector2(.75f,-.75f);
            timer=HarborUI.Rect("ComboTimer",transform);var background=timer.gameObject.AddComponent<HarborImage>();background.color=new Color(.16f,.24f,.24f,.65f);background.Border=0;background.Radius=3;background.raycastTarget=false;
            remaining=HarborUI.Rect("Remaining",timer);var line=remaining.gameObject.AddComponent<HarborImage>();line.Border=0;line.Radius=2;line.color=new Color(1,.92f,.47f);line.raycastTarget=false;
            star=HarborUI.Rect("RotatingComboStar",transform).gameObject.AddComponent<HarborVictoryGraphic>();star.raycastTarget=false;star.rectTransform.pivot=Vector2.one*.5f;
            anchor=HarborUI.Rect("SwingingComboAnchor",transform).gameObject.AddComponent<HarborEmblem>();anchor.AnchorOnly=true;anchor.raycastTarget=false;anchor.rectTransform.pivot=Vector2.one*.5f;
            // These layers paint behind the actual hull images, never over the ship silhouette.
            var fleetFire=HarborUI.Rect("FleetFireLayer",transform.parent);GameplayArt.Stretch(fleetFire);fleetFire.SetSiblingIndex(2);
            foreach(var hull in hulls)hull.transform.SetParent(fleetFire,false);
            embers.transform.SetParent(fleetFire,false);GameplayArt.Stretch(embers.rectTransform);
            exitSubscription=game.Events.Subscribe<ShipExitBoardEvent>(e=>{
                if(game.TryGetShip(e.Ship.ShipId,out var ship) && !game.Board.TryGetShip(ship.Id,out _) && State.Record(e,battle.Time))previousTier=State.Tier;
            });
            hitSubscription=game.Events.Subscribe<BossDamagedEvent>(e=>{var token=battle.Attacks.FirstOrDefault(a=>a.AttackId==e.AttackId);LastHitTier=token==null?0:State.TierFor(token.Ship.ShipId);});
            endSubscription=game.Events.Subscribe<AttemptEndedEvent>(e=>{if(e.SessionId==game.SessionId){State.End();ended=true;Present();}});
            Present();
        }
        private RawImage Flame(string name,Material material)
        {var image=HarborUI.Rect(name,transform).gameObject.AddComponent<RawImage>();image.texture=Texture2D.whiteTexture;image.material=material;image.raycastTarget=false;return image;}
        public void Present()
        {
            if(State==null)return;
            State.Advance(combat.Time);
            var r=((RectTransform)transform).rect;var w=r.width;var sw=w/5;var time=(float)combat.Time;
            var layout=new GameplayBattleLayout(w,r.height);var u=layout.Unit;
            var decay=State.Count==0?Mathf.Clamp01(1-(float)(combat.Time-State.LastExitAt-ExitComboState.Window)/.25f):1;
            var tier=State.Tier>0?State.Tier:previousTier;
            var opacity=ended?0:(State.Count>0&&State.Tier==0?0:decay);
            foreach(var material in new[]{bannerMaterial,hullMaterial}){material.SetFloat("_FlameTime",time);material.SetFloat("_Strength",tier);material.SetFloat("_Opacity",opacity);}
            fire.gameObject.SetActive(tier>0&&opacity>0);HarborUI.Place(fire.rectTransform,new Rect(layout.Combo.x-7*u,layout.Combo.y+10*u,layout.Combo.width+14*u,70*u));
            badge.gameObject.SetActive(State.Tier>0&&!ended);timer.gameObject.SetActive(State.Tier>0&&!ended);
            star.gameObject.SetActive(State.Tier>0&&!ended);anchor.gameObject.SetActive(State.Tier>0&&!ended);
            HarborUI.Place(star.rectTransform,new Rect(layout.Combo.x+5*u,layout.Combo.y+24*u,15*u,15*u));
            star.color=new Color(1,.86f,.25f);star.rectTransform.localRotation=Quaternion.Euler(0,0,-time*90);
            HarborUI.Place(anchor.rectTransform,new Rect(layout.Combo.xMax-6*u,layout.Combo.y+3*u,13*u,16*u));
            anchor.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*4)*16);
            HarborUI.Place(badge,layout.Combo);
            label.text=State.Count+(UILanguage.IsChinese?" 连击":" COMBO");label.fontSize=Mathf.RoundToInt((UILanguage.IsChinese?16:13)*u);
            var pulse=Mathf.Clamp01(1-(float)(combat.Time-State.LastExitAt)/.2f);
            label.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(pulse*Mathf.PI)*.18f);
            HarborUI.Place(timer,new Rect(layout.Combo.x+7*u,layout.Combo.y-3*u,layout.Combo.width-14*u,3*u));var ratio=(float)(State.Remaining/ExitComboState.Window);
            HarborUI.Place(remaining,new Rect((layout.Combo.width-14*u)*(1-ratio),0,(layout.Combo.width-14*u)*ratio,3*u));
            embers.BaseY=layout.FleetBottom;embers.Rise=layout.FleetHeight*.9f;
            embers.ActiveSlots=0;embers.Clock=time;embers.Tier=tier;embers.Opacity=opacity;
            for(var i=0;i<5;i++)
            {
                var arrived=combat.Fleet.StandardGroups.FirstOrDefault(g=>g.SlotIndex==i)?.ArrivedCount??0;
                if(i==defaultSlot)arrived+=combat.Fleet.Support.ArrivedCount;
                // Empty/unarrived slots do not burn; support ships are included in the default slot by the owner.
                hulls[i].gameObject.SetActive(tier>0&&opacity>0&&arrived>0);if(arrived>0)embers.ActiveSlots|=1<<i;
                HarborUI.Place(hulls[i].rectTransform,new Rect(i*sw+sw*.08f,layout.FleetBottom,sw*.84f,layout.FleetHeight*1.5f));
            }
        }
        private void LateUpdate(){if(embers!=null)embers.SetVerticesDirty();}
        private void OnDestroy()
        {exitSubscription?.Dispose();endSubscription?.Dispose();hitSubscription?.Dispose();if(bannerMaterial)Destroy(bannerMaterial);if(hullMaterial)Destroy(hullMaterial);}
    }
}
