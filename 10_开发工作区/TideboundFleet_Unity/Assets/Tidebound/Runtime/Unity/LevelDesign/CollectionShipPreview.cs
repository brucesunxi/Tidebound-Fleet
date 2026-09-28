using System;
using Tidebound.Collection;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Shared cosmetic presentation; gameplay skins retain their private 3D preview.</summary>
    public sealed class CollectionShipPreview : MonoBehaviour
    {
        private static int nextRig;
        public string DisplayedShowcaseId {get;private set;}
        private bool showcase;private float sway;private Quaternion baseRotation;private Vector3 basePosition;
        public Func<bool> AllowMotion;
        public float MotionSpeed {get;set;}=1;
        public float RockAmplitude {get;set;}=1;
        public float BobAmplitude {get;set;}=1;
        public float IllustrationFill {get;set;}=.68f;
        public float IllustrationCenterY {get;set;}=.46f;
        public void UseCelebrationMotion()
        { MotionSpeed=2.3f;RockAmplitude=1.95f;BobAmplitude=1.625f; }
        private RenderTexture texture;private Camera camera;private Transform ship;private MeshRenderer hull;private Image shot;private float shotTime=-1;private int direction;
        private ShipPrototypeResources resources;
        private HarborShowcaseModel showcaseModel;
        private Material reflectionMaterial;
        private Material waterMaterial;
        private RawImage renderImage, reflectedImage, waterImage;
        private GameObject rig;
        private bool preferIllustration;
        private int previewResolution;
        private string illustrationId;
        private Vector2 framedSize;
        public bool UsesIllustration {get;private set;}
        public void Initialize(bool showcase=false,int showcaseResolution=640,bool preferIllustration=true)
        {
            this.showcase=showcase;DisplayedShowcaseId=showcase?ShowcaseCatalog.DefaultId:null;
            this.preferIllustration=preferIllustration;previewResolution=showcaseResolution;
            var art=showcase&&preferIllustration?LoadIllustration(ShowcaseCatalog.DefaultId):null;
            if(art==null)BuildRig();
            if(showcase)
            {
                var reflection=Visual("WaterReflection",transform);
                reflectedImage=reflection.gameObject.AddComponent<RawImage>();reflectedImage.raycastTarget=false;reflectedImage.color=new Color(.30f,.82f,1,.38f);
                var shader=Resources.Load<Shader>("TideboundUI/ShipReflection");
                if(shader!=null){reflectionMaterial=new Material(shader){name="UI_Ship_Display_Reflection"};reflectedImage.material=reflectionMaterial;}
                var water=Visual("WaterContact",transform);
                waterImage=water.gameObject.AddComponent<RawImage>();waterImage.raycastTarget=false;
                var waterShader=Resources.Load<Shader>("TideboundUI/ShowcaseWaterUI");
                if(waterShader!=null){waterMaterial=new Material(waterShader){name="UI_Ship_Display_Water"};waterImage.material=waterMaterial;}
                waterImage.enabled=waterMaterial!=null;
            }
            renderImage=Visual("ShipRender",transform).gameObject.AddComponent<RawImage>();renderImage.raycastTarget=false;
            illustrationId=ShowcaseCatalog.DefaultId;BindPresentation(art);
            var dot=new GameObject("PreviewShot",typeof(RectTransform));dot.transform.SetParent(transform,false);shot=dot.AddComponent<Image>();shot.color=new Color(1,.85f,.3f);shot.raycastTarget=false;shot.rectTransform.sizeDelta=new Vector2(4,8);shot.enabled=false;
        }
        private void BuildRig()
        {
            rig=new GameObject("PreviewRig");rig.transform.SetParent(transform,false);rig.transform.position=new Vector3(5000+(++nextRig)*32,5000,0);
            resources=rig.AddComponent<ShipPrototypeResources>();resources.Initialize();
            var model=new GameObject("StandardShip");model.transform.SetParent(rig.transform,false);ship=model.transform;
            if(showcase){showcaseModel=model.AddComponent<HarborShowcaseModel>();showcaseModel.Build();ship.localScale=new Vector3(1.16f,1,1);}
            else {hull=resources.AddHull(ship,2,0);resources.AddShadow(ship,2);}
            baseRotation=showcase?Quaternion.Euler(66,0,-145):Quaternion.identity;ship.localRotation=baseRotation;
            basePosition=showcase?new Vector3(0,-.88f,0):Vector3.zero;ship.localPosition=basePosition;
            foreach(var t in rig.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
            var lens=new GameObject("PreviewCamera");lens.transform.SetParent(rig.transform,false);lens.transform.localPosition=new Vector3(0,0,-10);
            camera=lens.AddComponent<Camera>();camera.orthographic=true;camera.aspect=1;camera.orthographicSize=showcase?ShowcaseExtent():1.2f;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.depth=-5;
            texture=new RenderTexture(showcase?previewResolution:192,showcase?previewResolution:192,16){name="TideboundShipPreview",antiAliasing=showcase?4:1};texture.Create();camera.targetTexture=texture;
        }
        public void PresentShowcase(string id)
        {
            var definition=ShowcaseCatalog.Find(id);
            if(!showcase||definition==null||DisplayedShowcaseId==id)return;
            var art=preferIllustration?LoadIllustration(id):null;
            if(art!=null){illustrationId=id;BindPresentation(art);DisplayedShowcaseId=id;return;}
            if(rig==null)BuildRig();
            var parent=ship.parent;ship.gameObject.SetActive(false);Destroy(ship.gameObject);
            var model=new GameObject("StandardShip");model.transform.SetParent(parent,false);ship=model.transform;
            showcaseModel=model.AddComponent<HarborShowcaseModel>();showcaseModel.Build(definition.VisualIndex);
            ship.localScale=new Vector3(1.16f,1,1);ship.localRotation=baseRotation;ship.localPosition=basePosition;
            foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
            camera.orthographicSize=ShowcaseExtent();BindPresentation(null);DisplayedShowcaseId=id;
        }
        private static Texture2D LoadIllustration(string id)
        {
            return ShowcaseArt.Load(id);
        }
        private void BindPresentation(Texture2D art)
        {
            UsesIllustration=art!=null;
            if(rig!=null)rig.SetActive(!UsesIllustration);
            renderImage.texture=UsesIllustration?(Texture)art:texture;
            renderImage.rectTransform.localRotation=Quaternion.identity;
            SetAnchors(renderImage.rectTransform,UsesIllustration?new Vector2(.16f,.12f):Vector2.zero,UsesIllustration?new Vector2(.84f,.80f):Vector2.one);
            if(reflectedImage!=null)
            {
                reflectedImage.texture=renderImage.texture;
                SetAnchors(reflectedImage.rectTransform,UsesIllustration?new Vector2(.16f,.015f):new Vector2(0,-.16f),UsesIllustration?new Vector2(.84f,.17f):new Vector2(1,.17f));
                reflectedImage.color=new Color(.30f,.82f,1,UsesIllustration?.23f:.38f);
            }
            if(waterImage!=null)
            {
                waterImage.gameObject.SetActive(UsesIllustration);
                SetAnchors(waterImage.rectTransform,new Vector2(-.09f,.025f),new Vector2(1.09f,.275f));
            }
            if(UsesIllustration)FrameIllustration();
        }
        private void FrameIllustration()
        {
            framedSize=((RectTransform)transform).rect.size;
            var uv=ShowcaseArt.VisibleUV(illustrationId);renderImage.uvRect=uv;
            var aspect=ShowcaseArt.Aspect(illustrationId);
            var width=Mathf.Max(1,framedSize.x);var height=Mathf.Max(1,framedSize.y);
            var w=Mathf.Min(width*IllustrationFill,height*IllustrationFill*aspect);var h=w/aspect;
            SetAnchors(renderImage.rectTransform,new Vector2(.5f-w/width/2,IllustrationCenterY-h/height/2),new Vector2(.5f+w/width/2,IllustrationCenterY+h/height/2));
            if(reflectedImage!=null)
            {reflectedImage.uvRect=uv;SetAnchors(reflectedImage.rectTransform,new Vector2(.5f-w/width/2,.015f),new Vector2(.5f+w/width/2,.17f));}
        }
        public void SetShowcaseOwned(bool owned)
        {
            if(!showcase||renderImage==null)return;
            renderImage.color=Color.white;renderImage.material=owned?null:ShowcaseArt.LockedMaterial;
        }
        private static void SetAnchors(RectTransform rect,Vector2 min,Vector2 max)
        {rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        public void SetWaterVisible(bool visible)
        {if(waterImage!=null)waterImage.enabled=visible&&waterMaterial!=null;if(reflectedImage!=null)reflectedImage.enabled=visible;showcaseModel?.SetWaterVisible(visible);}
        public void Present(int slot,bool owned)
        {
            if(hull==null)return;
            hull.sharedMaterial=resources.HullMaterial(2,slot);var block=new MaterialPropertyBlock();
            if(!owned)block.SetColor("_Color",new Color(.07f,.10f,.13f));hull.SetPropertyBlock(block);
        }
        private float ShowcaseExtent()
        {
            // Presentation framing only. Fit once in rig space, with room for the small idle sway.
            var extent=1.3f;
            foreach(var vertex in ship.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                var point=baseRotation*Vector3.Scale(vertex,ship.localScale)+basePosition;
                extent=Mathf.Max(extent,Mathf.Abs(point.x),Mathf.Abs(point.y));
            }
            return extent*1.04f;
        }
        public void Rotate(){if(showcase)return;direction=(direction+1)%4;ship.localRotation=Quaternion.Euler(0,0,direction*90);}
        public void Fire(){if(showcase)return;shotTime=0;shot.enabled=true;}
        private void Update()
        {
            if(showcase)
            {
                var animate=AllowMotion?.Invoke()==true;
                if(animate)sway+=Time.unscaledDeltaTime*MotionSpeed;
                if(reflectionMaterial!=null)reflectionMaterial.SetFloat("_Phase",sway);
                if(waterMaterial!=null)waterMaterial.SetFloat("_Phase",sway);
                if(UsesIllustration)
                {
                    if(framedSize!=((RectTransform)transform).rect.size)FrameIllustration();
                    var wave=animate?Mathf.Sin(sway*1.2566f):0;
                    renderImage.rectTransform.localRotation=Quaternion.Euler(0,0,wave*.8f*RockAmplitude);
                    var bob=new Vector2(0,wave*((RectTransform)transform).rect.height*.004f*BobAmplitude);
                    renderImage.rectTransform.offsetMin=renderImage.rectTransform.offsetMax=bob;
                }
                else
                {
                    showcaseModel.SetMotionPhase(sway);
                    ship.localRotation=baseRotation*Quaternion.Euler(0,0,animate?Mathf.Sin(sway*1.2566f)*1.25f*RockAmplitude:0);
                    ship.localPosition=basePosition+(animate?new Vector3(0,Mathf.Sin(sway*1.2566f)*.027f*BobAmplitude,0):Vector3.zero);
                }
            }
            if(shotTime<0)return;shotTime+=Time.unscaledDeltaTime;
            shot.rectTransform.anchoredPosition=(Vector2)(ship.localRotation*Vector3.up)*Mathf.Lerp(0,45,shotTime/.3f);
            if(shotTime>=.3f){shot.enabled=false;shotTime=-1;}
        }
        private void OnDestroy(){if(camera!=null)camera.targetTexture=null;if(texture!=null){texture.Release();Destroy(texture);}if(reflectionMaterial!=null)Destroy(reflectionMaterial);if(waterMaterial!=null)Destroy(waterMaterial);}
        private static RectTransform Visual(string name,Transform parent)
        {var value=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();value.SetParent(parent,false);return value;}
    }
}
