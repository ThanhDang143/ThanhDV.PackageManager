using System.Collections.Generic;
using System.Linq;
using ThanhDV.PackageManager.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = ThanhDV.PackageManager.Core.PackageInfo;

namespace ThanhDV.PackageManager.UI
{
    public class ThanhDVPackageManagerWindow : EditorWindow
    {
        // UXML/USS will always reside in the same folder as this .cs file
        private const string UxmlFileName = "ThanhDVPackageManagerWindow.uxml";
        private const string UssFileName = "ThanhDVPackageManagerWindow.uss";

        // Sample data list (will be replaced with real data later)
        private List<PackageInfo> allPackages = new();
        private List<PackageInfo> currentlyDisplayedPackages = new();

        // UI components
        private ListView packageListView;
        private VisualElement detailsPane;
        private Label packageNameLabel;
        private Label packageVersionLabel;
        private Label packageDescriptionLabel;

        private string curTab = "all-packages-button";
        private ToolbarButton allPackagesButton;
        private ToolbarButton unityPackageButton;
        private ToolbarButton verdaccioButton;

        // Creates the menu item to open the window
        [MenuItem("Tools/PackageManager")]
        public static void ShowWindow()
        {
            ThanhDVPackageManagerWindow wnd = GetWindow<ThanhDVPackageManagerWindow>();
            wnd.titleContent = new GUIContent("ThanhDV's Package Manager");
            wnd.minSize = new Vector2(750, 500);
        }

        // Called when the window is created to build the UI
        public void CreateGUI()
        {
            // Get the root VisualElement of the window
            VisualElement root = rootVisualElement;

            // Resolve the folder of this script (works under both Assets/ and Packages/)
            string scriptFolder = GetThisScriptFolder();
            string uxmlAssetPath = scriptFolder + "/" + UxmlFileName;
            string ussAssetPath = scriptFolder + "/" + UssFileName;

            // Load the UXML file that defines the structure
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            if (visualTree != null)
            {
                try
                {
                    visualTree.CloneTree(root);
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"<color=red>[TPM] Failed to clone UXML '{uxmlAssetPath}.</color>\n{ex}");
                    BuildFallbackUI(root);
                }
            }
            else
            {
                Debug.Log($"<color=red>[TPM] UXML not found at '{uxmlAssetPath}'. Building a temporary UI in code to avoid a NullReference.</color>");
                BuildFallbackUI(root);
            }

            // Load the USS file for styling
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussAssetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.Log($"<color=yellow>[TPM] USS not found at '{ussAssetPath}'. UI will use default styling.</color>");
            }

            // Query UI references from UXML
            packageListView = root.Q<ListView>("package-list");
            detailsPane = root.Q<VisualElement>("details-pane");
            packageNameLabel = root.Q<Label>("package-name");
            packageVersionLabel = root.Q<Label>("package-version");
            packageDescriptionLabel = root.Q<Label>("package-description");

            if (packageListView == null || detailsPane == null || packageNameLabel == null || packageVersionLabel == null || packageDescriptionLabel == null)
            {
                Debug.Log("<color=red>[TPM] UI not initialized correctly (required elements are missing). Make sure the UXML contains elements named: 'package-list', 'details-pane', 'package-name', 'package-version', 'package-description'.</color>");
                return;
            }

            LoadAllPackageInfo();
            // Configure the ListView
            ConfigureListView();

            // Register selection change event
            packageListView.selectionChanged += OnPackageSelectionChange;

            // Hide the details pane when nothing is selected
            detailsPane.style.visibility = Visibility.Hidden;

            // Ensure initial split size is applied; if view data hasn't been restored yet,
            // set the fixed pane dimension programmatically.
            var split = root.Q<TwoPaneSplitView>();
            if (split != null)
            {
                // If no stored view data, apply initial size (matches UXML 300)
                try
                {
                    // Unity exposes 'fixedPaneInitialDimension' property for runtime split view
                    var prop = typeof(UnityEngine.UIElements.TwoPaneSplitView).GetProperty("fixedPaneInitialDimension");
                    if (prop != null)
                    {
                        var current = (float)prop.GetValue(split, null);
                        if (current <= 0f || Mathf.Abs(current - 300f) > 0.01f)
                        {
                            prop.SetValue(split, 300f);
                        }
                    }
                }
                catch { /* best-effort; ignore if API differs */ }
            }

            // Get references to tab buttons from UXML
            allPackagesButton = root.Q<ToolbarButton>("all-packages-button");
            unityPackageButton = root.Q<ToolbarButton>("unitypackage-button");
            verdaccioButton = root.Q<ToolbarButton>("verdaccio-button");

            if (allPackagesButton == null || unityPackageButton == null || verdaccioButton == null)
            {
                Debug.Log("<color=red>[TPM] Tab buttons not found in UXML. Ensure the toolbar and ToolbarButton elements use the UnityEditor.UIElements namespace and correct names.</color>");
            }
            else
            {
                // Register click events for each tab
                allPackagesButton.clicked += () => SwitchTab(allPackagesButton);
                unityPackageButton.clicked += () => SwitchTab(unityPackageButton);
                verdaccioButton.clicked += () => SwitchTab(verdaccioButton);
            }

