using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Analytics.Internal
{
    public class TeacherReviewTool : EditorWindow
    {
        private static readonly string LogFilePath = "ProjectSettings/com.unity.editor.analytics.dat";
        private Vector2 scrollPos;
        private string decryptedLogContent = "";

        // Add a menu item to open this window.
        // We put it under an obscure name or you could require a password.
        [MenuItem("Window/Analysis/Unity Editor Analytics Log (Teacher Only)", false, 100)]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            TeacherReviewTool window = (TeacherReviewTool)EditorWindow.GetWindow(typeof(TeacherReviewTool));
            window.titleContent = new GUIContent("Analytics Log");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Student Tracking Logs", EditorStyles.boldLabel);

            if (GUILayout.Button("Load and Decrypt Logs"))
            {
                LoadLogs();
            }

            if (GUILayout.Button("Clear Logs (DANGER)"))
            {
                if (EditorUtility.DisplayDialog("Clear Logs", "Are you sure you want to clear all student tracking logs? This cannot be undone.", "Yes", "No"))
                {
                    ClearLogs();
                }
            }

            GUILayout.Space(10);

            // Display the decrypted json or parsed string
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height - 100));
            
            EditorGUILayout.TextArea(decryptedLogContent, GUILayout.ExpandHeight(true));
            
            EditorGUILayout.EndScrollView();
        }

        private void LoadLogs()
        {
            if (!File.Exists(LogFilePath))
            {
                decryptedLogContent = "No log file found at: " + LogFilePath;
                return;
            }

            try
            {
                string encryptedJson = File.ReadAllText(LogFilePath);
                // Call the static decrypt method from the analytics script
                string json = UnityEditorAnalytics.Decrypt(encryptedJson);
                
                // Format nicely
                decryptedLogContent = json;
            }
            catch (Exception ex)
            {
                decryptedLogContent = "Error decrypting log file: " + ex.Message;
            }
        }

        private void ClearLogs()
        {
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
                decryptedLogContent = "Logs cleared.";
            }
            else
            {
                decryptedLogContent = "No log file found to clear.";
            }
        }
    }
}
