using System.Collections.Generic;
using System.Linq;
using ThanhDV.PackageManager.Core;
using UnityEditor;
using PackageInfo = ThanhDV.PackageManager.Core.PackageInfo;

namespace ThanhDV.PackageManager.UI
{
    /// <summary>
    /// Acts as the Presenter in an MVP pattern. It connects the View (PackageManagerView)
    /// with the Model (data from PackageManagerService). It handles all the business logic,
    /// such as fetching, filtering, and preparing data to be displayed by the View.
    /// </summary>
    public class PackageManagerPresenter
    {
        private readonly PackageManagerView _view;
        private readonly List<PackageInfo> _allPackages = new();
        private string _currentTab = "all-packages-button";

        public PackageManagerPresenter(PackageManagerView view)
        {
            _view = view;
            RegisterViewEvents();
        }

        private void RegisterViewEvents()
        {
            _view.OnPackageSelected += OnPackageSelectionChanged;
            _view.OnTabSelected += OnTabSelectionChanged;
            _view.OnRefreshClicked += LoadInitialData;
        }

        public void LoadInitialData()
        {
            _view.ShowEmptyMessage("Fetching packages...");

            PackageManagerService.Fetch(database =>
            {
                _allPackages.Clear();

                if (database.UnityPackages != null)
                {
                    foreach (var pkgData in database.UnityPackages.packages)
                    {
                        var latestVersion = pkgData.versions.Keys.Max();
                        _allPackages.Add(new PackageInfo
                        {
                            Name = pkgData.name,
                            DisplayName = pkgData.displayName,
                            Description = pkgData.description,
                            Version = latestVersion,
                            Source = PackageSource.UnityPackage
                        });
                    }
                }

                if (database.VerdaccioPackages != null)
                {
                    foreach (var pkgData in database.VerdaccioPackages.packages)
                    {
                        _allPackages.Add(new PackageInfo
                        {
                            Name = pkgData.name,
                            DisplayName = pkgData.displayName,
                            Description = pkgData.description,
                            Version = pkgData.version,
                            Source = PackageSource.Verdaccio
                        });
                    }
                }

                _allPackages.Sort((p1, p2) => string.Compare(p1.DisplayName, p2.DisplayName, System.StringComparison.Ordinal));

                FilterAndRefreshList();
                _view.SetLastUpdateTime(System.DateTime.Now);

                EditorUtility.ClearProgressBar();
            });
        }

        private void FilterAndRefreshList()
        {
            var filteredPackages = _currentTab switch
            {
                "unitypackage-button" => _allPackages.Where(p => p.Source == PackageSource.UnityPackage).ToList(),
                "verdaccio-button" => _allPackages.Where(p => p.Source == PackageSource.Verdaccio).ToList(),
                _ => new List<PackageInfo>(_allPackages), // "all-packages-button"
            };

            if (filteredPackages.Count == 0)
            {
                _view.ShowEmptyMessage("No packages found!!!");
            }
            else
            {
                _view.DisplayPackages(filteredPackages);
            }
        }

        private void OnPackageSelectionChanged(PackageInfo selectedPackage)
        {
            _view.UpdateDetails(selectedPackage);
        }

        private void OnTabSelectionChanged(string newTab)
        {
            _currentTab = newTab;
            FilterAndRefreshList();
        }
    }
}
