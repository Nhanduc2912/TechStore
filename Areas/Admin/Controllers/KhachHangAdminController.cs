using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using TechStore.Areas.Admin.Attributes;
using TechStore.Areas.Admin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/khachhang")]
    [AdminOnly]
    public class KhachHangAdminController : Controller
    {
        private readonly TechStoreContext _db;
        private readonly AdminService _adminService;

        public KhachHangAdminController(TechStoreContext context, AdminService adminService)
        {
            _db = context;
            _adminService = adminService;
        }

        /// <summary>
        /// Danh sách khách hàng
        /// </summary>
        [Route("")]
        [Route("index")]
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string search = "")
        {
            const int pageSize = 10;

            var query = _db.KhachHangs
                .Include(k => k.MaTkNavigation)
                .Include(k => k.HoaDons) // Include để đếm đơn hàng
                .AsQueryable();

            // Tìm kiếm: Tìm theo Tên hoặc Email (Trong bảng KhachHang hoặc TaiKhoan)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(k => 
                    k.HoTen.Contains(search) || 
                    (k.Email != null && k.Email.Contains(search)) ||
                    (k.MaTkNavigation != null && k.MaTkNavigation.Email.Contains(search))
                );
                ViewBag.Search = search;
            }

            var totalCustomers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCustomers / pageSize);

            var customers = await query
                .OrderByDescending(k => k.MaKh)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCustomers = totalCustomers;

            return View(customers);
        }

        /// <summary>
        /// Chi tiết khách hàng và lịch sử mua hàng
        /// </summary>
        [Route("detail/{id}")]
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var customer = await _db.KhachHangs
                .Include(k => k.MaTkNavigation)
                .Include(k => k.HoaDons)
                    .ThenInclude(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(k => k.MaKh == id);

            if (customer == null)
                return NotFound();

            // Tính thống kê
            ViewBag.TongDonHang = customer.HoaDons.Count;
            ViewBag.TongTienMua = customer.HoaDons
                .SelectMany(h => h.ChiTietHoaDons)
                .Sum(ct => ct.DonGia * ct.SoLuong);

            return View(customer);
        }

        /// <summary>
        /// Form sửa ghi chú khách hàng (Demo - vì DB chưa có cột GhiChu)
        /// </summary>
        [Route("edit-note/{id}")]
        [HttpGet]
        public async Task<IActionResult> EditNote(int id)
        {
            var customer = await _db.KhachHangs.FindAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        /// <summary>
        /// Lưu ghi chú
        /// </summary>
        [Route("save-note/{id}")]
        [HttpPost]
        public async Task<IActionResult> SaveNote(int id, string note)
        {
            try
            {
                var customer = await _db.KhachHangs.FindAsync(id);
                if (customer == null)
                    return NotFound();

                // Lưu ý: Vì bảng KhachHang chưa có cột GhiChu nên tạm thời không lưu vào DB
                // Nếu bạn đã thêm cột GhiChu, hãy bỏ comment dòng dưới:
                // customer.GhiChu = note;
                // _db.Update(customer);
                // await _db.SaveChangesAsync();

                /*
                // Ghi log nếu cần
                await _adminService.LogActivityAsync(
                    (User?.Identity?.Name ?? "Admin"),
                    "Ghi chú khách hàng",
                    "KhachHang",
                    $"Ghi chú khách #{id}: {customer.HoTen}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Request.Headers["User-Agent"].ToString()
                );
                */

                TempData["Success"] = "Lưu ghi chú thành công! (Ghi chú tạm thời chưa lưu vào DB)";
                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        /// <summary>
        /// Khóa tài khoản khách hàng
        /// </summary>
        [Route("lock/{id}")]
        [HttpPost]
        public async Task<IActionResult> Lock(int id)
        {
            try
            {
                var customer = await _db.KhachHangs
                    .Include(k => k.MaTkNavigation)
                    .FirstOrDefaultAsync(k => k.MaKh == id);

                if (customer == null)
                    return Json(new { success = false, message = "Khách hàng không tồn tại!" });

                // Kiểm tra nếu khách hàng không có tài khoản (Khách vãng lai)
                if (customer.MaTkNavigation == null)
                {
                    return Json(new { success = false, message = "Khách hàng vãng lai không có tài khoản để khóa!" });
                }

                // Khóa tài khoản
                customer.MaTkNavigation.TrangThai = false; // 0 = Locked
                _db.Update(customer.MaTkNavigation);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Khóa tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Mở khóa tài khoản khách hàng
        /// </summary>
        [Route("unlock/{id}")]
        [HttpPost]
        public async Task<IActionResult> Unlock(int id)
        {
            try
            {
                var customer = await _db.KhachHangs
                    .Include(k => k.MaTkNavigation)
                    .FirstOrDefaultAsync(k => k.MaKh == id);

                if (customer == null)
                    return Json(new { success = false, message = "Khách hàng không tồn tại!" });

                if (customer.MaTkNavigation == null)
                {
                    return Json(new { success = false, message = "Khách hàng không có tài khoản!" });
                }

                // Mở khóa tài khoản
                customer.MaTkNavigation.TrangThai = true; // 1 = Active
                _db.Update(customer.MaTkNavigation);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Mở khóa tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Xuất danh sách khách hàng
        /// </summary>
        [Route("export")]
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            // Chức năng Export Excel (Demo)
            TempData["Info"] = "Tính năng xuất Excel đang được phát triển!";
            return RedirectToAction("Index");
        }
    }
}