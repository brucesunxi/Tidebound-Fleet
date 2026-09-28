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
            if(assetPath.EndsWith("/Skins/UI_HarborHeader_v1.png"))
            {
                // Shared full-width title: the general 256px icon budget visibly blurs it.
                texture.maxTextureSize=2048;texture.npotScale=TextureImporterNPOTScale.None;
                texture.alphaSource=TextureImporterAlphaSource.FromInput;
                var header=texture.GetDefaultPlatformTextureSettings();header.maxTextureSize=2048;
                header.textureCompression=TextureImporterCompression.Uncompressed;texture.SetPlatformTextureSettings(header);return;
            }
            if(assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/Gameplay/"))
            {
                texture.maxTextureSize=2048;texture.npotScale=TextureImporterNPOTScale.None;texture.alphaSource=TextureImporterAlphaSource.FromInput;
                texture.textureCompression=TextureImporterCompression.Uncompressed;return;
            }
            if(assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/HeroButtons/"))
            {
                texture.maxTextureSize=2048;texture.npotScale=TextureImporterNPOTScale.None;
                texture.alphaSource=TextureImporterAlphaSource.FromInput;
                var hero=texture.GetDefaultPlatformTextureSettings();hero.maxTextureSize=2048;
                hero.textureCompression=TextureImporterCompression.Uncompressed;texture.SetPlatformTextureSettings(hero);return;
            }
            if(assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/Commerce/"))
            {
                texture.maxTextureSize=2048;texture.npotScale=TextureImporterNPOTScale.None;
                var background=texture.GetDefaultPlatformTextureSettings();background.maxTextureSize=2048;
                background.textureCompression=TextureImporterCompression.CompressedHQ;background.compressionQuality=100;
                texture.SetPlatformTextureSettings(background);return;
            }
            if(assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/Approved/"))
            {
                texture.maxTextureSize=4096;texture.npotScale=TextureImporterNPOTScale.None;
                texture.alphaSource=TextureImporterAlphaSource.FromInput;
                var approved=texture.GetDefaultPlatformTextureSettings();approved.maxTextureSize=4096;
                approved.textureCompression=TextureImporterCompression.Uncompressed;texture.SetPlatformTextureSettings(approved);return;
            }
            if(assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/VisualSamples/Trails/"))
            {
                texture.maxTextureSize=2048;texture.npotScale=TextureImporterNPOTScale.None;
                texture.alphaSource=TextureImporterAlphaSource.FromInput;texture.mipmapEnabled=true;texture.filterMode=UnityEngine.FilterMode.Trilinear;
                var trailPlatform=texture.GetDefaultPlatformTextureSettings();trailPlatform.maxTextureSize=2048;
                trailPlatform.textureCompression=TextureImporterCompression.Uncompressed;texture.SetPlatformTextureSettings(trailPlatform);return;
            }
            if(assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/VisualSamples/Scenes/")||assetPath.StartsWith("Assets/Tidebound/Resources/TideboundUI/VisualSamples/ShipSkins/"))
            {
                texture.maxTextureSize=2048;texture.npotScale=TextureImporterNPOTScale.None;
                if(assetPath.Contains("/ShipSkins/")){texture.mipmapEnabled=true;texture.filterMode=UnityEngine.FilterMode.Trilinear;}
                var platform=texture.GetDefaultPlatformTextureSettings();platform.maxTextureSize=2048;
                platform.textureCompression=TextureImporterCompression.CompressedHQ;platform.compressionQuality=100;
                texture.SetPlatformTextureSettings(platform);return;
            }
            texture.maxTextureSize=assetPath.Contains("Harbor_Background")?2048:
                assetPath.Contains("/Showcase/")||assetPath.Contains("Home_GoldButton")?1024:256;
            texture.textureCompression=TextureImporterCompression.Compressed;
        }
    }
}
