using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Home-only original display geometry. Never used as a board ship, skin identity or collider.</summary>
    public sealed class HarborShowcaseModel : MonoBehaviour
    {
        private Mesh mesh;private Material material;
        private static readonly Color Cream=new Color(1,.95f,.80f), White=new Color(1,.99f,.94f),
            Navy=new Color(.025f,.25f,.49f), Blue=new Color(.035f,.47f,.78f), Red=new Color(.97f,.16f,.09f),
            Gold=new Color(1,.67f,.075f), Glass=new Color(.035f,.49f,.73f);
        public void Build()
        {
            var b=new Model();const float deck=-.57f;
            var hull=Outline(.63f,1.02f,48);
            b.Loft(hull,new[]{.58f,.82f,.97f,1f,.98f},new[]{-.04f,-.12f,-.31f,-.46f,deck},Navy);
            b.Loft(hull,new[]{1.005f,1.02f,1.01f},new[]{-.38f,-.45f,-.50f},Blue);
            b.Loft(hull,new[]{.97f,.99f,.98f},new[]{deck,-.69f,-.74f},Cream);b.Cap(hull,.98f,-.741f,Cream);
            var inset=Outline(.50f,.85f,48);b.Cap(inset,1,-.747f,new Color(.58f,.32f,.12f));
            // A raised ivory rail wraps the wooden deck, with a second dark rub strip below it.
            b.Loft(hull,new[]{.99f,1.01f,.99f},new[]{-.72f,-.79f,-.81f},White);
            b.Loft(Outline(.53f,.90f,48),new[]{1f,1f},new[]{-.75f,-.81f},Cream);
            var cabin=RoundBox(.42f,.42f,.10f,new Vector2(0,-.24f));
            b.Loft(cabin,new[]{1f,1f,.94f},new[]{-.75f,-1.28f,-1.34f},Cream);b.Cap(cabin,.94f,-1.34f,Cream);
            // Raised window surrounds and inset blue glass on both sides and on the forward wall.
            foreach(var x in new[]{-.425f,.425f})foreach(var y in new[]{-.45f,-.14f})
                Window(b,new Vector3(x,y,-1.075f),Vector3.up,Vector3.back,.12f,.185f);
            foreach(var x in new[]{-.215f,.215f})Window(b,new Vector3(x,.185f,-1.075f),Vector3.right,Vector3.back,.16f,.18f);
            var roof=RoundBox(.48f,.49f,.13f,new Vector2(0,-.24f));
            b.Loft(roof,new[]{.94f,1f,1f,.93f},new[]{-1.31f,-1.36f,-1.45f,-1.48f},Red);b.Cap(roof,.93f,-1.48f,Red);
            // Rounded chimney, cap, mast and pennant.
            b.Cylinder(new Vector3(-.18f,-.44f,-1.46f),new Vector3(-.18f,-.44f,-1.82f),.105f,Navy);
            b.Cylinder(new Vector3(-.18f,-.44f,-1.79f),new Vector3(-.18f,-.44f,-1.87f),.128f,Cream);
            b.Cylinder(new Vector3(.19f,-.31f,-1.44f),new Vector3(.19f,-.31f,-2.18f),.025f,Gold);
            b.Sphere(new Vector3(.19f,-.31f,-2.20f),new Vector3(.058f,.058f,.058f),Gold);
            var flagOrigin=new Vector3(.19f,-.31f,-2.13f);
            for(var i=0;i<16;i++)
            {
                var x=i*.027f;var nx=(i+1)*.027f;var wave=Mathf.Sin(i*.37f)*.035f;var nw=Mathf.Sin((i+1)*.37f)*.035f;
                b.Quad(flagOrigin+new Vector3(x,wave,0),flagOrigin+new Vector3(nx,nw,.035f),flagOrigin+new Vector3(nx,nw,.25f),flagOrigin+new Vector3(x,wave,.25f),Red);
            }
            // Life rings sit on each side of the hull and remain separate from the white rail.
            foreach(var side in new[]{-1,1})foreach(var y in new[]{-.40f,.06f})
            {
                var center=new Vector3(side*.635f,y,-.63f);b.Torus(center,Vector3.up,Vector3.back,.145f,.042f,White,true);
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
            mesh=b.Finish();var filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            material=new Material(Resources.Load<Shader>("TideboundUI/ShowcaseVolume")){name="HomeFlagshipLacquer"};
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        private static void Window(Model b,Vector3 c,Vector3 u,Vector3 v,float hw,float hh)
        {
            var normal=Vector3.Cross(u,v);if(Vector3.Dot(normal,c)<0)normal=-normal;
            b.Panel(c,u,v,hw+.02f,hh+.02f,Cream);b.Panel(c+normal*.006f,u,v,hw,hh,Glass);
            b.Panel(c+normal*.009f-u*hw*.48f,u,v,hw*.12f,hh*.87f,new Color(.32f,.77f,.92f));
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
            private readonly List<Color> colors=new List<Color>();private readonly List<int> triangles=new List<int>();
            private void Tri(Vector3 a,Vector3 b,Vector3 c,Color color,Vector3 na,Vector3 nb,Vector3 nc)
            {var i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);normals.Add(na);normals.Add(nb);normals.Add(nc);colors.Add(color);colors.Add(color);colors.Add(color);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {var n=Vector3.Cross(b-a,c-a).normalized;Tri(a,b,c,color,n,n,n);Tri(a,c,d,color,n,n,n);}
            public void Panel(Vector3 c,Vector3 u,Vector3 v,float w,float h,Color color)=>Quad(c-u*w-v*h,c+u*w-v*h,c+u*w+v*h,c-u*w+v*h,color);
            public void Loft(Vector2[] shape,float[] sizes,float[] heights,Color color)
            {
                for(var level=0;level<sizes.Length-1;level++)for(var i=0;i<shape.Length;i++)
                {
                    var j=(i+1)%shape.Length;var a=new Vector3(shape[i].x*sizes[level],shape[i].y*sizes[level],heights[level]);var b=new Vector3(shape[j].x*sizes[level],shape[j].y*sizes[level],heights[level]);
                    var c=new Vector3(shape[j].x*sizes[level+1],shape[j].y*sizes[level+1],heights[level+1]);var d=new Vector3(shape[i].x*sizes[level+1],shape[i].y*sizes[level+1],heights[level+1]);
                    var tangentI=shape[j]-shape[(i+shape.Length-1)%shape.Length];var tangentJ=shape[(j+1)%shape.Length]-shape[i];
                    var ni=Vector3.Cross(new Vector3(tangentI.x,tangentI.y,0),d-a).normalized;var nj=Vector3.Cross(new Vector3(tangentJ.x,tangentJ.y,0),c-b).normalized;
                    // Ring outlines run counterclockwise; height is negative Z, so invert the inward cross product.
                    ni=-ni;nj=-nj;Tri(a,c,b,color,ni,nj,nj);Tri(a,d,c,color,ni,ni,nj);
                }
            }
            public void Cap(Vector2[] shape,float scale,float z,Color color)
            {var c=Vector2.zero;foreach(var v in shape)c+=v;c*=scale/shape.Length;for(var i=0;i<shape.Length;i++){var a=shape[i]*scale;var b=shape[(i+1)%shape.Length]*scale;Tri(new Vector3(c.x,c.y,z),new Vector3(b.x,b.y,z),new Vector3(a.x,a.y,z),color,Vector3.back,Vector3.back,Vector3.back);}}
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
            public Mesh Finish(){var m=new Mesh{name="HomeFlagship_Original",indexFormat=IndexFormat.UInt32};m.SetVertices(vertices);m.SetNormals(normals);m.SetColors(colors);m.SetTriangles(triangles,0);m.RecalculateBounds();return m;}
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
