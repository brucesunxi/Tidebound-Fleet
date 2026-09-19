using System;
using Tidebound.Config;
using Tidebound.LevelDesign;
using UnityEditor;
using UnityEngine;

namespace Tidebound.EditorTools
{
    [CustomEditor(typeof(LevelConfigSO))]
    public sealed class LevelConfigInspector : Editor
    {
        private string validationMessage;
        private MessageType messageType;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Validate data only"))
            {
                try
                {
                    using (var session = LevelConfigLoader.Load((LevelConfigSO)target))
                    {
                        var report = LevelStructureAnalyzer.Analyze(session.InitialBoard);
                        validationMessage = $"{session.LevelId}: {session.Width}x{session.Height}, {session.Ships.Count} ships, " +
                            $"Boss HP {session.Boss.InitialHp}, hash {BoardStateFingerprint.Compute(session.InitialBoard)}, " +
                            $"exits {report.Dependencies.InitialExitCount}, moves {report.Dependencies.InitialMoveCount}, " +
                            $"depth {report.Dependencies.DependencyDepth}, cycles {report.Dependencies.Cycles.Count}.";
                        if (LevelProductionProfiles.TryGetCandidate(session.Width, session.Height, out var profile))
                        {
                            var production = LevelProductionValidator.Validate(session.InitialBoard, profile);
                            validationMessage += production.IsValid ? $" Production profile {profile.Id} passed." :
                                " Production issues: " + string.Join(" | ", production.Issues);
                        }
                    }
                    messageType = MessageType.Info;
                }
                catch (Exception e) { validationMessage = e.Message; messageType = MessageType.Error; }
            }
            if (!string.IsNullOrEmpty(validationMessage)) EditorGUILayout.HelpBox(validationMessage, messageType);
        }
    }
}
