using System;
using System.Collections.Generic;

namespace ThanhDV.PackageManager.Core
{
    public enum PackageSource
    {
        None,
        UnityPackage,
        Verdaccio
    }

    public class PackageInfo
    {
        public string DisplayName;
        public string Name;
        public string Version;
        public string Description;
        public PackageSource Source;
    }

    #region UnityPackage
    [Serializable]
    public class UnityPackageRegistry
    {
        public List<UnityPackageData> packages;
    }

    [Serializable]
    public class UnityPackageData
    {
        public string name;
        public string displayName;
        public string description;
        public Dictionary<string, string> versions; // <version, download URL>
    }
    #endregion

    #region Verdaccio
    [Serializable]
    public class VerdaccioRegistry
    {
        public List<VerdaccioPackage> packages;
    }

    [Serializable]
    public class VerdaccioPackage
    {
        public string name;
        public string displayName;
        public string description;
        public string version;
    }
    #endregion
}
