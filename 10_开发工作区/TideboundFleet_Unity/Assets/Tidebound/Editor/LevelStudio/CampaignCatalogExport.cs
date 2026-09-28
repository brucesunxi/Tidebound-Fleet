using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Tidebound.Config;
using Tidebound.Unity.LevelDesign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tidebound.EditorTools
{
    public static class CampaignCatalogExport
    {
        public const string Folder = "Assets/Tidebound/Config/Levels/Campaign/";
        public static string AssetPath(string file) => file.StartsWith("P5R_Ten_", StringComparison.Ordinal)
            ? Phase5RTenLevelExport.Folder + "/" + file : Folder + file;
        [MenuItem("Tools/Tidebound/Campaign/Validate 30")]
        public static void Validate30() => Validate(30);
        [MenuItem("Tools/Tidebound/Campaign/Validate 100")]
        public static void Validate100() => Validate(100);
        [MenuItem("Tools/Tidebound/Campaign/Use Validated 30 In Portrait Scene")]
        public static void Use30() => Apply(30);
        [MenuItem("Tools/Tidebound/Campaign/Use Validated 100 In Portrait Scene")]
        public static void Use100() => Apply(100);

        private static string Validate(int count)
        {
            var manifest = File.ReadAllText(Folder + "manifest-" + count + ".json");
            var gate = JObject.Parse(File.ReadAllText(Folder + "validation-" + count + ".json"));
            if ((bool?)gate["passed"] != true || (int?)gate["count"] != count ||
                (string)gate["manifestSha256"] != CampaignPackValidator.Sha256(manifest))
                throw new InvalidOperationException("Run the production-model validation for this exact pack first.");
            CampaignPackValidator.Validate(manifest, file => File.ReadAllText(AssetPath(file)));
            Debug.Log("Campaign " + count + ": data/proofs/structure certified. Human/device acceptance remains pending.");
            return manifest;
        }
        private static void Apply(int count)
        {
            var text = Validate(count); AssetDatabase.Refresh();
            var existing = SceneManager.GetSceneByPath(PortraitGrayboxScene.ScenePath);
            if (existing.IsValid() && existing.isDirty)
                throw new InvalidOperationException("Portrait scene has unsaved edits. Save them before changing its catalog.");
            var scene = existing.IsValid() && existing.isLoaded ? existing : EditorSceneManager.OpenScene(
                PortraitGrayboxScene.ScenePath, Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PortraitPuzzleGraybox>(true)).Single();
                var rows = JObject.Parse(text)["levels"].ToArray();
                var layouts = rows.Select(r => AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath((string)r["layoutFile"]))).ToArray();
                var proofs = rows.Select(r => AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath((string)r["proofFile"]))).ToArray();
                if (layouts.Any(x => x == null) || proofs.Any(x => x == null)) throw new InvalidOperationException("Missing imported content.");
                Undo.RecordObject(game, "Set campaign catalog");
                game.ConfigureAssets(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "manifest-" + count + ".json"), layouts, proofs);
                EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save campaign scene.");
            }
            finally { if (!existing.IsValid() && !Application.isBatchMode) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
