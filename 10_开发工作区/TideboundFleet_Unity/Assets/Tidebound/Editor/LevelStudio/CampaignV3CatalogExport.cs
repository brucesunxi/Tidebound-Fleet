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
    public static class CampaignV3CatalogExport
    {
        public const string Folder="Assets/Tidebound/Config/Levels/CampaignV3/";
        [MenuItem("Tools/Tidebound/Campaign V3/Validate 100 And Tool Recoveries")]
        public static void Validate100()
        {
            var text=File.ReadAllText(Folder+"manifest-100.json");
            var report=CampaignV3Validator.Validate(text,file=>File.ReadAllText(Folder+file));
            Debug.Log("Campaign V3 validated: "+report.ToString());
        }
        [MenuItem("Tools/Tidebound/Campaign V3/Use Validated 100 In Current Portrait Layout")]
        public static void Apply100()
        {
            Validate100();AssetDatabase.Refresh();
            var existing=SceneManager.GetSceneByPath(PortraitGrayboxScene.ScenePath);
            if(existing.IsValid()&&existing.isDirty)throw new InvalidOperationException("Save the current portrait layout before replacing its level catalog.");
            var scene=existing.IsValid()&&existing.isLoaded?existing:EditorSceneManager.OpenScene(PortraitGrayboxScene.ScenePath,Application.isBatchMode?OpenSceneMode.Single:OpenSceneMode.Additive);
            try
            {
                var data=JObject.Parse(File.ReadAllText(Folder+"manifest-100.json"));
                var game=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PortraitPuzzleGraybox>(true)).Single();
                var layouts=data["levels"].Select(r=>AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+(string)r["layoutFile"]))
                    .Concat(data["previousRevisions"].Select(r=>AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignCatalogExport.AssetPath((string)r["layoutFile"])))).ToArray();
                var proofs=data["levels"].Select(r=>AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+(string)r["proofFile"])).ToArray();
                if(layouts.Any(x=>x==null)||proofs.Any(x=>x==null))throw new InvalidOperationException("Missing campaign or archive asset");
                Undo.RecordObject(game,"Install campaign V3");
                game.ConfigureAssets(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"manifest-100.json"),layouts,proofs);
                EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save campaign references");
            }
            finally{if(!existing.IsValid()&&!Application.isBatchMode)EditorSceneManager.CloseScene(scene,true);}
        }
    }
    public sealed class CampaignV3Browser : EditorWindow
    {
        private Vector2 scroll;
        [MenuItem("Tools/Tidebound/Campaign V3/Browse New 100 Levels")]
        public static void Open()=>GetWindow<CampaignV3Browser>("新版100关");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("新版船阵与通关证明。编辑会另存修订，不覆盖已发布排列。真人难度与真机触控仍待测试。",MessageType.Info);
            var path=CampaignV3CatalogExport.Folder+"manifest-100.json";if(!File.Exists(path))return;
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(var row in JObject.Parse(File.ReadAllText(path))["levels"])
            {
                var title=$"第 {row["number"]} 关    {row["ships"]} 艘 / HP {(int)row["ships"]*10}    {row["riskPoints"]} 个已验证风险点";
                if(GUILayout.Button(title))GetWindow<TideboundLevelStudio>("Tidebound Level Studio").OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(CampaignV3CatalogExport.Folder+(string)row["layoutFile"]));
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
