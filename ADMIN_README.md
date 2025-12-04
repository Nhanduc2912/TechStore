# 🎯 ADMIN PANEL CHUYÊN NGHIỆP - TECHSTORE

## 📋 Tổng quan

Admin Panel của TechStore đã được nâng cấp hoàn toàn với:

- ✅ Bảo mật cơ bản với Authorization attribute
- ✅ Audit Logging - ghi lại mọi hoạt động
- ✅ Dashboard với biểu đồ động (Chart.js)
- ✅ Quản lý Sản phẩm (CRUD + Bulk operations)
- ✅ Quản lý Đơn hàng (Chi tiết + Cập nhật trạng thái)
- ✅ Quản lý Khách hàng (Khóa/Mở khóa tài khoản)
- ✅ Quản lý Danh mục
- ✅ Báo cáo & Thống kê
- ✅ Giao diện hiện đại, responsive

---

## 🚀 HƯỚNG DẪN SỬ DỤNG

### 1. TRƯỚC TIÊN - DATABASE MIGRATIONS

Admin Panel cần 2 bảng mới: `AdminLog` và `SystemSettings`

```bash
# Tạo migration
Add-Migration AddAdminLogAndSystemSettings

# Update database
Update-Database
```

### 2. CẤU TRÚC THƯ MỤC

```
Areas/Admin/
├── Controllers/
│   ├── HomeAdminController.cs          ← Dashboard
│   ├── SanPhamAdminController.cs        ← Quản lý sản phẩm
│   ├── DonHangAdminController.cs        ← Quản lý đơn hàng
│   ├── KhachHangAdminController.cs      ← Quản lý khách hàng
│   ├── DanhMucAdminController.cs        ← Quản lý danh mục
│   └── ThongKeAdminController.cs        ← Báo cáo
│
├── Attributes/
│   └── AdminOnlyAttribute.cs            ← Custom attribute kiểm tra Admin
│
├── Services/
│   └── AdminService.cs                  ← Business logic
│
└── Views/
    ├── Shared/_LayoutAdmin.cshtml       ← Layout chính
    ├── HomeAdmin/Index.cshtml           ← Dashboard
    ├── SanPham/Index.cshtml, Create.cshtml
    ├── DonHang/Index.cshtml
    ├── KhachHang/Index.cshtml
    └── DanhMuc/Index.cshtml
```

### 3. ROUTES

Tất cả routes được thiết lập với pattern `/admin/[controller]/[action]`

```
/Admin/HomeAdmin                  → Dashboard
/Admin/SanPham                     → Danh sách sản phẩm
/Admin/SanPham/Create             → Thêm sản phẩm
/Admin/SanPham/Edit/{id}          → Sửa sản phẩm
/Admin/DonHang                     → Danh sách đơn hàng
/Admin/DonHang/Detail/{id}        → Chi tiết đơn hàng
/Admin/KhachHang                   → Danh sách khách hàng
/Admin/DanhMuc                     → Quản lý danh mục
/Admin/ThongKe/DoanhThu            → Báo cáo doanh thu
```

---

## 🔐 BẢOMẬT

### AdminOnly Attribute

```csharp
[AdminOnly]  // Chỉ Admin mới truy cập được
public class SanPhamAdminController : Controller
{
    // ...
}
```

Kiểm tra:

1. Session "MaKh" có tồn tại không
2. Session "VaiTro" == "Admin"

### Audit Logging

Mọi hành động (Thêm, Sửa, Xóa) đều được ghi log:

```csharp
await _adminService.LogActivityAsync(
    User.Identity.Name,
    "Thêm sản phẩm",
    "SanPham",
    "Chi tiết hoạt động",
    HttpContext.Connection.RemoteIpAddress?.ToString(),
    Request.Headers["User-Agent"].ToString()
);
```

---

## 📊 FEATURES CHI TIẾT

### Dashboard

- 4 stat cards (Doanh thu, Đơn, Sản phẩm, Khách)
- Biểu đồ doanh thu 7 ngày (Chart.js)
- Biểu đồ trạng thái đơn hàng (Doughnut chart)
- Top 5 sản phẩm bán chạy
- Timeline hoạt động gần đây

### Quản lý Sản phẩm

- ✅ CRUD sản phẩm
- ✅ Upload hình ảnh
- ✅ Phân trang, tìm kiếm, lọc theo danh mục
- ✅ Xóa nhiều cùng lúc (Bulk delete)
- ⏳ Import/Export Excel (Chuẩn bị)

### Quản lý Đơn hàng

- ✅ Danh sách với lọc theo trạng thái
- ✅ Chi tiết đơn hàng
- ✅ Cập nhật trạng thái
- ✅ Hủy đơn
- ⏳ In hóa đơn PDF (Chuẩn bị)

### Quản lý Khách hàng

- ✅ Danh sách khách hàng
- ✅ Chi tiết (lịch sử mua hàng, tổng chi tiêu)
- ✅ Ghi chú về khách hàng
- ✅ Khóa/Mở khóa tài khoản

