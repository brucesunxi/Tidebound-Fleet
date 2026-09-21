using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Reuses V1 geometry on a private render layer. No physics, coins or combat callbacks.</summary>
    public sealed class CollectionShipPreview : MonoBehaviour
    {
        private static int nextRig;
        private bool showcase;private float sway;private Quaternion baseRotation;private Vector3 basePosition;
        public Func<bool> AllowMotion;
        private RenderTexture texture;private Camera camera;private Transform ship;private MeshRenderer hull;private Image shot;private float shotTime=-1;private int direction;
        private ShipPrototypeResources resources;
        private HarborShowcaseModel showcaseModel;
        public void Initialize(bool showcase=false)
        {
            this.showcase=showcase;
            var rig=new GameObject("PreviewRig");rig.transform.SetParent(transform,false);rig.transform.position=new Vector3(5000+(++nextRig)*32,5000,0);
            resources=rig.AddComponent<ShipPrototypeResources>();resources.Initialize();
            var model=new GameObject("StandardShip");model.transform.SetParent(rig.transform,false);ship=model.transform;
            if(showcase){showcaseModel=model.AddComponent<HarborShowcaseModel>();showcaseModel.Build();ship.localScale=new Vector3(1.16f,1,1);}
            else {hull=resources.AddHull(ship,2,0);resources.AddShadow(ship,2);}
            baseRotation=showcase?Quaternion.Euler(66,0,-145):Quaternion.identity;ship.localRotation=baseRotation;
            basePosition=showcase?new Vector3(0,-.88f,0):Vector3.zero;ship.localPosition=basePosition;
            foreach(var t in rig.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
            var lens=new GameObject("PreviewCamera");lens.transform.SetParent(rig.transform,false);lens.transform.localPosition=new Vector3(0,0,-10);
            camera=lens.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=showcase?1.49f:1.2f;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.depth=-5;
            texture=new RenderTexture(showcase?640:192,showcase?640:192,16){name="TideboundShipPreview",antiAliasing=showcase?4:1};texture.Create();camera.targetTexture=texture;
            var image=gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            var dot=new GameObject("PreviewShot",typeof(RectTransform));dot.transform.SetParent(transform,false);shot=dot.AddComponent<Image>();shot.color=new Color(1,.85f,.3f);shot.raycastTarget=false;shot.rectTransform.sizeDelta=new Vector2(4,8);shot.enabled=false;
        }
        public void Present(int slot,bool owned)
        {
            if(hull==null)return;
            hull.sharedMaterial=resources.HullMaterial(2,slot);var block=new MaterialPropertyBlock();
            if(!owned)block.SetColor("_Color",new Color(.07f,.10f,.13f));hull.SetPropertyBlock(block);
        }
        public void Rotate(){direction=(direction+1)%4;ship.localRotation=Quaternion.Euler(0,0,direction*90);}
        public void Fire(){shotTime=0;shot.enabled=true;}
        private void Update()
        {
            if(showcase)
            {
                var animate=AllowMotion?.Invoke()==true;
                if(animate)sway+=Time.unscaledDeltaTime;
                showcaseModel.SetMotionPhase(sway);
                ship.localRotation=baseRotation*Quaternion.Euler(0,0,animate?Mathf.Sin(sway*1.2566f):0);
                ship.localPosition=basePosition+(animate?new Vector3(0,Mathf.Sin(sway*1.2566f)*.012f,0):Vector3.zero);
            }
            if(shotTime<0)return;shotTime+=Time.unscaledDeltaTime;
            shot.rectTransform.anchoredPosition=(Vector2)(ship.localRotation*Vector3.up)*Mathf.Lerp(0,45,shotTime/.3f);
            if(shotTime>=.3f){shot.enabled=false;shotTime=-1;}
        }
        private void OnDestroy(){if(camera!=null)camera.targetTexture=null;if(texture!=null){texture.Release();Destroy(texture);}}
    }
}
