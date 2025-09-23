# Package Manager

Description...

### PackageManager window workflow
1. `LoadAllPackageInfo()` kết nối internet tìm, tải và đổ tất cả `PackageInfo` được tìm thấy vào `allPackages`
2. Khi người dùng chọn tab (All/UnityPackage/Verdaccio) hoặc tìm kiếm `FilterAndRefreshList()` sẽ được gọi.
3. `FilterAndRefreshList()` thực hiện lọc dữ liệu trong `allPackages` và đưa vào `currentlyDisplayedPackages`.
4. Cuối cùng là hiển thị `PackageInfo` trong `currentlyDisplayedPackages` lên cửa sổ