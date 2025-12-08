using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Areas.Admin.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin")]
    [Route("admin/homeadmin")]
    [AdminOnly]
    public class HomeAdminController : Controller
    {
        private readonly TechStoreContext _db;

        public HomeAdminController(TechStoreContext context)
        {
            _db = context;
        }

        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index(string period = "week")
        {
            try
            {
                // --- 1. THỐNG KÊ CƠ BẢN (STATS CARDS) ---
                ViewBag.TongSanPham = await _db.HangHoas.CountAsync();
                ViewBag.TongKhachHang = await _db.KhachHangs.CountAsync();
                ViewBag.TongDonHang = await _db.HoaDons.CountAsync();
                ViewBag.DoanhThu = await _db.ChiTietHoaDons.SumAsync(ct => ct.DonGia * ct.SoLuong);

                // --- 2. BIỂU ĐỒ DOANH THU (LINE CHART) ---
                // Logic lọc theo thời gian (Demo: Mặc định 7 ngày qua)
                var today = DateTime.Today;
                var sevenDaysAgo = today.AddDays(-6);
                
                var revenueData = await _db.HoaDons
                    .Where(h => h.NgayDat >= sevenDaysAgo)
                    .SelectMany(h => h.ChiTietHoaDons)
                    .Select(ct => new { 
                        Date = ct.MaHdNavigation!.NgayDat, 
                        Total = ct.DonGia * ct.SoLuong 
                    })
                    .ToListAsync();

                var revenueGrouped = revenueData
                    .GroupBy(x => x.Date.HasValue ? x.Date.Value.Date : DateTime.MinValue)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.Total) })
                    .ToList();

                var labels = new List<string>();
                var dataRevenue = new List<double>();

                for (int i = 0; i < 7; i++)
                {
                    var date = sevenDaysAgo.AddDays(i);
                    labels.Add(date.ToString("dd/MM"));
                    var record = revenueGrouped.FirstOrDefault(r => r.Date == date);
                    dataRevenue.Add(record != null ? record.Revenue : 0);
                }

                ViewBag.RevenueLabels = System.Text.Json.JsonSerializer.Serialize(labels);
                ViewBag.RevenueData = System.Text.Json.JsonSerializer.Serialize(dataRevenue);

                // --- 3. BIỂU ĐỒ TRẠNG THÁI ĐƠN HÀNG (PIE CHART) ---
                var orderStats = await _db.HoaDons
                    .GroupBy(h => h.MaTrangThai)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                // Mapping trạng thái: 0: Mới, 1: Duyệt, 2: Giao, 3: Xong, -1: Hủy
                var pieData = new int[5]; // [Mới, Duyệt, Giao, Xong, Hủy]
                foreach(var item in orderStats)
                {
                    if (item.Status == 0) pieData[0] = item.Count;
                    else if (item.Status == 1) pieData[1] = item.Count;
                    else if (item.Status == 2) pieData[2] = item.Count;
                    else if (item.Status == 3) pieData[3] = item.Count;
                    else if (item.Status == -1) pieData[4] = item.Count;
                }
                ViewBag.PieData = System.Text.Json.JsonSerializer.Serialize(pieData);

                // --- 4. CÁC WIDGETS DỮ LIỆU ---
                
                // Top 5 Sản phẩm bán chạy
                ViewBag.TopProducts = await _db.ChiTietHoaDons
                    .GroupBy(ct => ct.MaHh)
                    .Select(g => new {
                        Name = g.First().MaHhNavigation != null ? g.First().MaHhNavigation!.TenHh : "Unknown",
                        Img = g.First().MaHhNavigation != null ? g.First().MaHhNavigation!.HinhAnh : "",
                        Sold = g.Sum(x => x.SoLuong),
                        Revenue = g.Sum(x => x.DonGia * x.SoLuong)
                    })
                    .OrderByDescending(x => x.Sold)
                    .Take(5)
                    .ToListAsync();

                // Đơn hàng mới cần xử lý (Trạng thái = 0)
                ViewBag.NewOrders = await _db.HoaDons
                    .Include(h => h.MaKhNavigation)
                    .Include(h => h.ChiTietHoaDons)
                    .Where(h => h.MaTrangThai == 0)
                    .OrderByDescending(h => h.NgayDat)
                    .Take(15)
                    .ToListAsync();

                // Khách hàng mới (5 người gần nhất)
                ViewBag.NewCustomers = await _db.KhachHangs
                    .Include(k => k.MaTkNavigation)
                    .OrderByDescending(k => k.MaKh)
                    .Take(5)
                    .ToListAsync();

                // Cảnh báo tồn kho (Dưới 10 sản phẩm)
                ViewBag.LowStock = await _db.HangHoas
                    .Where(p => p.SoLuong < 10 && p.HieuLuc == true)
                    .OrderBy(p => p.SoLuong)
                    .Take(5)
                    .ToListAsync();

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi tải Dashboard: " + ex.Message;
                return View();
            }
        }
    }
}