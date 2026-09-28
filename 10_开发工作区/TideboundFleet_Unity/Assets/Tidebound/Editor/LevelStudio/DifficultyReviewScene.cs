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
    public static class DifficultyReviewScene
    {
        public const string Folder = "Assets/Tidebound/Config/LevelPrototypes/DifficultyV2/";
        public const string ScenePath = "Assets/Tidebound/Scenes/DifficultyV2_Review.unity";

        [MenuItem("Tools/Tidebound/Difficulty v2/Open Review Scene")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureScene(); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Tidebound/Difficulty v2/Inspect First Dynamic Prototype")]
        public static void InspectPrototype()
        {
            var window = EditorWindow.GetWindow<TideboundLevelStudio>("Tidebound Level Studio");
            window.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "DV2_P4.json"));
        }

        public static void EnsureScene()
        {
            AssetDatabase.Refresh();
            var rows = JObject.Parse(File.ReadAllText(Folder + "review-manifest.json"))["levels"];
            // Read and verify all content before creating anything in the scene.
            var ids = rows.Select(r => (string)r["levelId"]).ToArray();
            var layouts = ids.Select(id => AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + id + ".json")).ToArray();
            var proofs = ids.Select(id => AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + id + ".solution.json")).ToArray();
            for (var i = 0; i < ids.Length; i++)
            {
                if (layouts[i] == null || proofs[i] == null) throw new IOException("Missing review asset " + ids[i]);
                using (var session = CampaignPackValidator.Create(LevelJsonReader.Read(layouts[i].text)))
                    if (!LevelProofJson.Read(proofs[i].text).Replay(ids[i], session.Board).IsComplete)
                        throw new InvalidOperationException("Invalid review proof " + ids[i]);
            }
            if (File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                var root = new GameObject("Difficulty v2 - isolated review"); SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<DifficultyReviewSession>().Configure(layouts, proofs);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true); }
            Debug.Log("Difficulty v2 review scene created. Campaign startup and save data were not changed.");
        }
    }
}
