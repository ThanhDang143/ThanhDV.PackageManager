using System;
using System.Collections.Generic;
using System.Linq;
using ThanhDV.PackageManager.Core;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ThanhDV.PackageManager.UI
{
    /// <summary>
    /// Manages the UI elements and user interactions for the package manager window.
    /// This class is responsible for querying elements from the UXML, binding data,
    /// and exposing events for user actions. It does not contain any business logic.
    /// </summary>
    public class PackageManagerView
    {
        // UI Components
        private readonly ListView _packageListView;
        private readonly VisualElement _detailsPane;
        private readonly Label _packageNameLabel;
        private readonly Label _packageVersionLabel;
        private readonly Label _packageDescriptionLabel;
        private readonly Label _emptyListLabel;

        private readonly ToolbarButton _allPackagesButton;
        private readonly ToolbarButton _unityPackageButton;
        private readonly ToolbarButton _verdaccioButton;

        private readonly Label _lastUpdateLabel;
        private readonly Button _refreshButton;

        // Events for user interaction
        public event Action<PackageInfo> OnPackageSelected;
        public event Action<string> OnTabSelected;
        public event Action OnRefreshClicked;

        public PackageManagerView(VisualElement root)
        {
            // Query UI references from the root VisualElement
            _packageListView = root.Q<ListView>("package-list");
            _detailsPane = root.Q<VisualElement>("details-pane");
            _packageNameLabel = root.Q<Label>("package-name");
            _packageVersionLabel = root.Q<Label>("package-version");
            _packageDescriptionLabel = root.Q<Label>("package-description");
            _emptyListLabel = root.Q<Label>("empty-list-label");

            _allPackagesButton = root.Q<ToolbarButton>("all-packages-button");
            _unityPackageButton = root.Q<ToolbarButton>("unitypackage-button");
            _verdaccioButton = root.Q<ToolbarButton>("verdaccio-button");

            _lastUpdateLabel = root.Q<Label>("last-update-label");
            _refreshButton = root.Q<Button>("refresh-button");

            if (_packageListView == null || _detailsPane == null || _packageNameLabel == null || _emptyListLabel == null || _allPackagesButton == null || _lastUpdateLabel == null || _refreshButton == null)
            {
                UnityEngine.Debug.Log($"<color=red>[TPM] Critical UI elements are missing from the UXML. Please check the UXML file!!!</color>");
                return;
            }

            ConfigureListView();
            RegisterCallbacks();

            // Initial state
            _detailsPane.style.visibility = Visibility.Hidden;
            _emptyListLabel.style.display = DisplayStyle.None;
        }

        private void RegisterCallbacks()
        {
            _packageListView.selectionChanged += (selectedItems) =>
            {
                var selectedPackage = selectedItems.FirstOrDefault() as PackageInfo;
                OnPackageSelected?.Invoke(selectedPackage);
            };

            _allPackagesButton.clicked += () => SwitchTab(_allPackagesButton);
            _unityPackageButton.clicked += () => SwitchTab(_unityPackageButton);
            _verdaccioButton.clicked += () => SwitchTab(_verdaccioButton);

            _refreshButton.clicked += () => OnRefreshClicked?.Invoke();
        }

        private void ConfigureListView()
        {
            _packageListView.makeItem = () =>
            {
                var container = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        paddingLeft = 5,
                        paddingRight = 5,
                        paddingTop = 2,
                        paddingBottom = 2,
                        minWidth = 0,
                        flexShrink = 1,
                        borderBottomWidth = 1,
                        borderBottomColor = new UnityEngine.Color(0.137f, 0.137f, 0.137f)
                    }
                };

                var nameLabel = new Label
                {
                    name = "packageName",
                    style =
                    {
                        flexGrow = 1,
                        flexBasis = 0,
                        borderBottomWidth = 0,
                        unityTextAlign = UnityEngine.TextAnchor.MiddleLeft,
                        whiteSpace = WhiteSpace.NoWrap,
                        textOverflow = TextOverflow.Ellipsis,
                        overflow = Overflow.Hidden
                    }
                };
                container.Add(nameLabel);

                var rightGroup = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        minWidth = 0
                    }
                };

                var versionLabel = new Label
                {
                    name = "packageVersion",
                    style =
                    {
                        color = UnityEngine.Color.gray,
                        marginLeft = 8,
                        borderBottomWidth = 0,
                    }
                };
                rightGroup.Add(versionLabel);

                var statusIcon = new Image
                {
                    name = "packageStatusIcon",
                    style =
                    {
                        width = 16,
                        height = 16,
                        marginLeft = 6,
                        unityBackgroundImageTintColor = UnityEngine.Color.white,
                        visibility = Visibility.Visible
                    }
                };
                rightGroup.Add(statusIcon);

                container.Add(rightGroup);

                return container;
            };

            _packageListView.bindItem = (element, index) =>
            {
                var nameLabel = element.Q<Label>("packageName");
                var versionLabel = element.Q<Label>("packageVersion");
                var statusIcon = element.Q<Image>("packageStatusIcon");

                if (_packageListView.itemsSource[index] is PackageInfo package)
                {
                    nameLabel.text = package.DisplayName;
                    versionLabel.text = package.Version;
                    // Ngẫu nhiên hiển thị icon trạng thái
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        statusIcon.image = UnityEditor.EditorGUIUtility.IconContent("d_Progress").image;
                        statusIcon.style.visibility = Visibility.Visible;
                    }
                    else
                    {
                        statusIcon.image = null;
                        statusIcon.style.visibility = Visibility.Hidden; // vẫn giữ chỗ
                    }
                }
                else
                {
                    nameLabel.text = "Unknown";
                    versionLabel.text = "";
                    statusIcon.image = null;
                    statusIcon.style.visibility = Visibility.Hidden;
                }
            };
        }

        private void SwitchTab(ToolbarButton selectedButton)
        {
            _allPackagesButton.RemoveFromClassList("tab-button-selected");
            _unityPackageButton.RemoveFromClassList("tab-button-selected");
            _verdaccioButton.RemoveFromClassList("tab-button-selected");

            selectedButton.AddToClassList("tab-button-selected");

            OnTabSelected?.Invoke(selectedButton.name);
        }

        public void DisplayPackages(List<PackageInfo> packages)
        {
            _packageListView.style.display = DisplayStyle.Flex;
            _emptyListLabel.style.display = DisplayStyle.None;

            _packageListView.itemsSource = packages;
            _packageListView.Rebuild();
        }

        public void ShowEmptyMessage(string message)
        {
            _packageListView.style.display = DisplayStyle.None;
            _emptyListLabel.text = message;
            _emptyListLabel.style.display = DisplayStyle.Flex;
        }

        public void UpdateDetails(PackageInfo package)
        {
            if (package == null)
            {
                _detailsPane.style.visibility = Visibility.Hidden;
                return;
            }

            _detailsPane.style.visibility = Visibility.Visible;
            _packageNameLabel.text = package.DisplayName;
            _packageVersionLabel.text = $"{package.Name} | {package.Version}";
            _packageDescriptionLabel.text = package.Description;
        }

        public void SetLastUpdateTime(DateTime time)
        {
            _lastUpdateLabel.text = $"Last update {time:MMM dd, HH:mm}";
        }
    }
}
