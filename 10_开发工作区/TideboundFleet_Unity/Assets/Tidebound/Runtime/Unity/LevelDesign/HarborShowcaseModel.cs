using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Home-only original display geometry. Never used as a board ship, skin identity or collider.</summary>
    public sealed class HarborShowcaseModel : MonoBehaviour
    {
        private Mesh mesh, waterMesh;private Material material, waterMaterial;
        private Renderer waterRenderer;
        public float WaterPhase { get; private set; }
        public void SetMotionPhase(float phase){WaterPhase=phase;if(waterMaterial!=null)waterMaterial.SetFloat("_Phase",phase);}
        public void SetWaterVisible(bool visible){if(waterRenderer!=null)waterRenderer.enabled=visible;}
        private static readonly Color Cream=new Color(1,.95f,.80f), White=new Color(1,.99f,.94f),
            Navy=new Color(.025f,.22f,.43f), Blue=new Color(.035f,.38f,.64f), Red=new Color(.97f,.12f,.055f),
            Gold=new Color(1,.67f,.075f), Glass=new Color(.035f,.49f,.73f);
        public void Build()
        {
            var b=new Model();const float deck=-.57f;
            var hull=Outline(.63f,1.02f,48);
            b.Loft(hull,new[]{.60f,.73f,.87f,.96f,1f,.99f},new[]{-.035f,-.09f,-.20f,-.36f,-.48f,deck},Navy,true);
            b.Loft(hull,new[]{.994f,1.015f,1.015f,.992f},new[]{-.41f,-.425f,-.47f,-.485f},Blue,true);
            b.Loft(hull,new[]{.98f,1f,1f,.975f},new[]{deck,-.62f,-.69f,-.74f},Cream,true);b.Cap(hull,.98f,-.741f,Cream);
            var inset=Outline(.50f,.85f,48);b.Cap(inset,1,-.747f,new Color(.58f,.32f,.12f));
            // A raised ivory rail wraps the wooden deck, with a second dark rub strip below it.
            b.Loft(hull,new[]{.975f,1.015f,1.025f,1.005f,.98f},new[]{-.72f,-.76f,-.785f,-.81f,-.82f},White,true);
            b.Loft(Outline(.53f,.90f,48),new[]{1f,1f},new[]{-.75f,-.81f},Cream);
            var cabin=RoundBox(.42f,.42f,.13f,new Vector2(0,-.24f));
            b.Loft(cabin,new[]{1.025f,1f,.955f,.91f},new[]{-.75f,-.82f,-1.25f,-1.34f},Cream);b.Cap(cabin,.94f,-1.34f,Cream);
            // Raised window surrounds and inset blue glass on both sides and on the forward wall.
            foreach(var x in new[]{-.413f,.413f})foreach(var y in new[]{-.45f,-.14f})
                Window(b,new Vector3(x,y,-1.075f),Vector3.up,new Vector3(-Mathf.Sign(x)*.065f,0,-1).normalized,.12f,.175f);
            foreach(var x in new[]{-.215f,.215f})Window(b,new Vector3(x,.169f,-1.075f),Vector3.right,new Vector3(0,-.05f,-1).normalized,.16f,.175f);
            var roof=RoundBox(.48f,.49f,.13f,new Vector2(0,-.24f));
            b.Loft(roof,new[]{.91f,.99f,1.015f,1.01f,.975f,.86f,.48f,.02f},new[]{-1.30f,-1.33f,-1.37f,-1.41f,-1.455f,-1.48f,-1.495f,-1.50f},Red,true);
            b.Cap(roof,.02f,-1.50f,Red);
            // Rounded chimney, cap, mast and pennant.
            b.Cylinder(new Vector3(.18f,-.44f,-1.46f),new Vector3(.18f,-.44f,-1.82f),.105f,Navy);
            b.Cylinder(new Vector3(.18f,-.44f,-1.79f),new Vector3(.18f,-.44f,-1.87f),.128f,Cream);
            b.Cylinder(new Vector3(-.19f,-.31f,-1.44f),new Vector3(-.19f,-.31f,-2.18f),.025f,Gold);
            b.Sphere(new Vector3(-.19f,-.31f,-2.20f),new Vector3(.058f,.058f,.058f),Gold);
            var flagOrigin=new Vector3(-.19f,-.31f,-2.13f);
            for(var i=0;i<16;i++)
            {
                var x=-i*.035f;var nx=-(i+1)*.035f;var wave=Mathf.Sin(i*.32f)*.08f;var nw=Mathf.Sin((i+1)*.32f)*.08f;
                b.Quad(flagOrigin+new Vector3(x,wave,0),flagOrigin+new Vector3(nx,nw,-.02f),flagOrigin+new Vector3(nx,nw,.29f),flagOrigin+new Vector3(x,wave,.29f),Red);
            }
            // Life rings sit on each side of the hull and remain separate from the white rail.
            foreach(var side in new[]{-1,1})foreach(var y in new[]{-.40f,.06f})
            {
                var center=new Vector3(side*.635f,y,-.63f);b.Torus(center,Vector3.up,Vector3.back,.157f,.051f,White,true);
            }
            // Gold portholes and a deck spotlight.
            foreach(var side in new[]{-1,1})foreach(var y in new[]{-.52f,-.1f})
            {
                var center=new Vector3(side*.429f,y,-.84f);b.Torus(center,Vector3.up,Vector3.back,.055f,.015f,Gold);
                b.Disc(center,Vector3.up,Vector3.back,.043f,Navy);
            }
            b.Cylinder(new Vector3(.23f,.57f,-.76f),new Vector3(.23f,.57f,-1.01f),.033f,Gold);
            b.Cylinder(new Vector3(.23f,.53f,-1.02f),new Vector3(.23f,.70f,-1.02f),.095f,Gold);
            b.Disc(new Vector3(.23f,.705f,-1.02f),Vector3.right,Vector3.back,.075f,Glass);
            b.Torus(new Vector3(.23f,.71f,-1.02f),Vector3.right,Vector3.back,.085f,.015f,Cream);
            // Bow anchor is modelled in relief, with a curved fluke rather than painted typography.
            var anchor=new Vector3(0,1.015f,-.48f);
            b.Cylinder(anchor+Vector3.back*.15f,anchor+Vector3.forward*.16f,.018f,Gold);
            b.Cylinder(anchor+new Vector3(-.09f,0,-.04f),anchor+new Vector3(.09f,0,-.04f),.016f,Gold);
            b.Torus(anchor+Vector3.back*.20f,Vector3.right,Vector3.forward,.035f,.012f,Gold);
            for(var i=0;i<16;i++)
            {
                var a=i*Mathf.PI/16;var c=(i+1)*Mathf.PI/16;
                b.Cylinder(anchor+new Vector3(Mathf.Cos(a)*.15f,0,Mathf.Sin(a)*.17f),anchor+new Vector3(Mathf.Cos(c)*.15f,0,Mathf.Sin(c)*.17f),.017f,Gold);
            }
            // Wooden foredeck steps, rail posts and a gold rope line give the silhouette readable depth.
            foreach(var y in new[]{.33f,.47f,.61f})
            {
                var step=RoundBox(.16f,.059f,.016f,new Vector2(-.18f,y));
                b.Loft(step,new[]{1f,.97f},new[]{-.756f,-.80f},new Color(.65f,.33f,.105f));b.Cap(step,.97f,-.80f,new Color(.83f,.51f,.21f));
            }
            foreach(var side in new[]{-1,1})
            {
                foreach(var y in new[]{-.71f,-.41f,-.11f})b.Cylinder(new Vector3(side*.53f,y,-.80f),new Vector3(side*.53f,y,-1.0f),.022f,Gold);
                b.Cylinder(new Vector3(side*.53f,-.76f,-1f),new Vector3(side*.53f,-.05f,-1f),.024f,Gold);
            }
            mesh=b.Finish();var filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            material=new Material(Resources.Load<Shader>("TideboundUI/ShowcaseVolume")){name="HomeFlagshipLacquer"};
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            BuildWater();
        }
        private void BuildWater()
        {
            var go=new GameObject("WaterContact",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            waterMesh=new Mesh{name="HomeWaterContact"};
            waterMesh.vertices=new[]{new Vector3(-1.08f,-1.4f,-.155f),new Vector3(1.08f,-1.4f,-.155f),new Vector3(1.08f,1.4f,-.155f),new Vector3(-1.08f,1.4f,-.155f)};
            waterMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};waterMesh.triangles=new[]{0,2,1,0,3,2};waterMesh.RecalculateBounds();
            waterMaterial=new Material(Resources.Load<Shader>("TideboundUI/ShowcaseWater")){name="HomeWaterContact"};
            go.GetComponent<MeshFilter>().sharedMesh=waterMesh;waterRenderer=go.GetComponent<MeshRenderer>();waterRenderer.sharedMaterial=waterMaterial;
            waterRenderer.shadowCastingMode=ShadowCastingMode.Off;waterRenderer.receiveShadows=false;
        }
        private static void Window(Model b,Vector3 c,Vector3 u,Vector3 v,float hw,float hh)
        {
            var normal=Vector3.Cross(u,v);if(Vector3.Dot(normal,c)<0)normal=-normal;
            b.RoundedPanel(c,u,v,normal,hw+.025f,hh+.02f,.042f,.014f,White);
            b.RoundedPanel(c+normal*.016f,u,v,normal,hw,hh,.034f,.007f,Glass);
            // A restrained reflection streak follows the pane rather than covering it with a flat white block.
            b.RoundedPanel(c+normal*.024f-u*hw*.60f,u,v,normal,hw*.085f,hh*.74f,.008f,.001f,new Color(.42f,.82f,.95f));
        }
        private static Vector2[] Outline(float width,float length,int count)
        {
            var result=new Vector2[count];for(var i=0;i<count;i++)
            {var a=i*Mathf.PI*2/count;var y=Mathf.Sin(a);result[i]=new Vector2(Mathf.Cos(a)*width*(y>0?1-y*.28f:1),y*length);}return result;
        }
        private static Vector2[] RoundBox(float w,float h,float radius,Vector2 c)
        {
            var points=new Vector2[32];for(var corner=0;corner<4;corner++)for(var i=0;i<8;i++)
            {var a=(corner*90+i*90f/7)*Mathf.Deg2Rad;var origin=new Vector2(corner==0||corner==3?w-radius:-w+radius,corner<2?h-radius:-h+radius);points[corner*8+i]=c+origin+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}return points;
        }
        private sealed class Model
        {
            private readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
            private readonly List<Color> colors=new List<Color>();private readonly List<Vector2> materials=new List<Vector2>();private readonly List<int> triangles=new List<int>();
            private void Tri(Vector3 a,Vector3 b,Vector3 c,Color color,Vector3 na,Vector3 nb,Vector3 nc)
            {var i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);normals.Add(na);normals.Add(nb);normals.Add(nc);colors.Add(color);colors.Add(color);colors.Add(color);var finish=FinishFor(color);materials.Add(finish);materials.Add(finish);materials.Add(finish);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);}
            private static Vector2 FinishFor(Color c)
            {
                if(c==Gold)return new Vector2(.82f,.22f);
                if(c==Glass)return new Vector2(.32f,.10f);
                if(c==Navy||c==Blue)return new Vector2(.12f,.24f);
                if(c==Cream||c==White||c==Red)return new Vector2(.04f,.32f);
                return new Vector2(0,.58f);
            }
            public void RoundedPanel(Vector3 center,Vector3 u,Vector3 v,Vector3 normal,float width,float height,float radius,float depth,Color color)
            {
                var shape=RoundBox(width,height,radius,Vector2.zero);
                for(var i=0;i<shape.Length;i++)
                {
                    var j=(i+1)%shape.Length;var a=center+u*shape[i].x+v*shape[i].y;var b=center+u*shape[j].x+v*shape[j].y;
                    var ai=center+(u*shape[i].x+v*shape[i].y)*.96f+normal*depth;var bi=center+(u*shape[j].x+v*shape[j].y)*.96f+normal*depth;
                    var na=((u*shape[i].x/width+v*shape[i].y/height).normalized+normal).normalized;
                    var nb=((u*shape[j].x/width+v*shape[j].y/height).normalized+normal).normalized;
                    Tri(a,b,bi,color,na,nb,normal);Tri(a,bi,ai,color,na,normal,normal);Tri(center+normal*depth,ai,bi,color,normal,normal,normal);
                }
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {var n=Vector3.Cross(b-a,c-a).normalized;Tri(a,b,c,color,n,n,n);Tri(a,c,d,color,n,n,n);}
            public void Panel(Vector3 c,Vector3 u,Vector3 v,float w,float h,Color color)=>Quad(c-u*w-v*h,c+u*w-v*h,c+u*w+v*h,c-u*w+v*h,color);
            public void Loft(Vector2[] shape,float[] sizes,float[] heights,Color color,bool smoothProfile=false)
            {
                var center=Vector2.zero;foreach(var point in shape)center+=point;center/=shape.Length;
                var relative=new Vector2[shape.Length];for(var n=0;n<shape.Length;n++)relative[n]=shape[n]-center;
                for(var level=0;level<sizes.Length-1;level++)for(var i=0;i<shape.Length;i++)
                {
                    var j=(i+1)%shape.Length;var a=new Vector3(center.x+relative[i].x*sizes[level],center.y+relative[i].y*sizes[level],heights[level]);var b=new Vector3(center.x+relative[j].x*sizes[level],center.y+relative[j].y*sizes[level],heights[level]);
                    var c=new Vector3(center.x+relative[j].x*sizes[level+1],center.y+relative[j].y*sizes[level+1],heights[level+1]);var d=new Vector3(center.x+relative[i].x*sizes[level+1],center.y+relative[i].y*sizes[level+1],heights[level+1]);
                    var na=LoftNormal(relative,sizes,heights,i,level,smoothProfile);
                    var nb=LoftNormal(relative,sizes,heights,j,level,smoothProfile);
                    var nc=smoothProfile?LoftNormal(relative,sizes,heights,j,level+1,true):nb;
                    var nd=smoothProfile?LoftNormal(relative,sizes,heights,i,level+1,true):na;
                    Tri(a,c,b,color,na,nc,nb);Tri(a,d,c,color,na,nd,nc);
                }
            }
            private static Vector3 LoftNormal(Vector2[] shape,float[] sizes,float[] heights,int i,int level,bool smooth)
            {
                var prev=(i+shape.Length-1)%shape.Length;var next=(i+1)%shape.Length;var tangent=shape[next]-shape[prev];
                var lo=smooth?Mathf.Max(0,level-1):level;var hi=smooth?Mathf.Min(sizes.Length-1,level+1):level+1;
                var profile=new Vector3(shape[i].x*(sizes[hi]-sizes[lo]),shape[i].y*(sizes[hi]-sizes[lo]),heights[hi]-heights[lo]);
                return -Vector3.Cross(new Vector3(tangent.x,tangent.y,0),profile).normalized;
            }
            public void Cap(Vector2[] shape,float scale,float z,Color color)
            {var c=Vector2.zero;foreach(var v in shape)c+=v;c/=shape.Length;for(var i=0;i<shape.Length;i++){var a=c+(shape[i]-c)*scale;var b=c+(shape[(i+1)%shape.Length]-c)*scale;Tri(new Vector3(c.x,c.y,z),new Vector3(b.x,b.y,z),new Vector3(a.x,a.y,z),color,Vector3.back,Vector3.back,Vector3.back);}}
            public void Disc(Vector3 c,Vector3 u,Vector3 v,float r,Color color)
            {for(var i=0;i<32;i++){var a=i*Mathf.PI*2/32;var b=(i+1)*Mathf.PI*2/32;Quad(c,c+(u*Mathf.Cos(a)+v*Mathf.Sin(a))*r,c+(u*Mathf.Cos(b)+v*Mathf.Sin(b))*r,c,color);}}
            public void Cylinder(Vector3 a,Vector3 b,float radius,Color color)
            {
                var n=(b-a).normalized;var u=Vector3.Cross(n,Mathf.Abs(n.z)<.9f?Vector3.forward:Vector3.up).normalized;var v=Vector3.Cross(n,u);
                const int count=24;for(var i=0;i<count;i++)
                {var t=i*Mathf.PI*2/count;var t2=(i+1)*Mathf.PI*2/count;var n1=u*Mathf.Cos(t)+v*Mathf.Sin(t);var n2=u*Mathf.Cos(t2)+v*Mathf.Sin(t2);
                    Tri(a+n1*radius,b+n1*radius,b+n2*radius,color,n1,n1,n2);Tri(a+n1*radius,b+n2*radius,a+n2*radius,color,n1,n2,n2);}
                Disc(a,u,-v,radius,color);Disc(b,u,v,radius,color);
            }
            public void Torus(Vector3 center,Vector3 u,Vector3 v,float radius,float tube,Color color,bool rescue=false)
            {
                var axis=Vector3.Cross(u,v);const int count=48,section=10;
                for(var i=0;i<count;i++)for(var j=0;j<section;j++)
                {
                    var p=new Vector3[4];var n=new Vector3[4];for(var k=0;k<4;k++)
                    {var a=(i+(k==1||k==2?1:0))*Mathf.PI*2/count;var t=(j+(k>=2?1:0))*Mathf.PI*2/section;var radial=u*Mathf.Cos(a)+v*Mathf.Sin(a);n[k]=radial*Mathf.Cos(t)+axis*Mathf.Sin(t);p[k]=center+radial*radius+n[k]*tube;}
                    var c=rescue&&i%12<5?Red:color;Tri(p[0],p[1],p[2],c,n[0],n[1],n[2]);Tri(p[0],p[2],p[3],c,n[0],n[2],n[3]);
                }
            }
            public void Sphere(Vector3 c,Vector3 scale,Color color)
            {
                for(var i=0;i<24;i++)for(var j=0;j<12;j++)
                {var p=new Vector3[4];for(var k=0;k<4;k++){var a=(i+(k==1||k==2?1:0))*Mathf.PI*2/24;var t=(j+(k>=2?1:0))*Mathf.PI/12;p[k]=new Vector3(Mathf.Cos(a)*Mathf.Sin(t),Mathf.Sin(a)*Mathf.Sin(t),Mathf.Cos(t));}
                    Tri(c+Vector3.Scale(p[0],scale),c+Vector3.Scale(p[1],scale),c+Vector3.Scale(p[2],scale),color,p[0],p[1],p[2]);Tri(c+Vector3.Scale(p[0],scale),c+Vector3.Scale(p[2],scale),c+Vector3.Scale(p[3],scale),color,p[0],p[2],p[3]);}
            }
            public Mesh Finish(){var m=new Mesh{name="HomeFlagship_Original",indexFormat=IndexFormat.UInt32};m.SetVertices(vertices);m.SetNormals(normals);m.SetColors(colors);m.SetUVs(0,materials);m.SetTriangles(triangles,0);m.RecalculateBounds();return m;}
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(waterMesh!=null)Destroy(waterMesh);if(waterMaterial!=null)Destroy(waterMaterial);}
    }
}
