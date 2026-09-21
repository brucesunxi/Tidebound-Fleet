using System.Collections.Generic;
using System.Linq;
using Tidebound.Combat;
using Tidebound.Core;
using UnityEngine;
using UnityEngine.UI;
using Tidebound.Unity.UI;

namespace Tidebound.Unity.Boss
{
    /// <summary>Presentation-only combat HUD. Removing a projectile cannot change damage or victory.</summary>
    public sealed class FleetCombatGrayboxView : MonoBehaviour
    {
        private GameSession session;
        private FleetCombatSystem combat;
        private Font font;
        private RectTransform root,boss,hpTrack,hpFill,support;
        private Image bossImage;
        private Text hpLabel,victory,supportLabel;
        private readonly List<RectTransform> seats=new List<RectTransform>();
        private readonly List<Text> counts=new List<Text>();
        private readonly Dictionary<string,RectTransform> projectiles=new Dictionary<string,RectTransform>();
        public int ProjectileCount => projectiles.Count;
        public void Initialize(GameSession session,FleetCombatSystem combat,Font font)
        {
            this.session=session;this.combat=combat;this.font=font;root=(RectTransform)transform;
            gameObject.AddComponent<RectMask2D>();
            boss=Panel("Kraken",new Color(.65f,.20f,.30f));bossImage=boss.GetComponent<Image>();
            Text("KrakenLabel",boss,"KRAKEN",12);
            hpTrack=Panel("HpTrack",new Color(.20f,.10f,.15f));hpFill=Panel("HpFill",new Color(.95f,.30f,.35f),hpTrack);
            hpLabel=Text("Hp",hpTrack,"",10);
            for(var i=0;i<5;i++)
            {
                var seat=Panel("FleetSeat"+i,new Color(.12f,.27f,.35f));seats.Add(seat);
                counts.Add(Text("Count",seat,"-",11));
            }
            support=Panel("LongSupport",new Color(.28f,.35f,.40f));supportLabel=Text("Count",support,"",10);
            victory=Text("Victory",root,"VICTORY",20);victory.color=new Color(1,.85f,.25f);
        }
        public void Present()
        {
            if(combat==null) return;
            var w=root.rect.width;var h=root.rect.height;
            Set(boss,new Rect(w/2-45,h-24,90,22));
            Set(hpTrack,new Rect(8,h-39,w-16,11));
            Set(hpFill,new Rect(0,0,(w-16)*session.Boss.Hp/session.Boss.InitialHp,11));
            hpLabel.text=session.Boss.Hp+" / "+session.Boss.InitialHp;
            bossImage.color=combat.Time-combat.LastHitAt<.12 ? new Color(1,.75f,.65f) : new Color(.65f,.20f,.30f);
            var seatWidth=(w-24)/5;
            for(var i=0;i<seats.Count;i++)
            {
                Set(seats[i],new Rect(4+i*(seatWidth+4),0,seatWidth,20));
                var group=combat.Fleet.StandardGroups.FirstOrDefault(g=>g.SlotIndex==i);
                counts[i].text=group==null ? "-" : "F"+(i+1)+" x"+group.ArrivedCount;
                seats[i].GetComponent<Image>().color=group!=null && group.ArrivedCount>0 ? new Color(.18f,.52f,.65f) : new Color(.10f,.21f,.28f);
            }
            Set(support,new Rect(w-62,24,58,16));supportLabel.text="Long x"+combat.Fleet.Support.ArrivedCount;
            support.gameObject.SetActive(combat.Fleet.Support.ArrivedCount>0);
            foreach(var token in combat.Attacks)
            {
                if(token.Stage!=AttackStage.InFlight) continue;
                if(!projectiles.TryGetValue(token.AttackId,out var projectile))
                { projectile=Panel("Shot_"+token.Ship.ShipId,new Color(1,.83f,.25f));projectiles.Add(token.AttackId,projectile); }
                var start=token.Group.IsSupport ? support.anchoredPosition+support.sizeDelta/2 :
                    seats[token.Group.SlotIndex].anchoredPosition+seats[token.Group.SlotIndex].sizeDelta/2;
                var end=boss.anchoredPosition+boss.sizeDelta/2;
                Set(projectile,new Rect(Vector2.Lerp(start,end,token.FlightProgress(combat.Time))-Vector2.one*3,Vector2.one*6));
            }
            var active=combat.Attacks.Where(t=>t.Stage==AttackStage.InFlight).Select(t=>t.AttackId).ToHashSet();
            foreach(var id in projectiles.Keys.Where(id=>!active.Contains(id)).ToArray())
            { Destroy(projectiles[id].gameObject);projectiles.Remove(id); }
            victory.gameObject.SetActive(combat.IsVictorious);
            if(combat.IsVictorious) { victory.transform.SetAsLastSibling();Set(victory.rectTransform,new Rect(0,22,w,28)); }
        }
        private RectTransform Panel(string name,Color color,RectTransform parent=null)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent!=null?parent:root,false);
            r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;
            var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return r;
        }
        private Text Text(string name,RectTransform parent,string label,int size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var text=r.gameObject.AddComponent<HarborText>();text.text=label;text.font=font;text.fontSize=size;
            text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;
        }
        private static void Set(RectTransform r,Rect bounds)
        { r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=bounds.position;r.sizeDelta=bounds.size; }
    }
}
