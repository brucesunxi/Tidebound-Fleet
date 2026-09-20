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
        private Mesh toyStandard,toyBlue,toyLong;
        private Material toyMaterial,toyBlueMaterial,toyLongMaterial;
        private bool toyStudy;
        private readonly List<HullBinding> hulls=new List<HullBinding>();
        private struct HullBinding { public MeshRenderer Renderer;public int Length,Slot; }
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
        public Mesh Hull(int length)=>toyStudy ? (length==3 ? toyLong : toyStandard) : length==3 ? longShip : standard;
        private Mesh HullForSlot(int length,int slot)=>toyStudy && length!=3 && slot==1 ? toyBlue : Hull(length);
        public Material HullMaterial(int length,int slot)=>toyStudy ? (length==3 ? toyLongMaterial : slot==1 ? toyBlueMaterial : toyMaterial) : length==3 ? support : palette[Mathf.Clamp(slot,0,4)];
        public MeshRenderer AddHull(Transform parent,int length,int slot)
        {
            var renderer=Renderer("Volume",parent,HullForSlot(length,slot),HullMaterial(length,slot));
            hulls.Add(new HullBinding{Renderer=renderer,Length=length,Slot=slot});return renderer;
        }
        /// <summary>Developer visual comparison; does not select skins or change the save. Off by default.</summary>
        public void SetToyStudy(bool enabled)
        {
            if(enabled && toyStandard==null)
            {
                toyStandard=Own(ShipPrototypeMesh.BuildToy(2));toyBlue=Own(ShipPrototypeMesh.BuildToy(2,true));toyLong=Own(ShipPrototypeMesh.BuildToy(3));
                toyMaterial=Material(surface.shader,new Color(.10f,.49f,.61f),"ToyStudy_Default");
                toyBlueMaterial=Material(surface.shader,new Color(.09f,.29f,.60f),"ToyStudy_Blue");
                toyLongMaterial=Material(surface.shader,new Color(.37f,.47f,.51f),"ToyStudy_FixedLong");
            }
            toyStudy=enabled;
            foreach(var h in hulls)if(h.Renderer!=null)
            {h.Renderer.GetComponent<MeshFilter>().sharedMesh=HullForSlot(h.Length,h.Slot);h.Renderer.sharedMaterial=HullMaterial(h.Length,h.Slot);}
        }
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
