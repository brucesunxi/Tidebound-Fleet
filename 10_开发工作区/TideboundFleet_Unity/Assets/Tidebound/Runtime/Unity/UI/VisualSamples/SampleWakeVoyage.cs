using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    /// <summary>Isolated kinematic demonstration, never reads a level, wallet or save.</summary>
    public sealed class SampleWakeVoyage : MonoBehaviour
    {
        public RectTransform Stage;
        public SampleWakeRibbon[] Ribbons;
        public RectTransform[] Boats;
        public bool Paused,Manual;
        public float Elapsed {get;private set;}
        public bool MainMoving {get;private set;}
        public string Phase {get;private set;}
        private float travel;
        private Vector2 mainStern;
        private readonly int[] miniLoops={-1,-1,-1};
        public void Configure(Texture2D ship,Rect shipUV,float shipAspect)
        {
            Stage=(RectTransform)transform;
            if(Ribbons==null||Ribbons.Length==0)
            {
                Ribbons=new SampleWakeRibbon[4];Boats=new RectTransform[4];
                for(var i=0;i<4;i++)
                {
                    var r=VisualSampleGallery.Rect("WakePath_"+i,Stage);VisualSampleGallery.Fill(r);r.pivot=new Vector2(0,1);
                    Ribbons[i]=r.gameObject.AddComponent<SampleWakeRibbon>();Ribbons[i].raycastTarget=false;
                    Ribbons[i].Length=i==0?143:68;Ribbons[i].Lifetime=i==0?1.55f:.85f;Ribbons[i].color=new Color(1,1,1,i==0?.84f:.65f);
                }
                for(var i=0;i<4;i++)
                {
                    var boat=VisualSampleGallery.Rect("PreviewShip_"+i,Stage);boat.pivot=new Vector2(.5f,.5f);Boats[i]=boat;
                    var raw=boat.gameObject.AddComponent<RawImage>();raw.raycastTarget=false;
                }
            }
            for(var i=0;i<Boats.Length;i++)
            {var raw=Boats[i].GetComponent<RawImage>();raw.texture=ship;raw.uvRect=shipUV;var h=i==0?82:38;Boats[i].sizeDelta=new Vector2(h*shipAspect,h);}
            Restart();
        }
        public void SetStyle(Texture2D texture,Rect uv,float aspect)
        {if(Ribbons==null)return;foreach(var r in Ribbons)r.SetStyle(texture,uv,aspect);Restart();}
        public void Restart()
        {Elapsed=travel=0;MainMoving=false;Phase="Ready";if(Ribbons!=null)foreach(var r in Ribbons)r.ClearHistory();for(var i=0;i<3;i++)miniLoops[i]=-1;}
        private void Update(){if(!Manual&&!Paused)Advance(Mathf.Min(Time.unscaledDeltaTime,.05f));}
        public void Advance(float dt)
        {
            if(Ribbons==null||Stage.rect.height<50||Paused)return;
            Elapsed+=dt;var cycle=Elapsed%8f;MainMoving=cycle<5.5f;
            Phase=MainMoving?"Sailing":cycle<6.7f?"Stopped":cycle<7.5f?"Blocked":"Reset";
            if(MainMoving)travel+=dt*115;
            if(cycle>=7.5f){travel=0;Ribbons[0].ClearHistory();}
            var w=Stage.rect.width;var h=Stage.rect.height;
            // Reserve the caption and the lower three-ship lane at every review aspect ratio.
            var routeWidth=w*.60f;var routeHeight=Mathf.Clamp(h-302,64,190);
            Vector2 direction;var center=Route(travel,routeWidth,routeHeight,26,out direction)+new Vector2(w*.20f,-82-routeHeight);
            mainStern=center-direction*(Boats[0].sizeDelta.y*.43f);
            var shake=Phase=="Blocked"?new Vector2(Mathf.Sin(cycle*45)*2,0):Vector2.zero;
            Pose(Boats[0],center+shake,direction);
            Ribbons[0].Advance(dt,mainStern,MainMoving);
            for(var i=0;i<3;i++)
            {
                var shifted=Elapsed+i*.42f;var lap=Mathf.FloorToInt(shifted/3.6f);var t=shifted%3.6f;
                if(lap!=miniLoops[i]){Ribbons[i+1].ClearHistory();miniLoops[i]=lap;}
                var active=t<2f;var y=-h+30+Mathf.Min(t,2)*48;
                var pos=new Vector2(67+i*106,y);Pose(Boats[i+1],pos,Vector2.up);
                Ribbons[i+1].Advance(dt,pos-Vector2.up*16,active);
            }
        }
        public void Seek(float seconds)
        {
            var wasPaused=Paused;Paused=false;Restart();
            var whole=Mathf.FloorToInt(seconds*60);for(var i=0;i<whole;i++)Advance(1f/60);
            if(seconds-whole/60f>.00001f)Advance(seconds-whole/60f);Paused=wasPaused;
        }
        private static void Pose(RectTransform boat,Vector2 p,Vector2 forward)
        {boat.anchoredPosition=p;boat.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(forward.y,forward.x)*Mathf.Rad2Deg-90);}
        private static Vector2 Route(float distance,float w,float h,float r,out Vector2 direction)
        {
            var v=h-2*r;var u=w-2*r;var arc=Mathf.PI*r*.5f;var total=2*v+2*u+4*arc;var d=distance%total;
            if(d<v){direction=Vector2.up;return new Vector2(0,r+d);}d-=v;
            if(d<arc)return Arc(new Vector2(r,h-r),180-d/r*Mathf.Rad2Deg,out direction);d-=arc;
            if(d<u){direction=Vector2.right;return new Vector2(r+d,h);}d-=u;
            if(d<arc)return Arc(new Vector2(w-r,h-r),90-d/r*Mathf.Rad2Deg,out direction);d-=arc;
            if(d<v){direction=Vector2.down;return new Vector2(w,h-r-d);}d-=v;
            if(d<arc)return Arc(new Vector2(w-r,r),-d/r*Mathf.Rad2Deg,out direction);d-=arc;
            if(d<u){direction=Vector2.left;return new Vector2(w-r-d,0);}d-=u;
            return Arc(new Vector2(r,r),-90-d/r*Mathf.Rad2Deg,out direction);
            Vector2 Arc(Vector2 c,float deg,out Vector2 tangent)
            {var a=deg*Mathf.Deg2Rad;tangent=new Vector2(Mathf.Sin(a),-Mathf.Cos(a));return c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}
        }
    }
}
