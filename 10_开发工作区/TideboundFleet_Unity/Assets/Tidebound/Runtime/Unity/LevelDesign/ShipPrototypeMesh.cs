using System.Collections.Generic;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Original low-poly greybox geometry, generated once per presentation; no external art.</summary>
    internal static class ShipPrototypeMesh
    {
        private sealed class Builder
        {
            public readonly List<Vector3> Vertices=new List<Vector3>();
            public readonly List<int> Triangles=new List<int>();
            public readonly List<Color> Colors=new List<Color>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
            {var i=Vertices.Count;Vertices.Add(a);Vertices.Add(b);Vertices.Add(c);Colors.Add(color);Colors.Add(color);Colors.Add(color);Triangles.Add(i);Triangles.Add(i+1);Triangles.Add(i+2);}
            public void Ring(Vector2[] shape,float scale,float z,float upperScale,float upperZ,Color color)
            {
                for(var i=0;i<shape.Length;i++)
                {var j=(i+1)%shape.Length;var a=Point(shape[i]*scale,z);var b=Point(shape[j]*scale,z);var c=Point(shape[j]*upperScale,upperZ);var d=Point(shape[i]*upperScale,upperZ);Triangle(a,c,b,color);Triangle(a,d,c,color);}
            }
            public void Cap(Vector2[] shape,float scale,float z,Color color)
            {
                var center=Vector2.zero;foreach(var point in shape)center+=point;center*=scale/shape.Length;
                for(var i=0;i<shape.Length;i++)Triangle(Point(center,z),Point(shape[(i+1)%shape.Length]*scale,z),Point(shape[i]*scale,z),color);
            }
            public Mesh Finish(string name)
            {var m=new Mesh{name=name};m.SetVertices(Vertices);m.SetColors(Colors);m.SetTriangles(Triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
        private static Vector3 Point(Vector2 v,float z)=>new Vector3(v.x,v.y,z);
        public static Mesh Build(int length)
        {
            var b=new Builder();var half=length*.5f-.09f;
            // Counterclockwise hull outline: flat stern, broad shoulders, single unmistakable bow.
            var hull=new[]{new Vector2(-.29f,-half),new Vector2(.29f,-half),new Vector2(.38f,-half+.16f),
                new Vector2(.38f,half-.46f),new Vector2(.24f,half-.14f),new Vector2(0,half),
                new Vector2(-.24f,half-.14f),new Vector2(-.38f,half-.46f),new Vector2(-.38f,-half+.16f)};
            b.Ring(hull,.82f,-.07f,1,-.18f,new Color(.64f,.68f,.72f,1));
            b.Ring(hull,1,-.18f,.85f,-.29f,new Color(1,1,1,1));
            b.Cap(hull,.85f,-.29f,new Color(.93f,.96f,.96f,.60f));
            var cabin=new[]{new Vector2(-.22f,-half+.23f),new Vector2(.22f,-half+.23f),new Vector2(.22f,-half+.67f),new Vector2(-.22f,-half+.67f)};
            b.Ring(cabin,1,-.30f,.82f,-.48f,new Color(.19f,.30f,.37f,0));
            b.Cap(cabin,.82f,-.48f,new Color(.88f,.92f,.89f,0));
            // Raised white chevron remains readable without relying on the five palette colors.
            var y=half-.40f;var z=-.305f;var white=new Color(1,.98f,.87f,0);
            b.Triangle(new Vector3(-.20f,y-.15f,z),new Vector3(0,y+.16f,z),new Vector3(0,y-.01f,z),white);
            b.Triangle(new Vector3(0,y-.01f,z),new Vector3(0,y+.16f,z),new Vector3(.20f,y-.15f,z),white);
            if(length==3)
            {
                var hatch=new[]{new Vector2(-.19f,-.34f),new Vector2(.19f,-.34f),new Vector2(.19f,.16f),new Vector2(-.19f,.16f)};
                b.Ring(hatch,1,-.30f,.88f,-.38f,new Color(.28f,.37f,.42f,0));
                b.Cap(hatch,.88f,-.38f,new Color(.60f,.65f,.65f,0));
            }
            return b.Finish("PrototypeHull_"+length);
        }
        public static Mesh Quad()
        {
            var m=new Mesh{name="PrototypeQuad"};m.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};
            m.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};m.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            m.triangles=new[]{0,1,2,0,2,3};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        // B1 study only. All decoration remains inside the same logical footprint.
        public static Mesh BuildToy(int length,bool blue=false)
        {
            var b=new Builder();var half=length*.5f-.09f;
            var hull=new[]{new Vector2(-.23f,-half),new Vector2(.23f,-half),new Vector2(.34f,-half+.09f),
                new Vector2(.40f,-half+.25f),new Vector2(.40f,half-.73f),new Vector2(.33f,half-.30f),
                new Vector2(.18f,half-.10f),new Vector2(0,half),new Vector2(-.18f,half-.10f),
                new Vector2(-.33f,half-.30f),new Vector2(-.40f,half-.73f),new Vector2(-.40f,-half+.25f),new Vector2(-.34f,-half+.09f)};
            var cream=new Color(1,.94f,.78f,0);var bright=new Color(1,.99f,.90f,0);
            b.Ring(hull,.90f,-.05f,.96f,-.32f,new Color(.65f,.75f,.83f,1));
            b.Ring(hull,.96f,-.32f,1,-.46f,Color.white);
            b.Ring(hull,1,-.46f,.96f,-.53f,cream);
            b.Cap(hull,.96f,-.53f,bright);
            var cabin=RoundedRectangle(-.25f,-half+.30f,.25f,-half+.89f,.07f);
            b.Ring(cabin,1,-.535f,1,-.79f,cream);
            // Three broad windows: side faces read in a showcase, fore window stays below the roof.
            var glass=new Color(.08f,.40f,.51f,0);
            Face(b,new Vector3(.17f,-half+.891f,-.58f),new Vector3(-.17f,-half+.891f,-.58f),new Vector3(-.17f,-half+.891f,-.73f),new Vector3(.17f,-half+.891f,-.73f),glass);
            Face(b,new Vector3(.251f,-half+.42f,-.58f),new Vector3(.251f,-half+.76f,-.58f),new Vector3(.251f,-half+.76f,-.73f),new Vector3(.251f,-half+.42f,-.73f),glass);
            Face(b,new Vector3(-.251f,-half+.76f,-.58f),new Vector3(-.251f,-half+.42f,-.58f),new Vector3(-.251f,-half+.42f,-.73f),new Vector3(-.251f,-half+.76f,-.73f),glass);
            var roof=RoundedRectangle(-.28f,-half+.26f,.28f,-half+.94f,.09f);
            var accent=blue?new Color(.22f,.55f,.87f,0):new Color(.98f,.39f,.25f,0);
            if(length==3)accent=new Color(.47f,.60f,.63f,0);
            b.Ring(roof,1,-.79f,.96f,-.86f,accent);b.Cap(roof,.96f,-.86f,accent);
            // A chimney behind the cabin, and an unmistakable dark bow chevron on the light deck.
            var chimney=RoundedRectangle(-.12f,-half+.12f,.12f,-half+.26f,.035f);
            b.Ring(chimney,1,-.53f,1,-.71f,cream);b.Cap(chimney,1,-.71f,new Color(.12f,.28f,.34f,0));
            var y=half-.39f;var ink=new Color(.06f,.25f,.33f,0);const float z=-.54f;
            b.Triangle(new Vector3(-.21f,y-.16f,z),new Vector3(0,y+.18f,z),new Vector3(0,y-.02f,z),ink);
            b.Triangle(new Vector3(0,y-.02f,z),new Vector3(0,y+.18f,z),new Vector3(.21f,y-.16f,z),ink);
            if(length==3)
            {
                var cargo=RoundedRectangle(-.25f,-.34f,.25f,.35f,.055f);
                b.Ring(cargo,1,-.54f,.90f,-.66f,new Color(.38f,.49f,.53f,0));
                b.Cap(cargo,.90f,-.66f,new Color(.64f,.73f,.74f,0));
            }
            return b.Finish("ToyStudy_"+length+(blue?"_Blue":""));
        }
        private static Vector2[] RoundedRectangle(float left,float bottom,float right,float top,float bevel)=>new[]{
            new Vector2(left+bevel,bottom),new Vector2(right-bevel,bottom),new Vector2(right,bottom+bevel),new Vector2(right,top-bevel),
            new Vector2(right-bevel,top),new Vector2(left+bevel,top),new Vector2(left,top-bevel),new Vector2(left,bottom+bevel)};
        private static void Face(Builder b,Vector3 a,Vector3 c,Vector3 d,Vector3 e,Color color)
        {b.Triangle(a,d,c,color);b.Triangle(a,e,d,color);}
    }
}