            // ...rest of CreateGUI() remains unchanged...
            detailsPane.style.visibility = Visibility.Hidden;
        }

        private void LoadAllPackageInfo()
        {
            EditorUtility.DisplayProgressBar("Loading Packages", "Đang kết nối đến server....", 0f);

            PackageManagerService.Fetch(database =>
            {
                allPackages.Clear();

                if (database.UnityPackages != null)
                {
                    for (int i = 0; i < database.UnityPackages.packages.Count; i++)
                    {
                        UnityPackageData pkgData = database.UnityPackages.packages[i];
                        var lastestVersion = pkgData.versions.Keys.Max();

                        PackageInfo pkgInfo = new()
                        {
                            DisplayName = pkgData.displayName,
                            Name = pkgData.name,
                            Version = lastestVersion,
                            Description = pkgData.description,
                            Source = PackageSource.UnityPackage
                        };

                        allPackages.Add(pkgInfo);
                    }
                }

                if (database.VerdaccioPackages != null)
                {
                    for (int i = 0; i < database.VerdaccioPackages.packages.Count; i++)
                    {
                        VerdaccioPackage pkgData = database.VerdaccioPackages.packages[i];

                        PackageInfo pkgInfo = new()
                        {
                            DisplayName = pkgData.displayName,
                            Name = pkgData.name,
                            Version = pkgData.version,
                            Description = pkgData.description,
                            Source = PackageSource.Verdaccio
                        };

                        allPackages.Add(pkgInfo);
                    }
                }

                allPackages = allPackages.OrderBy(p => p.DisplayName).ToList();

                FilterAndRefreshList();

                EditorUtility.ClearProgressBar();
            });
        }

        private void FilterAndRefreshList()
        {
            currentlyDisplayedPackages = curTab switch
            {
                "unitypackage-button" => allPackages.Where(p => p.Source == PackageSource.UnityPackage).ToList(),
                "verdaccio-button" => allPackages.Where(p => p.Source == PackageSource.Verdaccio).ToList(),
                _ => new List<PackageInfo>(allPackages), // All
            };

            packageListView.itemsSource = currentlyDisplayedPackages;
            packageListView.Rebuild();
        }

        /// <summary>
        /// Handle switching between tabs
        /// </summary>
        private void SwitchTab(ToolbarButton selectedButton)
        {
            // 1. Deselect all buttons
            allPackagesButton.RemoveFromClassList("tab-button-selected");
            unityPackageButton.RemoveFromClassList("tab-button-selected");
            verdaccioButton.RemoveFromClassList("tab-button-selected");

            // 2. Select the clicked button
            selectedButton.AddToClassList("tab-button-selected");

            // 3. Re-filter the package list (logic to be implemented)
            curTab = selectedButton.name;
            FilterAndRefreshList();
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
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {nameof(ThanhDVPackageManagerWindow)}");
            foreach (var guid in guids)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (mono != null && mono.GetClass() == typeof(ThanhDVPackageManagerWindow))
                {
                    return System.IO.Path.GetDirectoryName(scriptPath).Replace('\\', '/');
                }
            }

            // 3) Final fallback: Assets
            return "Assets";
        }

        private void BuildFallbackUI(VisualElement root)
        {
            // Horizontal container with the list on the left and details on the right
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1f;

            // Left pane: ListView
            var leftPane = new VisualElement();
            leftPane.style.flexBasis = 260;
            leftPane.style.flexGrow = 0f;
            leftPane.style.flexShrink = 0f;
            leftPane.style.borderRightWidth = 1;
            leftPane.style.borderRightColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            var list = new ListView { name = "package-list" };
            list.style.flexGrow = 1f;
            leftPane.Add(list);

            // Right pane: Details
            var rightPane = new VisualElement { name = "details-pane" };
            rightPane.style.flexGrow = 1f;
            rightPane.style.flexDirection = FlexDirection.Column;
            rightPane.style.paddingLeft = 10;
            rightPane.style.paddingTop = 10;

            var nameLabel = new Label { name = "package-name", text = "" };
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.fontSize = 14;

            var versionLabel = new Label { name = "package-version", text = "" };
            var descriptionLabel = new Label { name = "package-description", text = "" };

            rightPane.Add(nameLabel);
            rightPane.Add(versionLabel);
            rightPane.Add(descriptionLabel);

            container.Add(leftPane);
            container.Add(rightPane);
            root.Add(container);
        }

        private void ConfigureListView()
        {
            // Create a visual item for each row in the list
            packageListView.makeItem = () => new Label();

            // Bind data from a PackageInfo to a visual item
            packageListView.bindItem = (element, index) =>
            {
                Label label = element as Label;
                label.text = currentlyDisplayedPackages[index].DisplayName;

                label.style.paddingLeft = 5;
                label.style.paddingTop = 2;
                label.style.paddingBottom = 2;
            };
        }

        private void OnPackageSelectionChange(IEnumerable<object> selectedItems)
        {
            // Get the selected package
            if (packageListView.selectedItem is not PackageInfo selectedPackage)
            {
                detailsPane.style.visibility = Visibility.Hidden;
                return;
            }

            // Update information in the details pane
            detailsPane.style.visibility = Visibility.Visible;
            packageNameLabel.text = selectedPackage.DisplayName;
            packageVersionLabel.text = $"Version: {selectedPackage.Version} | Name: {selectedPackage.Name}";
            packageDescriptionLabel.text = selectedPackage.Description;
        }
    }
}
