using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Scene-owned shared prototype meshes/materials. XY is the board; negative Z is height.</summary>
    public sealed class ShipPrototypeResources : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly Material[] palette = new Material[5];
        private Mesh standard, longShip, quad;
        private Material support, shadow, surface;
        private static readonly Color[] SlotColors = {
            new Color(.22f,.68f,.78f), new Color(.90f,.48f,.32f), new Color(.55f,.73f,.38f),
            new Color(.67f,.49f,.79f), new Color(.94f,.73f,.27f) };
        public void Initialize()
        {
            var hullShader=Resources.Load<Shader>("TideboundPrototype/ShipVolume");
            var shadowShader=Resources.Load<Shader>("TideboundPrototype/ContactShadow");
            if(hullShader==null || shadowShader==null)throw new InvalidOperationException("Missing ship prototype shaders.");
            for(var i=0;i<5;i++)palette[i]=Material(hullShader,SlotColors[i],"PrototypeSlot"+i);
            support=Material(hullShader,new Color(.47f,.57f,.64f),"FixedLongShip");
            shadow=Material(shadowShader,new Color(.008f,.025f,.04f,.48f),"ContactShadow");
            surface=Material(hullShader,Color.white,"BoardSurface");surface.renderQueue=2000;
            standard=Own(ShipPrototypeMesh.Build(2));longShip=Own(ShipPrototypeMesh.Build(3));
            quad=Own(ShipPrototypeMesh.Quad());
        }
        private T Own<T>(T value) where T:UnityEngine.Object {owned.Add(value);return value;}
        private Material Material(Shader shader,Color color,string label)
        {var value=Own(new Material(shader){name=label,enableInstancing=true});value.SetColor("_Color",color);return value;}
        public Mesh Hull(int length)=>length==3 ? longShip : standard;
        public Material HullMaterial(int length,int slot)=>length==3 ? support : palette[Mathf.Clamp(slot,0,4)];
        public MeshRenderer AddHull(Transform parent,int length,int slot)=>Renderer("Volume",parent,Hull(length),HullMaterial(length,slot));
        public MeshRenderer AddShadow(Transform parent,int length)
        {
            var r=Renderer("ContactShadow",parent,quad,shadow);
            r.transform.localPosition=new Vector3(0,0,-.025f);
            r.transform.localScale=new Vector3(.90f,length-.04f,1);return r;
        }
        public void AddSurface(Transform parent,Rect bounds,Color color)
        {
            var r=Renderer("SeaSurface",parent,quad,surface);
            r.transform.localPosition=new Vector3(bounds.center.x,bounds.center.y,.03f);
            r.transform.localScale=new Vector3(bounds.width,bounds.height,1);
            var block=new MaterialPropertyBlock();block.SetColor("_Color",color);block.SetFloat("_Unlit",1);r.SetPropertyBlock(block);
        }
        private static MeshRenderer Renderer(string label,Transform parent,Mesh mesh,Material material)
        {
            var go=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;return renderer;
        }
        private void OnDestroy(){foreach(var value in owned)if(value!=null)Destroy(value);owned.Clear();}
    }
}
