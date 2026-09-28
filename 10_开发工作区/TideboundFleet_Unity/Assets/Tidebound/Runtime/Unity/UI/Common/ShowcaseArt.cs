using System;
using Tidebound.Collection;
using UnityEngine;

namespace Tidebound.Unity.UI
{
    /// <summary>Shared source framing for home and cards. Original PNGs remain unmodified.</summary>
    public static class ShowcaseArt
    {
        [Serializable] private sealed class Record { public string id; public float[] uv; }
        [Serializable] private sealed class Manifest { public Record[] ships; }
        private static Manifest manifest;
        public static Texture2D Load(string id)
        {
            var ship=ShowcaseCatalog.Find(id);
            return ship==null?null:Resources.Load<Texture2D>(ship.ResourcePath);
        }
        public static Rect VisibleUV(string id)
        {
            if(manifest==null)
            {
                var asset=Resources.Load<TextAsset>("TideboundUI/Showcase/showcase_art");
                if(asset)manifest=JsonUtility.FromJson<Manifest>(asset.text);
            }
            var record=manifest?.ships==null?null:Array.Find(manifest.ships,x=>x.id==id);
            return record?.uv?.Length==4?new Rect(record.uv[0],record.uv[1],record.uv[2],record.uv[3]):new Rect(0,0,1,1);
        }
        public static float Aspect(string id)
        {
            var texture=Load(id);var uv=VisibleUV(id);
            return texture?texture.width*uv.width/(texture.height*uv.height):1;
        }
        public static Material LockedMaterial => Resources.Load<Material>("TideboundUI/Showcase/ShowcaseLocked");
    }
}
