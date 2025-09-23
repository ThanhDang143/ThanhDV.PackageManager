using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using ThanhDV.PackageManager.Helper;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ThanhDV.PackageManager.Core
{
    public static class PackageManagerService
    {
        private static URLConfig config;

        static PackageManagerService()
        {
            LoadConfig();
        }

        public static void Fetch(Action<PackageDatabase> onCompleted)
        {
            if (config == null || string.IsNullOrEmpty(config.VerdaccioSearchURL) || string.IsNullOrEmpty(config.UnityPackageRegistryURL))
            {
                Debug.Log("<color=red>[TPM] URL chưa được cấu hình. Vui lòng kiểm tra file config.json.</color>");
                onCompleted?.Invoke(new PackageDatabase()); // Trả về database rỗng
                return;
            }

            PackageDatabase database = new();
            int completedTasks = 0;
            int totalTasks = 2;
            FetchUnityPackage(OnFetchUnityPackageSuccess, OnFetchTaskCompleted);
            FetchVerdaccioPackage(OnFetchVerdaccioPackageSuccess, OnFetchTaskCompleted);

            void OnFetchUnityPackageSuccess(UnityPackageRegistry packages)
            {
                database.UnityPackages = packages;
                OnFetchTaskCompleted();
            }

            void OnFetchVerdaccioPackageSuccess(VerdaccioRegistry packages)
            {
                database.VerdaccioPackages = packages;
                OnFetchTaskCompleted();
            }

            void OnFetchTaskCompleted()
            {
                completedTasks++;
                if (completedTasks >= totalTasks)
                {
                    onCompleted?.Invoke(database);
                }
            }
        }

        private static void FetchUnityPackage(Action<UnityPackageRegistry> onSuccess, Action onError = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(config.UnityPackageRegistryURL);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            operation.completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = "{\"packages\":" + request.downloadHandler.text + "}";
                    UnityPackageRegistry registry = JsonConvert.DeserializeObject<UnityPackageRegistry>(json);
                    onSuccess?.Invoke(registry);
                }
                else
                {
                    Debug.Log($"<color=red>[TPM] Lỗi khi tải UnityPackage registry.</color>\n{request.error}");
                    onError?.Invoke();
                }

                request.Dispose();
            };
        }

        private static void FetchVerdaccioPackage(Action<VerdaccioRegistry> onSuccess, Action onError = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(config.VerdaccioSearchURL);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            operation.completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    List<VerdaccioPackage> packages = JsonConvert.DeserializeObject<List<VerdaccioPackage>>(json);
                    VerdaccioRegistry result = new() { packages = packages };
                    onSuccess?.Invoke(result);
                }
                else
                {
                    Debug.Log($"<color=red>[TPM] Lỗi khi truy vấn Verdaccio.</color>\n{request.error}");
                    onError?.Invoke();
                }
                request.Dispose();
            };
        }

        #region Helper

        private static void LoadConfig()
        {
            try
            {
                if (!TryGetConfigPath(out string configPath)) return;

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<URLConfig>(json);
                }
                else
                {
                    config = new URLConfig();
                    string jsonConfig = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(configPath, jsonConfig);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"<color=red>[TPM] Đã xảy ra lỗi khi đọc file config.json. Fallback to default settings.</color>\n{e.Message}");
                config = new URLConfig();
            }
        }

        /// <summary>
        /// Find path of config.json
        /// </summary>
        private static bool TryGetConfigPath(out string path)
        {
            path = "";
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {nameof(PackageManagerService)}");
            if (guids.Length == 0)
            {
                Debug.Log($"<color=red>[TPM] Không thể tìm thấy script '{nameof(PackageManagerService)}.cs'.</color>");
                return false;
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string scriptFolder = Path.GetDirectoryName(scriptPath);
            path = Path.Combine(scriptFolder, Constant.CONFIG_FILE_NAME);
            return true;
        }

        public class PackageDatabase
        {
            public UnityPackageRegistry UnityPackages;
            public VerdaccioRegistry VerdaccioPackages;
        }
        #endregion
    }
}
