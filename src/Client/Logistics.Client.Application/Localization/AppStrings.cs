namespace Logistics.Client.Application.Localization;

/// <summary>
/// Centralized localization strings defaulting to Vietnamese (<c>vi-VN</c>) with optional English (<c>en-US</c>) switching.
/// Structured cleanly for future resource-file (.resx) externalization.
/// </summary>
public static class AppStrings
{
    public const string DefaultCulture = "vi-VN";
    private static string _culture = DefaultCulture;

    public static string CurrentCulture
    {
        get => _culture;
        set => _culture = string.Equals(value, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US" : "vi-VN";
    }

    public static event EventHandler? LanguageChanged;

    public static void SetCulture(string cultureCode)
    {
        CurrentCulture = cultureCode;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string T(string key)
    {
        var map = _culture == "en-US" ? En : Vi;
        if (map.TryGetValue(key, out var val)) return val;
        if (Vi.TryGetValue(key, out var fallback)) return fallback;
        return key;
    }

    private static readonly Dictionary<string, string> Vi = new(StringComparer.Ordinal)
    {
        ["AppTitle"] = "HỆ THỐNG QUẢN LÝ LOGISTICS",
        ["LoginTitle"] = "Đăng nhập hệ thống",
        ["UsernameOrEmail"] = "Tên đăng nhập hoặc Email",
        ["Password"] = "Mật khẩu",
        ["ShowPassword"] = "Hiện mật khẩu",
        ["RememberUsername"] = "Ghi nhớ tên đăng nhập",
        ["SignIn"] = "Đăng nhập",
        ["SigningIn"] = "Đang xác thực...",
        ["Logout"] = "Đăng xuất",
        ["LogoutAll"] = "Đăng xuất tất cả thiết bị",
        ["AllBranchesScope"] = "Tất cả chi nhánh được cấp",
        ["BranchLabel"] = "Chi nhánh",
        ["Notifications"] = "Thông báo",
        ["Refresh"] = "Làm mới",
        ["Search"] = "Tìm kiếm",
        ["Filter"] = "Lọc",
        ["Create"] = "Thêm mới",
        ["Edit"] = "Chỉnh sửa",
        ["Save"] = "Lưu thay đổi",
        ["Cancel"] = "Hủy",
        ["Close"] = "Đóng",
        ["Delete"] = "Xóa",
        ["ExportCsv"] = "Xuất CSV",
        ["Loading"] = "Đang tải dữ liệu...",
        ["EmptyData"] = "Không có dữ liệu phù hợp.",
        ["Nav.Dashboard"] = "Bảng điều khiển",
        ["Nav.Organization"] = "Tổ chức & Nhân sự",
        ["Nav.CRM"] = "Khách hàng (CRM)",
        ["Nav.Catalog"] = "Danh mục hàng hóa",
        ["Nav.Sales"] = "Đơn hàng (Sales)",
        ["Nav.Shipping"] = "Vận đơn & Giao nhận",
        ["Nav.Carrier"] = "Đối tác vận chuyển (3PL)",
        ["Nav.Warehouse"] = "Kho bãi & Phân loại bưu kiện",
        ["Nav.Inventory"] = "Tồn kho vật tư / SKU",
        ["Nav.Finance"] = "Tài chính & Công nợ",
        ["Nav.COD"] = "Quản lý thu hộ (COD)",
        ["Nav.Notifications"] = "Trung tâm thông báo",
        ["Nav.Reports"] = "Báo cáo & Tác vụ nền",
        ["Nav.Settings"] = "Tài khoản & Cài đặt",
        ["ConfirmDeleteTitle"] = "Xác nhận thao tác",
        ["SessionExpiredTitle"] = "Phiên đăng nhập hết hạn",
        ["SessionExpiredMessage"] = "Phiên làm việc của bạn đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại.",
        ["ApiUnreachable"] = "Không thể kết nối tới máy chủ API. Vui lòng kiểm tra URL cấu hình hoặc kết nối mạng."
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        ["AppTitle"] = "LOGISTICS MANAGEMENT SYSTEM",
        ["LoginTitle"] = "System Sign In",
        ["UsernameOrEmail"] = "Username or Email",
        ["Password"] = "Password",
        ["ShowPassword"] = "Show password",
        ["RememberUsername"] = "Remember username",
        ["SignIn"] = "Sign In",
        ["SigningIn"] = "Authenticating...",
        ["Logout"] = "Sign Out",
        ["LogoutAll"] = "Sign Out All Sessions",
        ["AllBranchesScope"] = "All Authorized Branches",
        ["BranchLabel"] = "Branch",
        ["Notifications"] = "Notifications",
        ["Refresh"] = "Refresh",
        ["Search"] = "Search",
        ["Filter"] = "Filter",
        ["Create"] = "Create",
        ["Edit"] = "Edit",
        ["Save"] = "Save Changes",
        ["Cancel"] = "Cancel",
        ["Close"] = "Close",
        ["Delete"] = "Delete",
        ["ExportCsv"] = "Export CSV",
        ["Loading"] = "Loading data...",
        ["EmptyData"] = "No matching records found.",
        ["Nav.Dashboard"] = "Dashboard",
        ["Nav.Organization"] = "Organization & Staff",
        ["Nav.CRM"] = "Customers (CRM)",
        ["Nav.Catalog"] = "Product Catalog",
        ["Nav.Sales"] = "Sales Orders",
        ["Nav.Shipping"] = "Shipments & Delivery",
        ["Nav.Carrier"] = "Carrier Integration",
        ["Nav.Warehouse"] = "Warehouse & Parcel Hub",
        ["Nav.Inventory"] = "Physical Inventory",
        ["Nav.Finance"] = "Finance & Billing",
        ["Nav.COD"] = "Cash on Delivery (COD)",
        ["Nav.Notifications"] = "Notification Center",
        ["Nav.Reports"] = "Reports & Jobs",
        ["Nav.Settings"] = "Profile & Settings",
        ["ConfirmDeleteTitle"] = "Confirm Operation",
        ["SessionExpiredTitle"] = "Session Expired",
        ["SessionExpiredMessage"] = "Your session has expired or was revoked. Please sign in again.",
        ["ApiUnreachable"] = "Unable to connect to the API server. Please check the endpoint configuration or network."
    };
}