### Quản lý Danh mục

- ✅ CRUD danh mục
- ✅ Kiểm tra sản phẩm trước khi xóa

### Báo cáo

- ✅ Doanh thu theo ngày/tháng
- ✅ Top sản phẩm bán chạy
- ✅ Báo cáo khách hàng
- ✅ Báo cáo tồn kho

---

## 📦 MODELS THÊM

### AdminLog

```csharp
public class AdminLog
{
    public int MaLog { get; set; }
    public string TenAdmin { get; set; }
    public string HanhDong { get; set; }      // Create, Update, Delete, etc.
    public string Module { get; set; }        // SanPham, DonHang, etc.
    public string ChiTiet { get; set; }
    public string IPAddress { get; set; }
    public DateTime ThoiGian { get; set; }
    public int? TrangtaiHanhDong { get; set; } // 1: Success, 0: Failed
}
```

### SystemSettings

```csharp
public class SystemSettings
{
    public int Id { get; set; }
    public string SettingKey { get; set; }
    public string SettingValue { get; set; }
    public DateTime NgayCapNhat { get; set; }
}
```

---

## 🛠️ SERVICES

### AdminService

```csharp
// Lấy thống kê dashboard
var stats = await adminService.GetDashboardStatsAsync(from, to);

// Lấy dữ liệu biểu đồ
var chartData = await adminService.GetRevenueChartDataAsync(7);

// Lấy top sản phẩm
var topProducts = await adminService.GetTopProductsAsync(5);

// Ghi log hoạt động
await adminService.LogActivityAsync(tenAdmin, hanhDong, module, chiTiet, ip, userAgent);

// Lấy lịch sử hoạt động
var logs = await adminService.GetActivityLogsAsync(page, pageSize);

// Lấy cài đặt
var value = await adminService.GetSettingAsync("key");

// Helper format
adminService.FormatCurrency(amount)      // "1,000,000₫"
adminService.FormatDateTime(date)        // "01/01/2025 10:30"
adminService.GetStatusBadgeClass(status) // "badge bg-success"
```

---

## ✨ STYLING

### Color Theme

```css
--primary-color: #0d6efd
--danger-color: #e30019
--success-color: #198754
--warning-color: #ffc107
```

### Components

- Responsive sidebar (Fixed)
- Sticky topbar
- Bootstrap 5 tables
- Custom badge colors
- Smooth transitions

---

## 📱 RESPONSIVE

- ✅ Desktop: Đầy đủ layout
- ✅ Tablet: Sidebar collapse
- ✅ Mobile: Hamburger menu (todo)

---

## 🚧 FEATURES COMING SOON

1. **Excel Import/Export**

   - Sử dụng ClosedXML hoặc EPPlus
   - Import sản phẩm từ Excel
   - Export báo cáo

2. **PDF Generation**

   - In hóa đơn
   - In báo cáo
   - Sử dụng QuestPDF hoặc SelectPdf

3. **Email Notifications**

   - Thông báo cập nhật đơn hàng
   - Gửi báo cáo định kỳ

4. **Advanced Analytics**

   - Biểu đồ chi tiết hơn
   - Dự báo doanh thu
   - Phân tích hành vi khách

5. **API Dashboard**

   - Real-time data
   - Mobile app dashboard

6. **Dark Mode**

   - Toggle dark/light theme
   - Save preference

7. **Multi-Language**
   - i18n support
   - EN/VI languages

---

## 🎓 CÁCH EXTEND

### Thêm Controller Admin Mới

```csharp
[Area("Admin")]
[Route("admin/tenmodule")]
[AdminOnly]
public class TenModuleAdminController : Controller
{
    private readonly TechStoreContext _db;
    private readonly AdminService _adminService;

    public TenModuleAdminController(TechStoreContext context, AdminService adminService)
    {
        _db = context;
        _adminService = adminService;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        // ... logic
        return View();
    }
}
```

### Thêm View

1. Tạo folder: `Areas/Admin/Views/TenModule/`
2. Tạo cshtml files: `Index.cshtml`, `Create.cshtml`, `Edit.cshtml`
3. Layout: `Layout = "~/Areas/Admin/Views/Shared/_LayoutAdmin.cshtml";`

### Ghi Log

```csharp
await _adminService.LogActivityAsync(
    User.Identity.Name ?? "Admin",
    "Hành động",
    "Module",
    "Chi tiết",
    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
    Request.Headers["User-Agent"].ToString()
);
```

---

## 📞 HỖTRỢ

Nếu gặp lỗi:

1. Kiểm tra AdminLog trong database
2. Xem console error message
3. Kiểm tra session "VaiTro" == "Admin"

---

## 📝 CHANGELOG

### v1.0 (Current)

- Dashboard nâng cao với Chart.js
- CRUD cho Sản phẩm, Đơn hàng, Khách hàng
- Audit logging
- AdminOnly attribute
- Professional UI/UX

---

**Phát triển bởi:** TechStore Dev Team  
**Ngày:** 03/12/2025  
**Phiên bản:** 1.0
