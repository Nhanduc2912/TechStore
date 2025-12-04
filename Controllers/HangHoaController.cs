using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly TechStoreContext _context;

        public HangHoaController(TechStoreContext context)
        {
            _context = context;
        }

        // ACTION DUY NHẤT XỬ LÝ MỌI BỘ LỌC
        public IActionResult Index(string? query, int? loai, double? min, double? max, int? sort)
        {
            var hangHoas = _context.HangHoas.AsQueryable();

            // 1. QUAN TRỌNG: Chỉ lấy sản phẩm đang có hiệu lực (Chưa bị xóa mềm)
            // Xử lý cả trường hợp HieuLuc là null (coi như là hiện)
            hangHoas = hangHoas.Where(p => p.HieuLuc == true || p.HieuLuc == null);

            // 2. Lọc theo Từ khóa (Search)
            if (!string.IsNullOrEmpty(query))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
                ViewBag.Keyword = query;
            }

            // 3. Lọc theo Danh mục (Category)
            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaDm == loai.Value);
                ViewBag.Loai = loai.Value;
            }

            // 4. Lọc theo Khoảng giá (Price Range)
            if (min.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia >= min.Value);
            }
            if (max.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia <= max.Value);
            }
            
            ViewBag.Min = min; 
            ViewBag.Max = max;

            // 5. Sắp xếp (Sorting)
            switch (sort)
            {
                case 1: // Giá tăng dần
                    hangHoas = hangHoas.OrderBy(p => p.DonGia);
                    break;
                case 2: // Giá giảm dần
                    hangHoas = hangHoas.OrderByDescending(p => p.DonGia);
                    break;
                case 3: // Bán chạy / Xem nhiều nhất
                    hangHoas = hangHoas.OrderByDescending(p => p.SoLuotXem);
                    break;
                default: // Mặc định: Mới nhất
                    hangHoas = hangHoas.OrderByDescending(p => p.MaHh);
                    break;
            }
            ViewBag.Sort = sort;

            return View(hangHoas.ToList());
        }

        public IActionResult Search(string? query)
        {
            return RedirectToAction("Index", new { query = query });
        }

        public IActionResult Detail(int id)
        {
            var data = _context.HangHoas
                .Include(p => p.MaDmNavigation)
                .SingleOrDefault(p => p.MaHh == id);

            if (data == null || (data.HieuLuc.HasValue && data.HieuLuc == false))
            {
                return NotFound();
            }

            // Tăng lượt xem
            data.SoLuotXem = (data.SoLuotXem ?? 0) + 1;
            _context.SaveChanges();

            // Lấy sản phẩm tương tự (cũng phải check HieuLuc)
            var sanPhamTuongTu = _context.HangHoas
                .Where(p => p.MaDm == data.MaDm && p.MaHh != data.MaHh && (p.HieuLuc == true || p.HieuLuc == null))
                .Take(4)
                .ToList();

            ViewBag.SanPhamTuongTu = sanPhamTuongTu;

            return View(data);
        }
    }
}