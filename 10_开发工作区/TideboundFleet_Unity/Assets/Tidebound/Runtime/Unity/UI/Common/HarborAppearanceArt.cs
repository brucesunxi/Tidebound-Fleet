using System;
using Tidebound.Collection;
using Tidebound.Unity.UI.VisualSamples;
using UnityEngine;

namespace Tidebound.Unity.UI
{
    public static class HarborAppearanceArt
    {
        private static VisualSampleGallery.SkinManifest skins;
        private static VisualSampleGallery.SceneManifest scenes;
        private static VisualSampleGallery.TrailManifest trails;
        public static VisualSampleGallery.SkinRecord Skin(string id)
        {
            if(skins==null)skins=JsonUtility.FromJson<VisualSampleGallery.SkinManifest>(Resources.Load<TextAsset>("TideboundUI/VisualSamples/ShipSkins/skin_catalog").text);
            var visual=AppearanceCatalog.VisualSkin(id);var index=int.Parse(visual.Substring(visual.Length-2))-1;
            return skins.skins[index];
        }
        public static Texture2D SceneTexture(string id)
        {
            if(scenes==null)scenes=JsonUtility.FromJson<VisualSampleGallery.SceneManifest>(Resources.Load<TextAsset>("TideboundUI/VisualSamples/Scenes/scene_catalog").text);
            if(string.IsNullOrEmpty(id))id=AppearanceCatalog.DefaultScene;
            var index=int.Parse(id.Substring(id.Length-2))-1;return Resources.Load<Texture2D>(scenes.scenes[index].resourcePath);
        }
        public static VisualSampleGallery.TrailRecord Trail(string id)
        {
            if(trails==null)trails=JsonUtility.FromJson<VisualSampleGallery.TrailManifest>(Resources.Load<TextAsset>("TideboundUI/VisualSamples/Trails/trail_catalog").text);
            if(string.IsNullOrEmpty(id))id=AppearanceCatalog.DefaultTrail;
            return trails.trails[int.Parse(id.Substring(id.Length-2))-1];
        }
        public static Rect UV(float[] uv)=>new Rect(uv[0],uv[1],uv[2],uv[3]);
    }
}
