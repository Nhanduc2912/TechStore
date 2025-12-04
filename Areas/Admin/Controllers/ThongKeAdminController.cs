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
    [Route("admin/thongke")]
    [AdminOnly]
    public class ThongKeAdminController : Controller
    {
        private readonly TechStoreContext _db;
        private readonly AdminService _adminService;

        public ThongKeAdminController(TechStoreContext context, AdminService adminService)
        {
            _db = context;
            _adminService = adminService;
        }

        /// <summary>
        /// Báo cáo doanh thu
        /// </summary>
        [Route("doanhthu")]
        [HttpGet]
        public async Task<IActionResult> DoanhThu(DateTime? from, DateTime? to)
        {
            from ??= DateTime.Now.AddMonths(-1);
            to ??= DateTime.Now;

            var orders = await _db.HoaDons
                .Where(h => h.NgayDat >= from && h.NgayDat <= to)
                .Include(h => h.ChiTietHoaDons)
                .ToListAsync();

            var doanhThu = orders
                .SelectMany(h => h.ChiTietHoaDons)
                .Sum(ct => ct.DonGia * ct.SoLuong);

            ViewBag.FromDate = from;
            ViewBag.ToDate = to;
            ViewBag.DoanhThu = _adminService.FormatCurrency(Convert.ToDecimal(doanhThu));
            ViewBag.TongDonHang = orders.Count;

            // Dữ liệu biểu đồ theo ngày
            // Fix CS8629: Đảm bảo xử lý null an toàn khi GroupBy
            var chartData = orders
                .Where(h => h.NgayDat.HasValue)
                .GroupBy(h => h.NgayDat!.Value.Date) // Sử dụng toán tử null-forgiving (!) vì đã Where HasValue
                .Select(g => new
                {
                    Date = g.Key.ToString("dd/MM"),
                    Revenue = g.SelectMany(h => h.ChiTietHoaDons)
                        .Sum(ct => ct.DonGia * ct.SoLuong)
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(chartData.Select(x => x.Date));
            ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(chartData.Select(x => x.Revenue));

            return View(orders);
        }

        /// <summary>
        /// Báo cáo top sản phẩm bán chạy
        /// </summary>
        [Route("sanpham")]
        [HttpGet]
        public async Task<IActionResult> SanPham()
        {
            var topProducts = await _db.ChiTietHoaDons
                .GroupBy(ct => ct.MaHh)
                .Select(g => new
                {
                    MaHh = g.Key,
                    // FIX CS8602: Sử dụng toán tử ?. và ?? để xử lý null an toàn
                    TenHh = g.FirstOrDefault() != null 
                            ? (g.FirstOrDefault()!.MaHhNavigation != null 
                                ? (g.FirstOrDefault()!.MaHhNavigation!.TenHh ?? "") 
                                : "") 
                            : "",
                    SoLuong = g.Sum(ct => ct.SoLuong),
                    DoanhThu = g.Sum(ct => ct.DonGia * ct.SoLuong),
                    LanMua = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(20)
                .ToListAsync();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(topProducts.Select(x => x.TenHh));
            ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(topProducts.Select(x => x.SoLuong));

            return View(topProducts);
        }

        /// <summary>
        /// Báo cáo khách hàng
        /// </summary>
        [Route("khachhang")]
        [HttpGet]
        public async Task<IActionResult> KhachHang()
        {
            var customers = await _db.KhachHangs
                .Include(k => k.HoaDons)
                    .ThenInclude(h => h.ChiTietHoaDons)
                .Select(k => new
                {
                    MaKh = k.MaKh,
                    HoTen = k.HoTen,
                    TongDonHang = k.HoaDons.Count,
                    TongTienMua = k.HoaDons
                        .SelectMany(h => h.ChiTietHoaDons)
                        .Sum(ct => ct.DonGia * ct.SoLuong),
                    LanMuaGanDay = k.HoaDons
                        .Where(h => h.NgayDat.HasValue && h.NgayDat.Value.Date >= DateTime.Now.AddMonths(-1).Date)
                        .Count()
                })
                .OrderByDescending(x => x.TongTienMua)
                .Take(20)
                .ToListAsync();

            return View(customers);
        }

        /// <summary>
        /// Báo cáo tồn kho
        /// </summary>
        [Route("tonkho")]
        [HttpGet]
        public async Task<IActionResult> TonKho()
        {
            var products = await _db.HangHoas
                .Include(p => p.MaDmNavigation)
                .Select(p => new
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    DanhMuc = p.MaDmNavigation != null ? p.MaDmNavigation.TenDm : "",
                    // FIX: Lấy dữ liệu thật từ DB (dùng ?? 0 để tránh null)
                    SoLuong = p.SoLuong ?? 0,
                    GiaNhap = p.GiaNhap ?? 0,
                    GiaBan = p.DonGia,
                    GiaTriTonKho = (p.SoLuong ?? 0) * (p.GiaNhap ?? 0)
                })
                .OrderBy(x => x.SoLuong)
                .ToListAsync();

            ViewBag.TongGiaTriTonKho = products.Sum(x => x.GiaTriTonKho);
            ViewBag.SoMatHang = products.Count;
            ViewBag.SoMatHangHeoDuoi10 = products.Count(x => x.SoLuong < 10);

            return View(products);
        }

        /// <summary>
        /// API lấy dữ liệu biểu đồ
        /// </summary>
        [Route("api/chart")]
        [HttpGet]
        public async Task<IActionResult> GetChartData(string type, int period = 7)
        {
            try
            {
                // FIX CS8600: Cho phép null
                object? data = null;
                switch (type.ToLower())
                {
                    case "revenue":
                        data = await _adminService.GetRevenueChartDataAsync(period);
                        break;
                    case "status":
                        data = await _adminService.GetOrderStatusChartDataAsync();
                        break;
                    case "products":
                        var topProducts = await _adminService.GetTopProductsAsync(5);
                        var labels = new List<string>();
                        var values = new List<int>();
                        foreach (var x in topProducts)
                        {
                            labels.Add(x.TenHh);
                            values.Add(x.SoLuong);
                        }
                        data = new { Labels = labels, Data = values };
                        break;
                }
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}