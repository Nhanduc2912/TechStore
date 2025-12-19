using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using TechStore.Areas.Admin.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/donhang")]
    [AdminOnly]
    public class DonHangAdminController : Controller
    {
        private readonly TechStoreContext _db;

        public DonHangAdminController(TechStoreContext context)
        {
            _db = context;
        }

        [Route("")]
        [Route("index")]
        [HttpGet]
        public async Task<IActionResult> Index(int status = -99, int page = 1, string search = "")
        {
            // status = -99 là mã mặc định cho "Tất cả" (để tránh trùng với -1 là "Hủy")
            const int pageSize = 10;
            
            var query = _db.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHoaDons) // Include cái này để tính tổng tiền không bị 0
                .AsQueryable();

            // Lọc theo trạng thái (Chỉ lọc khi status khác -99)
            if (status != -99)
            {
                query = query.Where(h => h.MaTrangThai == status);
            }
            ViewBag.SelectedStatus = status;

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(h => h.MaHd.ToString().Contains(search) || 
                                         (h.HoTenNguoiNhan != null && h.HoTenNguoiNhan.Contains(search)) ||
                                         (h.MaKhNavigation != null && h.MaKhNavigation.HoTen.Contains(search)));
                ViewBag.Search = search;
            }

            var totalOrders = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            var orders = await query
                .OrderByDescending(h => h.NgayDat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Statuses = new SelectList(await _db.TrangThaiDonHangs.ToListAsync(), "MaTrangThai", "TenTrangThai", status);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalOrders = totalOrders;

            return View(orders);
        }

        [Route("detail/{id}")]
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefaultAsync(h => h.MaHd == id);

            if (order == null) return NotFound();

            ViewBag.TongTien = order.ChiTietHoaDons.Sum(ct => ct.DonGia * ct.SoLuong);
            
            // Danh sách trạng thái để sửa nhanh tại trang chi tiết
            ViewBag.Statuses = new SelectList(await _db.TrangThaiDonHangs.ToListAsync(), "MaTrangThai", "TenTrangThai", order.MaTrangThai);
            
            return View(order);
        }

        [Route("update-status")]
[HttpPost]
public async Task<IActionResult> UpdateStatus(int id, int status)
{
    using (var transaction = _db.Database.BeginTransaction())
    {
        try
        {
            // 1. Lấy đơn hàng kèm chi tiết và thông tin Hàng hóa
            var order = await _db.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .ThenInclude(ct => ct.MaHhNavigation) // Dùng đúng MaHhNavigation
                .FirstOrDefaultAsync(h => h.MaHd == id);

            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            // --- SỬA LỖI Ở DÒNG NÀY (Thêm ?? 0) ---
            int oldStatus = order.MaTrangThai ?? 0; // Nếu null thì gán bằng 0
            // --------------------------------------
            
            int newStatus = status;            

            // --- ĐỊNH NGHĨA NHÓM TRẠNG THÁI ---
            // Nhóm A (Chưa trừ kho): 0 (Mới), 1 (Đã xác nhận)
            bool isNotDeducted = (oldStatus == 0 || oldStatus == 1);

            // Nhóm B (Đã trừ kho): 2 (Đang giao), 3 (Đã giao/Hoàn thành)
            bool isDeducted = (oldStatus == 2 || oldStatus == 3);

            // Nhóm Đích đến
            bool targetDeduct = (newStatus == 2 || newStatus == 3); // Chuyển sang Giao/Xong
            bool targetRestock = (newStatus == -1 || newStatus == 0 || newStatus == 1); // Chuyển sang Hủy hoặc quay lại Mới

            // --- XỬ LÝ LOGIC KHO ---

            // TRƯỜNG HỢP 1: Từ (Mới/Duyệt) -> Chuyển sang (Giao/Xong) => TRỪ KHO
            if (isNotDeducted && targetDeduct)
            {
                foreach (var item in order.ChiTietHoaDons)
                {
                    var hangHoa = item.MaHhNavigation;
                    
                    // Kiểm tra tồn kho (Sử dụng ?? 0 cho SoLuong để tránh lỗi null)
                    if (hangHoa == null || (hangHoa.SoLuong ?? 0) < item.SoLuong)
                    {
                        return Json(new { success = false, message = $"Sản phẩm '{hangHoa?.TenHh}' không đủ hàng (Còn: {hangHoa?.SoLuong ?? 0})" });
                    }

                    // Trừ kho (Cần ép kiểu để tránh lỗi tương tự)
                    hangHoa.SoLuong = (hangHoa.SoLuong ?? 0) - item.SoLuong;
                    _db.Update(hangHoa);
                }
            }
            // TRƯỜNG HỢP 2: Đang (Giao/Xong) -> Chuyển sang (Hủy/Mới) => HOÀN KHO
            else if (isDeducted && targetRestock)
            {
                foreach (var item in order.ChiTietHoaDons)
                {
                    var hangHoa = item.MaHhNavigation;
                    if (hangHoa != null)
                    {
                        // Cộng lại kho
                        hangHoa.SoLuong = (hangHoa.SoLuong ?? 0) + item.SoLuong;
                        _db.Update(hangHoa);
                    }
                }
            }

            // --- CẬP NHẬT TRẠNG THÁI ---
            order.MaTrangThai = newStatus;
            
            // Nếu trạng thái là 3 (Đã giao) thì cập nhật ngày giao
            if (newStatus == 3) 
            {
                order.NgayGiao = DateTime.Now;
            }
            else if (newStatus == 0 || newStatus == 1 || newStatus == -1)
            {
                    // Nếu quay xe về trạng thái chưa giao hoặc hủy thì xóa ngày giao
                    order.NgayGiao = null; 
            }

            _db.Update(order);
            await _db.SaveChangesAsync();
            
            // Commit Transaction
            transaction.Commit();

            return Json(new { success = true, message = "Cập nhật trạng thái và kho hàng thành công!" });
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }
}

        [Route("invoice/{id}")]
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _db.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefaultAsync(h => h.MaHd == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [Route("export")]
        [HttpGet]
        public IActionResult Export()
        {
            TempData["Info"] = "Tính năng export đang phát triển!";
            return RedirectToAction("Index");
        }
    }
}