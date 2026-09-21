using UnityEditor;

namespace Tidebound.EditorTools
{
    public sealed class HarborResourceImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/") || !assetPath.EndsWith(".png"))return;
            var texture=(TextureImporter)assetImporter;texture.textureType=TextureImporterType.Default;
            texture.mipmapEnabled=false;texture.alphaIsTransparency=true;texture.isReadable=false;
            texture.wrapMode=UnityEngine.TextureWrapMode.Clamp;texture.filterMode=UnityEngine.FilterMode.Bilinear;
            texture.maxTextureSize=assetPath.Contains("Harbor_Background")?2048:assetPath.Contains("Home_GoldButton")?1024:256;
            texture.textureCompression=TextureImporterCompression.Compressed;
        }
    }
}
