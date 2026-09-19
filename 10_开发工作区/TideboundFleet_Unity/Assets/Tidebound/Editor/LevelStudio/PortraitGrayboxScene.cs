using System.IO;
using System.Linq;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Unity.LevelDesign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tidebound.EditorTools
{
    public static class PortraitGrayboxScene
    {
        public const string ScenePath = "Assets/Tidebound/Scenes/Phase5R_PortraitGraybox.unity";
        private const string DataFolder = Phase5RTenLevelExport.Folder + "/";

        [MenuItem("Tools/Tidebound/Open Portrait Puzzle Graybox")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Tidebound/Validate Ten Candidate Catalog")]
        public static void ValidateCatalog()
        {
            ReadCatalog(out _, out _, out _);
            Debug.Log("10/10 candidate layouts and current proofs validated. Playtest approval remains pending.");
        }

        public static void EnsureScene()
        {
            ReadCatalog(out var manifest, out var layouts, out var proofs);
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                var root = new GameObject("Tidebound Portrait Puzzle");
                SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<PortraitPuzzleGraybox>().ConfigureAssets(manifest, layouts, proofs);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.Refresh();
            Debug.Log("Standalone portrait graybox scene saved. Project startup/build scenes were not changed.");
        }

        private static void ReadCatalog(out TextAsset manifest, out TextAsset[] layouts, out TextAsset[] proofs)
        {
            manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder + "manifest.json");
            layouts = Phase5RLevelRecipes.All.Select(r => AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder+r.LevelId+".json")).ToArray();
            proofs = Phase5RLevelRecipes.All.Select(r => AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder+r.LevelId+".solution.json")).ToArray();
            if (manifest==null || layouts.Any(x=>x==null) || proofs.Any(x=>x==null)) throw new IOException("Missing ten-candidate assets.");
            _ = new CandidateLevelCatalog(manifest.text,layouts.Select(x=>x.text),proofs.Select(x=>x.text));
        }
    }
}
