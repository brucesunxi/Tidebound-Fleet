using System;
using Tidebound.Config;
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
                        validationMessage = $"{session.LevelId}: {session.Width}x{session.Height}, {session.Ships.Count} ships, initial Boss HP {session.Boss.InitialHp}. Layout legality only; solvability is not checked.";
                    messageType = MessageType.Info;
                }
                catch (Exception e) { validationMessage = e.Message; messageType = MessageType.Error; }
            }
            if (!string.IsNullOrEmpty(validationMessage)) EditorGUILayout.HelpBox(validationMessage, messageType);
        }
    }
}
