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

        // --- THÊM VÀO GIỎ HÀNG ---
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

        // --- MUA NGAY ---
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

            // Thêm vào giỏ trước
            AddToCart(id, quantity);
            
            // Chuyển đến trang thanh toán
            return RedirectToAction("Checkout");
        }

        // --- CẬP NHẬT GIỎ HÀNG ---
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

        // --- CHECKOUT GET: ĐIỀU HƯỚNG THÔNG MINH KHI CHƯA LOGIN ---
        [HttpGet]
        public IActionResult Checkout()
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            // 1. Check đăng nhập
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null)
            {
                // SỬA LỖI: Gán thông báo lỗi để Modal tự bật lên
                TempData["Loi"] = "Bạn cần đăng nhập để tiến hành thanh toán!";
                
                // Lấy trang trước đó (Referer)
                var referer = Request.Headers["Referer"].ToString();

                // Nếu có trang trước và không phải là chính trang Checkout (tránh lặp vô tận)
                if (!string.IsNullOrEmpty(referer) && !referer.Contains("Checkout"))
                {
                    // Quay lại trang cũ (VD: Đang ở trang Detail -> Quay lại Detail + Bật Popup Login)
                    return Redirect(referer);
                }

                // Fallback: Nếu không xác định được trang trước, về trang Giỏ hàng
                return RedirectToAction("Index"); 
            }

            var maKh = int.Parse(maKhStr);
            var khachHang = _context.KhachHangs
                .Include(k => k.MaTkNavigation) 
                .FirstOrDefault(k => k.MaKh == maKh);
            var taiKhoan = khachHang?.MaTkNavigation;

            // 3. Check xác thực (Đá về Profile nếu chưa xác thực)
            if (taiKhoan != null)
            {
                if (taiKhoan.EmailDaXacThuc != true || taiKhoan.SDTDaXacThuc != true)
                {
                    TempData["Warning"] = "Bạn cần xác thực Email và Số điện thoại trước khi đặt hàng.";
                    return RedirectToAction("Profile", "KhachHang");
                }
            }

            ViewBag.NguoiNhan = khachHang?.HoTen ?? HttpContext.Session.GetString("HoTen");
            ViewBag.Email = khachHang?.Email ?? "";
            ViewBag.DienThoai = taiKhoan?.Sdt ?? "";
            ViewBag.EmailDaXacThuc = taiKhoan?.EmailDaXacThuc ?? false;
            ViewBag.SDTDaXacThuc = taiKhoan?.SDTDaXacThuc ?? false;

            return View(Cart);
        }

        // --- CHECKOUT POST ---
        [HttpPost]
        public IActionResult Checkout(string HoTen, string SoNha, string TinhThanh, string QuanHuyen, string PhuongXa, string GhiChu)
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) 
            {
                TempData["Loi"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại!";
                return RedirectToAction("Index");
            }
            var maKh = int.Parse(maKhStr);

            var khachHang = _context.KhachHangs
                .Include(k => k.MaTkNavigation)
                .FirstOrDefault(k => k.MaKh == maKh);
            var taiKhoan = khachHang?.MaTkNavigation;

            if (taiKhoan == null || taiKhoan.EmailDaXacThuc != true || taiKhoan.SDTDaXacThuc != true)
            {
                TempData["Warning"] = "Tài khoản chưa được xác thực đầy đủ.";
                return RedirectToAction("Profile", "KhachHang");
            }

            var diaChiDayDu = $"{SoNha}, {PhuongXa}, {QuanHuyen}, {TinhThanh}";

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var hoaDon = new HoaDon
                    {
                        MaKh = maKh,
                        NgayDat = DateTime.Now,
                        HoTenNguoiNhan = HoTen,
                        DiaChiNguoiNhan = diaChiDayDu,
                        SdtnguoiNhan = taiKhoan.Sdt, 
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
                        
                        // Đã sửa lỗi sai tên class ChiTietHoaDons -> ChiTietHoaDon
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