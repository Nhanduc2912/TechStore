using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using TechStore.ViewModels;
using TechStore.Areas.Admin.Attributes;
using System;
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

        public ThongKeAdminController(TechStoreContext context)
        {
            _db = context;
        }

        // --- 1. THÊM INDEX ĐỂ ĐIỀU HƯỚNG ---
        // Khi vào /admin/thongke -> Tự nhảy sang /admin/thongke/doanhthu
        [Route("")]
        [Route("index")]
        public IActionResult Index()
        {
            return RedirectToAction("DoanhThu");
        }

        // --- 2. DOANH THU ---
        [Route("doanhthu")]
        [HttpGet]
        public async Task<IActionResult> DoanhThu(DateTime? from, DateTime? to)
        {
            // Mặc định: Tháng hiện tại
            var now = DateTime.Now;
            from ??= new DateTime(now.Year, now.Month, 1);
            to ??= now;

            // Lấy đến cuối ngày của 'to'
            var toDate = to.Value.Date.AddDays(1).AddTicks(-1);

            var orders = await _db.HoaDons
                .Where(h => h.NgayDat >= from && h.NgayDat <= toDate && h.MaTrangThai == 3)
                .Include(h => h.ChiTietHoaDons)
                .OrderByDescending(h => h.NgayDat)
                .ToListAsync();

            var doanhThu = orders.SelectMany(h => h.ChiTietHoaDons).Sum(ct => ct.DonGia * ct.SoLuong);

            ViewBag.FromDate = from;
            ViewBag.ToDate = to;
            ViewBag.DoanhThu = doanhThu;
            ViewBag.TongDonHang = orders.Count;

            // Dữ liệu biểu đồ
            var chartData = orders
                .GroupBy(h => h.NgayDat!.Value.Date)
                .Select(g => new {
                    Date = g.Key.ToString("dd/MM"),
                    Revenue = g.SelectMany(h => h.ChiTietHoaDons).Sum(ct => ct.DonGia * ct.SoLuong)
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(chartData.Select(x => x.Date));
            ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(chartData.Select(x => x.Revenue));

            return View(orders);
        }

        // --- 3. SẢN PHẨM (Đã fix lỗi Null & Kiểu dữ liệu) ---
        [Route("sanpham")]
        [HttpGet]
        public async Task<IActionResult> SanPham()
        {
            var topProducts = await _db.ChiTietHoaDons
                .Include(ct => ct.MaHhNavigation)
                .Where(ct => ct.MaHh.HasValue) // 1. Chỉ lấy dòng có Mã hàng hóa (loại bỏ null trước)
                .GroupBy(ct => ct.MaHh)
                .Select(g => new BaoCaoSanPhamVM 
                {
                    // 2. Fix lỗi CS0266 (int? -> int): Dùng .GetValueOrDefault()
                    MaHh = g.Key.GetValueOrDefault(), 
                    
                    // 3. Fix lỗi CS8602 (Null Reference): Kiểm tra kỹ navigation
                    TenHh = g.FirstOrDefault().MaHhNavigation != null 
                            ? g.FirstOrDefault().MaHhNavigation.TenHh 
                            : "Sản phẩm đã xóa",
                    
                    SoLuong = g.Sum(ct => ct.SoLuong),
                    DoanhThu = g.Sum(ct => ct.DonGia * ct.SoLuong),
                    LanMua = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(20)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho biểu đồ
            var labels = topProducts.Select(x => x.TenHh).ToArray();
            var data = topProducts.Select(x => x.SoLuong).ToArray();

            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
            ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(data);

            return View(topProducts);
        }

        // --- 4. KHÁCH HÀNG (Đã fix) ---
        [Route("khachhang")]
        [HttpGet]
        public async Task<IActionResult> KhachHang()
        {
            var customers = await _db.KhachHangs
                .Include(k => k.HoaDons).ThenInclude(h => h.ChiTietHoaDons)
                .Select(k => new BaoCaoKhachHangVM
                {
                    MaKh = k.MaKh,
                    HoTen = k.HoTen ?? "Khách vãng lai", // Xử lý null
                    TongDonHang = k.HoaDons.Count,
                    // Chỉ tính tiền đơn thành công (Trạng thái 3)
                    TongTienMua = k.HoaDons.Where(h => h.MaTrangThai == 3)
                                           .SelectMany(h => h.ChiTietHoaDons)
                                           .Sum(ct => ct.DonGia * ct.SoLuong),
                    LanMuaGanDay = k.HoaDons.Count(h => h.NgayDat >= DateTime.Now.AddMonths(-1))
                })
                .OrderByDescending(x => x.TongTienMua)
                .Take(50)
                .ToListAsync();

            return View(customers);
        }

        // --- 5. TỒN KHO (Đã fix) ---
        [Route("tonkho")]
        [HttpGet]
        public async Task<IActionResult> TonKho()
        {
            var products = await _db.HangHoas
                .Include(p => p.MaDmNavigation)
                .Select(p => new BaoCaoTonKhoVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    // Xử lý null Navigation
                    DanhMuc = p.MaDmNavigation != null ? p.MaDmNavigation.TenDm : "Chưa phân loại",
                    
                    // Xử lý null số liệu
                    SoLuong = p.SoLuong ?? 0,
                    GiaNhap = p.GiaNhap ?? 0,
                    GiaBan = p.DonGia,
                    GiaTriTonKho = (p.SoLuong ?? 0) * (p.GiaNhap ?? 0)
                })
                .OrderBy(x => x.SoLuong)
                .ToListAsync();

            ViewBag.TongGiaTriTonKho = products.Sum(x => x.GiaTriTonKho);
            ViewBag.SoMatHang = products.Count;
            ViewBag.SoMatHangHeo = products.Count(x => x.SoLuong < 10);

            return View(products);
        }
    }
}