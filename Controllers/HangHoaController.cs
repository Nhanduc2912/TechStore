using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using System; // Cần thiết cho Math.Ceiling

namespace TechStore.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly TechStoreContext _context;

        public HangHoaController(TechStoreContext context)
        {
            _context = context;
        }

        // --- ACTION INDEX (ĐÃ CẬP NHẬT PHÂN TRANG) ---
        // Thêm tham số 'page' có giá trị mặc định là 1
        public IActionResult Index(string? query, int? loai, double? min, double? max, int? sort, int page = 1)
        {
            // Số lượng sản phẩm trên mỗi trang
            int pageSize = 9; 

            var hangHoas = _context.HangHoas.AsQueryable();

            // 1. Chỉ lấy sản phẩm đang có hiệu lực
            hangHoas = hangHoas.Where(p => p.HieuLuc == true || p.HieuLuc == null);

            // 2. Lọc theo Từ khóa
            if (!string.IsNullOrEmpty(query))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
            }

            // 3. Lọc theo Danh mục
            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaDm == loai.Value);
            }

            // 4. Lọc theo Khoảng giá
            if (min.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia >= min.Value);
            }
            if (max.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia <= max.Value);
            }

            // 5. Sắp xếp
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

            // --- XỬ LÝ PHÂN TRANG ---
            // Tính tổng số lượng kết quả (để tính số trang)
            int totalItems = hangHoas.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Kiểm tra trang hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Cắt dữ liệu (Skip & Take)
            var result = hangHoas
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Gửi dữ liệu bổ sung sang View để giữ trạng thái bộ lọc và phân trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            
            ViewBag.Keyword = query;
            ViewBag.Loai = loai;
            ViewBag.Min = min;
            ViewBag.Max = max;
            ViewBag.Sort = sort;

            return View(result);
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

            // Lấy sản phẩm tương tự
            var sanPhamTuongTu = _context.HangHoas
                .Where(p => p.MaDm == data.MaDm && p.MaHh != data.MaHh && (p.HieuLuc == true || p.HieuLuc == null))
                .Take(4)
                .ToList();

            ViewBag.SanPhamTuongTu = sanPhamTuongTu;

            return View(data);
        }
    }
}