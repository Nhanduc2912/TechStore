using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TechStore.Data;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TechStoreContext _context;

        public HomeController(ILogger<HomeController> logger, TechStoreContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy danh sách hàng hóa hiển thị trang chủ
            var hangHoa = _context.HangHoas
                                .AsNoTracking()
                                .Where(p => p.HieuLuc == true || p.HieuLuc == null)
                                // 1. Ưu tiên hàng còn tồn kho (SoLuong > 0) lên trước
                                .OrderByDescending(p => p.SoLuong > 0)
                                // 2. Sau đó mới sắp xếp theo mới nhất
                                .ThenByDescending(p => p.MaHh)
                                .Take(12)
                                .ToList();

            return View(hangHoa);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}