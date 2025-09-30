using ThanhDV.PackageManager.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThanhDV.PackageManager.UI
{
    public class PackageManagerWindow : EditorWindow
    {
        // UXML/USS will always reside in the same folder as this .cs file
        private const string UxmlFileName = "PackageManagerWindow.uxml";
        private const string UssFileName = "PackageManagerWindow.uss";

        private PackageManagerView _view;
        private PackageManagerPresenter _presenter;

        // Creates the menu item to open the window
        [MenuItem("Tools/Package Manager")]
        public static void ShowWindow()
        {
            PackageManagerWindow wnd = GetWindow<PackageManagerWindow>();
            wnd.titleContent = new GUIContent("ThanhDV's Package Manager");
            wnd.minSize = new Vector2(850, 500);
        }

        // Called when the window is created to build the UI
        public void CreateGUI()
        {
            // Get the root VisualElement of the window
            VisualElement root = rootVisualElement;

            // Load UXML and USS
            LoadUIAssets(root);

            // Initialize MVP components
            _view = new PackageManagerView(root);
            _presenter = new PackageManagerPresenter(_view);

            // Start the data loading process
            _presenter.LoadInitialData();
        }

        private void LoadUIAssets(VisualElement root)
        {
            // Resolve the folder of this script
            string scriptFolder = GetThisScriptFolder();
            string uxmlAssetPath = scriptFolder + "/" + UxmlFileName;
            string ussAssetPath = scriptFolder + "/" + UssFileName;

            // Load the UXML file that defines the structure
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
            }
            else
            {
                Debug.Log($"<color=red>[TPM] UXML not found at '{uxmlAssetPath}'. UI cannot be created!!!</color>");
                // Optionally build a fallback UI here if needed
            }

            // Load the USS file for styling
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussAssetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.Log($"<color=yellow>[TPM] USS not found at '{ussAssetPath}'. UI will use default styling!!!</color>");
            }
        }

        private string GetThisScriptFolder()
        {
            // 1) Best: resolve from the running window instance
            var thisScript = MonoScript.FromScriptableObject(this);
            if (thisScript != null)
            {
                var path = AssetDatabase.GetAssetPath(thisScript);
                if (!string.IsNullOrEmpty(path))
                {
                    return System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                }
            }

            // 2) Fallback: search by MonoScript type
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {nameof(PackageManagerWindow)}");
            foreach (var guid in guids)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (mono != null && mono.GetClass() == typeof(PackageManagerWindow))
                {
                    return System.IO.Path.GetDirectoryName(scriptPath).Replace('\\', '/');
                }
            }

            // 3) Final fallback: Assets
            return "Assets";
        }
    }
}
