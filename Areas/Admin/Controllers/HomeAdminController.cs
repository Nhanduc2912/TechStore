using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models; // Thêm namespace này để dùng Model nếu cần
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
        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Thống kê số lượng
                ViewBag.TongSanPham = await _db.HangHoas.CountAsync();
                ViewBag.TongKhachHang = await _db.KhachHangs.CountAsync();
                ViewBag.TongDonHang = await _db.HoaDons.CountAsync();
                
                // 2. Tính tổng doanh thu
                var doanhThu = await _db.ChiTietHoaDons.SumAsync(ct => ct.DonGia * ct.SoLuong);
                ViewBag.DoanhThu = doanhThu;

                // 3. Dữ liệu biểu đồ doanh thu
                var today = DateTime.Today;
                var sevenDaysAgo = today.AddDays(-6);
                
                var rawData = await _db.HoaDons
                    .Where(h => h.NgayDat >= sevenDaysAgo)
                    .SelectMany(h => h.ChiTietHoaDons)
                    .Select(ct => new { 
                        Date = ct.MaHdNavigation!.NgayDat, 
                        Total = ct.DonGia * ct.SoLuong 
                    })
                    .ToListAsync();

                var revenueGrouped = rawData
                    .GroupBy(x => x.Date.HasValue ? x.Date.Value.Date : DateTime.MinValue)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.Total) })
                    .ToList();

                var labels = new List<string>();
                var data = new List<double>();

                for (int i = 0; i < 7; i++)
                {
                    var date = sevenDaysAgo.AddDays(i);
                    labels.Add(date.ToString("dd/MM"));
                    var record = revenueGrouped.FirstOrDefault(r => r.Date == date);
                    data.Add(record != null ? record.Revenue : 0);
                }

                ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
                ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(data);

                // 4. Top 5 sản phẩm bán chạy (SỬA LỖI TẠI ĐÂY)
                var topProducts = await _db.ChiTietHoaDons
                    .GroupBy(ct => ct.MaHh)
                    .Select(g => new {
                        // Đổi Name -> TenHh, Sold -> SoLuong cho đồng bộ
                        TenHh = g.First().MaHhNavigation != null ? g.First().MaHhNavigation!.TenHh : "Unknown",
                        SoLuong = g.Sum(x => x.SoLuong)
                    })
                    .OrderByDescending(x => x.SoLuong) // Sắp xếp theo SoLuong
                    .Take(5)
                    .ToListAsync();
                
                ViewBag.TopProducts = topProducts;

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