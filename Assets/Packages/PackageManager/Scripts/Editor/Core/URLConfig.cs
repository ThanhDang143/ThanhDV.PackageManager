using System;
using ThanhDV.PackageManager.Helper;

namespace ThanhDV.PackageManager.Core
{
    [Serializable]
    public class URLConfig
    {
        public string UnityPackageRegistryURL;
        public string VerdaccioSearchURL;

        public URLConfig()
        {
            UnityPackageRegistryURL = Constant.DEFAULT_UnityPackageRegistryURL;
            VerdaccioSearchURL = Constant.DEFAULT_VerdaccioSearchURL;
        }
    }
}
