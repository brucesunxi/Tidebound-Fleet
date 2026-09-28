using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Water and foam only: never paints an opaque board or controls navigation.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HarborLaneGraphic : MaskableGraphic
    {
        public int Columns,Rows;
        public Func<bool> Paused;
        private float time,refresh;
        private void Update(){if(Paused?.Invoke()==true)return;time+=Time.unscaledDeltaTime;if(time-refresh>.05f){refresh=time;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();const float b=.7f;var l=.24f;var r=Columns+1.16f;var low=.24f;var high=Rows+1.16f;var cx=Columns/2f+b;
            Ribbon(v,new Vector2(l,low),new Vector2(l,high));Ribbon(v,new Vector2(r,low),new Vector2(r,high));
            Ribbon(v,new Vector2(l,low),new Vector2(r,low));Ribbon(v,new Vector2(l,high),new Vector2(cx-.68f,high));Ribbon(v,new Vector2(cx+.68f,high),new Vector2(r,high));
            Ribbon(v,new Vector2(cx,high),new Vector2(cx,high+1));
            for(float y=low+1;y<high-.8f;y+=2.7f){Arrow(v,new Vector2(l,y),Vector2.up);Arrow(v,new Vector2(r,y+.35f),Vector2.up);}
            for(float x=l+1;x<r-1;x+=2.9f){Arrow(v,new Vector2(x,low),x<cx?Vector2.left:Vector2.right);Arrow(v,new Vector2(x,high),x<cx?Vector2.right:Vector2.left);}
            Arrow(v,new Vector2(cx,high+.35f),Vector2.up);
        }
        private void Ribbon(VertexHelper v,Vector2 a,Vector2 b)
        {
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x);
            Line(v,a,b, .32f,new Color(0,.92f,1,.09f));Line(v,a,b,.26f,new Color(.1f,.9f,1,.07f));
            var count=Mathf.Max(2,Mathf.CeilToInt(Vector2.Distance(a,b)/.18f));
            for(var side=-1;side<=1;side+=2)
            for(var i=0;i<count;i++)
            {
                float t=(float)i/count,t2=(float)(i+1)/count;
                var p=Vector2.Lerp(a,b,t)+n*side*(.15f+.01f*Mathf.Sin(t*count*1.4f+time*.8f));
                var q=Vector2.Lerp(a,b,t2)+n*side*(.15f+.01f*Mathf.Sin(t2*count*1.4f+time*.8f));
                Line(v,p,q,.035f,new Color(.82f,1,1,.16f+.06f*Mathf.Sin(i*1.9f+time)));
                
            }
        }
        private void Arrow(VertexHelper v,Vector2 p,Vector2 d)
        {
            p+=d*Mathf.Sin(time*1.7f)*.04f;var n=new Vector2(-d.y,d.x);
            var c=new Color(.78f,1,1,.6f+.12f*Mathf.Sin(time*1.7f));
            Line(v,p-d*.10f+n*.13f,p+d*.05f,.065f,c);Line(v,p+d*.05f,p-d*.10f-n*.13f,.065f,c);
        }
        internal static void Line(VertexHelper v,Vector2 a,Vector2 b,float width,Color c)
        {
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width/2;var k=v.currentVertCount;
            v.AddVert(a+n,c,Vector2.zero);v.AddVert(b+n,c,Vector2.zero);v.AddVert(b-n,c,Vector2.zero);v.AddVert(a-n,c,Vector2.zero);
            v.AddTriangle(k,k+1,k+2);v.AddTriangle(k,k+2,k+3);
        }
    }
}
