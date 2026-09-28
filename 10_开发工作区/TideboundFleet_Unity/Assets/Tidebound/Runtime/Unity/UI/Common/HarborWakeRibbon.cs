using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Water-plane ribbon shared by the review and actual movement presentation. Historical centers stay put; art is cropped at a fixed distance scale.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class HarborWakeRibbon : MaskableGraphic
    {
        public Texture2D Texture;
        public Rect UV = new Rect(0,0,1,1);
        public float Length = 120, Width = 45, Lifetime = 1.15f, SampleSpacing = 1.5f;
        private struct Sample { public Vector2 position; public float time; public Sample(Vector2 p,float t){position=p;time=t;} }
        private readonly List<Sample> samples=new List<Sample>(160);
        private float clock;
        public override Texture mainTexture=>Texture?Texture:s_WhiteTexture;
        public int SampleCount=>samples.Count;
        public float HistoryLength { get {float d=0;for(var i=1;i<samples.Count;i++)d+=Vector2.Distance(samples[i-1].position,samples[i].position);return d;} }
        public Vector2 OldestPosition=>samples.Count==0?Vector2.zero:samples[0].position;
        public void SetStyle(Texture2D texture,Rect uv,float aspect)
        {Texture=texture;UV=uv;Width=Length*aspect;ClearHistory();SetMaterialDirty();}
        public void ClearHistory(){samples.Clear();clock=0;SetVerticesDirty();}
        public void Advance(float dt,Vector2 stern,bool travelling)
        {
            clock+=Mathf.Max(0,dt);
            while(samples.Count>0&&clock-samples[0].time>=Lifetime)samples.RemoveAt(0);
            if(travelling)
            {
                if(samples.Count>0&&Vector2.Distance(samples[samples.Count-1].position,stern)>Length*2)samples.Clear();
                if(samples.Count==0||Vector2.SqrMagnitude(stern-samples[samples.Count-1].position)>=SampleSpacing*SampleSpacing)
                    samples.Add(new Sample(stern,clock));
            }
            // Keep one extra segment for exact clipping at the configured maximum length.
            var distance=0f;
            for(var i=samples.Count-1;i>0;i--)
            {distance+=Vector2.Distance(samples[i].position,samples[i-1].position);if(distance>Length){if(i>1)samples.RemoveRange(0,i-1);break;}}
            if(samples.Count>200)samples.RemoveRange(0,samples.Count-200);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!Texture||samples.Count<2)return;
            var distance=0f;
            for(var i=samples.Count-1;i>0;i--)
            {
                var a=samples[i];var b=samples[i-1];var segment=Vector2.Distance(a.position,b.position);if(segment<.001f)continue;
                var end=Mathf.Min(Length,distance+segment);var f=(end-distance)/segment;
                var p=Vector2.Lerp(a.position,b.position,f);var t=Mathf.Lerp(a.time,b.time,f);
                AddRow(vh,a.position,Normal(i),distance,a.time);
                AddRow(vh,p,Vector2.Lerp(Normal(i),Normal(i-1),f).normalized,end,t);
                var n=vh.currentVertCount-4;vh.AddTriangle(n,n+2,n+1);vh.AddTriangle(n+1,n+2,n+3);
                distance=end;if(distance>=Length)break;
            }
        }
        private Vector2 Normal(int i)
        {
            var before=samples[Mathf.Max(0,i-1)].position;var after=samples[Mathf.Min(samples.Count-1,i+1)].position;
            var tangent=(after-before).normalized;return new Vector2(-tangent.y,tangent.x);
        }
        private void AddRow(VertexHelper vh,Vector2 p,Vector2 normal,float distance,float birth)
        {
            var life=Mathf.Clamp01((clock-birth)/Lifetime);var tint=color;
            tint.a*=1-Mathf.SmoothStep(0,1,life);
            var v=UV.yMax-Mathf.Clamp01(distance/Length)*UV.height;
            vh.AddVert(p+normal*Width*.5f,tint,new Vector2(UV.xMin,v));
            vh.AddVert(p-normal*Width*.5f,tint,new Vector2(UV.xMax,v));
        }
    }
}
