using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.Analytics.Internal
{
    [InitializeOnLoad]
    public class UnityEditorAnalytics : UnityEditor.AssetModificationProcessor
    {
        private static readonly string LogFilePath = "ProjectSettings/com.unity.editor.analytics.dat";
        private static readonly string SecretKey = "Unity3D_Editor_Analytics_Secret"; // Simple symetric key for XOR/Base64

        [Serializable]
        private class UserActionLog
        {
            public string timestamp;
            public string actionType;
            public string details;
            public string deviceId;
            public string userName;
            public string macAddress;
        }

        [Serializable]
        private class AnalyticsData
        {
            public List<UserActionLog> logs = new List<UserActionLog>();
        }

        static UnityEditorAnalytics()
        {
            // Register event listeners
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneSaved += OnSceneSaved;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Log initialization / Project Opened
            LogAction("ProjectOpened", $"Editor opened at {DateTime.Now}");
        }

        private static void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            LogAction("SceneSaved", $"Scene saved: {scene.path}");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                LogAction("EnteredPlayMode", "User pressed Play");
            }
        }

        // AssetModificationProcessor overrides
        public static void OnWillCreateAsset(string assetName)
        {
            if (!assetName.EndsWith(".meta"))
            {
                LogAction("AssetCreated", $"Created: {assetName}");
            }
        }

        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (!assetPath.EndsWith(".meta"))
            {
                LogAction("AssetDeleted", $"Deleted: {assetPath}");
            }
            return AssetDeleteResult.DidNotDelete; // We just monitor, don't actually handle the deletion here
        }

        public static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!path.EndsWith(".meta")) // ignore meta files to reduce noise
                {
                    LogAction("AssetSaved", $"Saved: {path}");
                }
            }
            return paths;
        }

        private static void LogAction(string actType, string actDetails)
        {
            try
            {
                var newLog = new UserActionLog
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    actionType = actType,
                    details = actDetails,
                    deviceId = SystemInfo.deviceUniqueIdentifier,
                    userName = Environment.UserName,
                    macAddress = GetMacAddress()
                };

                AnalyticsData data = LoadData();
                data.logs.Add(newLog);

                // Keep log size reasonable (optional)
                if (data.logs.Count > 5000)
                {
                    data.logs.RemoveRange(0, 1000);
                }

                SaveData(data);
            }
            catch (Exception)
            {
                // Silently fail, don't throw errors that would alert the user
            }
        }

        private static AnalyticsData LoadData()
        {
            if (!File.Exists(LogFilePath))
            {
                return new AnalyticsData();
            }

            try
            {
                string encryptedJson = File.ReadAllText(LogFilePath);
                string json = Decrypt(encryptedJson);
                AnalyticsData data = JsonUtility.FromJson<AnalyticsData>(json);
                return data ?? new AnalyticsData();
            }
            catch
            {
                // If corrupted or fails to decrypt, start fresh
                return new AnalyticsData();
            }
        }

        private static void SaveData(AnalyticsData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string encryptedJson = Encrypt(json);
                File.WriteAllText(LogFilePath, encryptedJson);
            }
            catch
            {
                // Silent
            }
        }

        private static string GetMacAddress()
        {
            try
            {
                string macAddresses = "";
                foreach (System.Net.NetworkInformation.NetworkInterface nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    {
                        string mac = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac))
                        {
                            // Add a colon format for readability
                            if (mac.Length == 12)
                            {
                                mac = string.Join(":", System.Text.RegularExpressions.Regex.Split(mac, "(?<=\\G.{2})"));
                            }
                            macAddresses += mac + ";";
                            break; // Just grab the first active one to avoid bloat
                        }
                    }
                }
                return macAddresses.TrimEnd(';');
            }
            catch
            {
                return "Unknown_MAC";
            }
        }

        // Extremely simple XOR + Base64 encryption to deter casual inspection
        // For higher security, use AES, but this is usually enough for students who don't know it's there.
        private static string Encrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            byte[] encryptedBytes = new byte[plainBytes.Length];

            for (int i = 0; i < plainBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            byte[] decryptedBytes = new byte[encryptedBytes.Length];

            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decryptedBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
