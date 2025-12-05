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
            try
            {
                var order = await _db.HoaDons.FindAsync(id);
                if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                order.MaTrangThai = status;
                if (status == 3) order.NgayGiao = DateTime.Now; // Đã giao

                _db.Update(order);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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