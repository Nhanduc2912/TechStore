using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Helpers;
using TechStore.ViewModels;
using TechStore.Models;
using System.Text.RegularExpressions;

namespace TechStore.Controllers
{
    public class GioHangController : Controller
    {
        private readonly TechStoreContext _context;
        const string CART_KEY = "MYCART";

        public GioHangController(TechStoreContext context)
        {
            _context = context;
        }

        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        public IActionResult Index()
        {
            return View(Cart);
        }

        // ... (Giữ nguyên các hàm AddToCart, BuyNow, UpdateCart, RemoveCart) ...
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            var hangHoa = _context.HangHoas.SingleOrDefault(p => p.MaHh == id);
            if (hangHoa == null) return NotFound();

            if (hangHoa.HieuLuc == false)
            {
                TempData["Error"] = "Sản phẩm này đã ngừng kinh doanh!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            int currentQtyInCart = item != null ? item.SoLuong : 0;
            int requestedQty = currentQtyInCart + quantity;
            
            if (requestedQty > (hangHoa.SoLuong ?? 0))
            {
                TempData["Error"] = $"Kho chỉ còn {hangHoa.SoLuong} sản phẩm \"{hangHoa.TenHh}\"!";
                return RedirectToAction("Detail", "HangHoa", new { id = id });
            }

            if (item == null)
            {
                item = new CartItem
                {
                    MaHh = hangHoa.MaHh,
                    TenHh = hangHoa.TenHh,
                    DonGia = hangHoa.DonGia,
                    HinhAnh = hangHoa.HinhAnh ?? "",
                    SoLuong = quantity
                };
                gioHang.Add(item);
            }
            else
            {
                item.SoLuong += quantity;
            }

            HttpContext.Session.Set(CART_KEY, gioHang);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult BuyNow(int id, int quantity = 1)
        {
            var hangHoa = _context.HangHoas.Find(id);
            if (hangHoa == null || hangHoa.HieuLuc == false) return NotFound();

            if (quantity > (hangHoa.SoLuong ?? 0))
            {
                 TempData["Error"] = $"Không đủ hàng! Chỉ còn {hangHoa.SoLuong} sản phẩm.";
                 return RedirectToAction("Detail", "HangHoa", new { id = id });
            }

            AddToCart(id, quantity);
            return RedirectToAction("Checkout");
        }

        [HttpGet]
        public IActionResult UpdateCart(int id, int quantity)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);
            
            if (item != null)
            {
                if (quantity > item.SoLuong)
                {
                    var hangHoa = _context.HangHoas.Find(id);
                    if (quantity > (hangHoa?.SoLuong ?? 0))
                    {
                        TempData["Error"] = $"Kho chỉ còn {hangHoa?.SoLuong} sản phẩm!";
                        return RedirectToAction("Index");
                    }
                }

                if (quantity > 0) item.SoLuong = quantity;
                else cart.Remove(item);
                HttpContext.Session.Set(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult RemoveCart(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                gioHang.Remove(item);
                HttpContext.Session.Set(CART_KEY, gioHang);
            }
            return RedirectToAction("Index");
        }
        // ... (Kết thúc phần giữ nguyên) ...


        // --- CHECKOUT GET: KIỂM TRA ĐỊA CHỈ TRONG PROFILE ---
        [HttpGet]
        public IActionResult Checkout()
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");
            var maKh = int.Parse(maKhStr);
            
            var khachHang = _context.KhachHangs
                .Include(k => k.MaTkNavigation) 
                .FirstOrDefault(k => k.MaKh == maKh);
            var taiKhoan = khachHang?.MaTkNavigation;

            // 1. Kiểm tra xác thực (Giữ nguyên)
            if (taiKhoan != null && (taiKhoan.EmailDaXacThuc != true || taiKhoan.SDTDaXacThuc != true))
            {
                TempData["Warning"] = "Bạn cần xác thực Email và Số điện thoại trước khi đặt hàng.";
                return RedirectToAction("Profile", "KhachHang");
            }

            // 2. KIỂM TRA ĐỊA CHỈ (LOGIC MỚI)
            // Nếu chưa có địa chỉ -> Đá về Profile để cập nhật
            if (string.IsNullOrEmpty(khachHang?.DiaChi))
            {
                TempData["Warning"] = "Vui lòng cập nhật địa chỉ giao hàng trong hồ sơ trước khi thanh toán!";
                return RedirectToAction("Profile", "KhachHang");
            }

            // Gửi thông tin xuống View để hiển thị (Read-only)
            ViewBag.NguoiNhan = khachHang.HoTen;
            ViewBag.Email = khachHang.Email;
            ViewBag.DienThoai = taiKhoan?.Sdt;
            ViewBag.DiaChi = khachHang.DiaChi; // Địa chỉ lấy từ DB

            return View(Cart);
        }

        // --- CHECKOUT POST: LẤY ĐỊA CHỈ TỪ DB ---
        [HttpPost]
        public IActionResult Checkout(string GhiChu)
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");
            var maKh = int.Parse(maKhStr);

            var khachHang = _context.KhachHangs
                .Include(k => k.MaTkNavigation)
                .FirstOrDefault(k => k.MaKh == maKh);
            var taiKhoan = khachHang?.MaTkNavigation;

            // Kiểm tra lại lần cuối
            if (khachHang == null || string.IsNullOrEmpty(khachHang.DiaChi))
            {
                TempData["Warning"] = "Thông tin địa chỉ không hợp lệ. Vui lòng cập nhật lại.";
                return RedirectToAction("Profile", "KhachHang");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var hoaDon = new HoaDon
                    {
                        MaKh = maKh,
                        NgayDat = DateTime.Now,
                        HoTenNguoiNhan = khachHang.HoTen,   // Lấy từ Profile
                        DiaChiNguoiNhan = khachHang.DiaChi, // Lấy từ Profile
                        SdtnguoiNhan = taiKhoan?.Sdt,       // Lấy từ Profile
                        GhiChu = GhiChu, 
                        MaTrangThai = 0,
                        PhiVanChuyen = 30000 
                    };

                    _context.HoaDons.Add(hoaDon);
                    _context.SaveChanges();

                    foreach (var item in Cart)
                    {
                        var product = _context.HangHoas.Find(item.MaHh);
                        if (product == null || (product.SoLuong ?? 0) < item.SoLuong)
                        {
                            throw new Exception($"Sản phẩm {item.TenHh} không đủ số lượng trong kho.");
                        }

                        product.SoLuong -= item.SoLuong;
                        
                        var chiTiet = new ChiTietHoaDon
                        {
                            MaHd = hoaDon.MaHd,
                            MaHh = item.MaHh,
                            DonGia = item.DonGia,
                            SoLuong = item.SoLuong,
                            GiamGia = 0
                        };
                        _context.ChiTietHoaDons.Add(chiTiet);
                    }
                    
                    _context.SaveChanges();
                    transaction.Commit();

                    HttpContext.Session.Remove(CART_KEY);
                    
                    return View("Success");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Loi"] = "Lỗi đặt hàng: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }
    }
}