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

        // --- THÊM VÀO GIỎ HÀNG (CÓ CHECK TỒN KHO & HIỆU LỰC) ---
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);

            // Lấy thông tin sản phẩm mới nhất từ DB
            var hangHoa = _context.HangHoas.SingleOrDefault(p => p.MaHh == id);
            if (hangHoa == null) return NotFound();

            // 1. Kiểm tra Hiệu lực (Nếu Admin đã ẩn/xóa mềm)
            if (hangHoa.HieuLuc == false)
            {
                TempData["Error"] = "Sản phẩm này đã ngừng kinh doanh!";
                // Quay lại trang trước đó
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // 2. Kiểm tra Tồn kho
            int currentQtyInCart = item != null ? item.SoLuong : 0;
            int requestedQty = currentQtyInCart + quantity;
            
            if (requestedQty > (hangHoa.SoLuong ?? 0))
            {
                TempData["Error"] = $"Kho chỉ còn {hangHoa.SoLuong} sản phẩm \"{hangHoa.TenHh}\"!";
                // Nếu đang ở trang chi tiết thì quay lại đó để hiện lỗi
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

        // --- MUA NGAY (THÊM & CHUYỂN ĐẾN CHECKOUT) ---
        [HttpPost]
        public IActionResult BuyNow(int id, int quantity = 1)
        {
            // Kiểm tra nhanh trước khi thêm
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

        // --- CẬP NHẬT GIỎ HÀNG (+/-) ---
        [HttpGet]
        public IActionResult UpdateCart(int id, int quantity)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);
            
            if (item != null)
            {
                // Kiểm tra tồn kho khi tăng số lượng
                if (quantity > item.SoLuong) // Nếu đang tăng
                {
                    var hangHoa = _context.HangHoas.Find(id);
                    if (quantity > (hangHoa?.SoLuong ?? 0))
                    {
                        TempData["Error"] = $"Kho chỉ còn {hangHoa?.SoLuong} sản phẩm!";
                        return RedirectToAction("Index");
                    }
                }

                if (quantity > 0)
                {
                    item.SoLuong = quantity;
                }
                else
                {
                    cart.Remove(item);
                }
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

        // --- CHECKOUT GET: KIỂM TRA XÁC THỰC NGẦM ---
        [HttpGet]
        public IActionResult Checkout()
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            // 1. Check đăng nhập
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null)
            {
                // Về trang chủ để bật Modal đăng nhập
                return RedirectToAction("Index", "Home");
            }

            var maKh = int.Parse(maKhStr);
            
            // 2. Lấy thông tin Khách hàng + Tài khoản
            var khachHang = _context.KhachHangs
                .Include(k => k.MaTkNavigation) 
                .FirstOrDefault(k => k.MaKh == maKh);
            
            var taiKhoan = khachHang?.MaTkNavigation;

            // 3. KIỂM TRA XÁC THỰC NGẦM
            // Nếu chưa xác thực Email hoặc SĐT -> ĐÁ VỀ PROFILE
            if (taiKhoan != null)
            {
                if (taiKhoan.EmailDaXacThuc != true || taiKhoan.SDTDaXacThuc != true)
                {
                    TempData["Warning"] = "Bạn cần xác thực Email và Số điện thoại trước khi đặt hàng để đảm bảo quyền lợi.";
                    return RedirectToAction("Profile", "KhachHang");
                }
            }

            // Nếu OK -> Hiển thị form (chỉ cần điền địa chỉ)
            ViewBag.NguoiNhan = khachHang?.HoTen ?? HttpContext.Session.GetString("HoTen");
            ViewBag.Email = khachHang?.Email ?? "";
            ViewBag.DienThoai = taiKhoan?.Sdt ?? "";
            
            // Truyền trạng thái xác thực xuống View (để hiển thị icon xanh)
            ViewBag.EmailDaXacThuc = taiKhoan?.EmailDaXacThuc ?? false;
            ViewBag.SDTDaXacThuc = taiKhoan?.SDTDaXacThuc ?? false;

            return View(Cart);
        }

        // --- CHECKOUT POST: LƯU ĐƠN HÀNG & TRỪ TỒN KHO ---
        [HttpPost]
        public IActionResult Checkout(string HoTen, string SoNha, string TinhThanh, string QuanHuyen, string PhuongXa, string GhiChu)
        {
            if (Cart.Count == 0) return RedirectToAction("Index");

            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");
            var maKh = int.Parse(maKhStr);

            // Lấy lại thông tin người dùng để đảm bảo an toàn
            var khachHang = _context.KhachHangs
                .Include(k => k.MaTkNavigation)
                .FirstOrDefault(k => k.MaKh == maKh);
            var taiKhoan = khachHang?.MaTkNavigation;

            // Kiểm tra lại lần cuối
            if (taiKhoan == null || taiKhoan.EmailDaXacThuc != true || taiKhoan.SDTDaXacThuc != true)
            {
                TempData["Warning"] = "Tài khoản chưa được xác thực đầy đủ.";
                return RedirectToAction("Profile", "KhachHang");
            }

            // Gộp địa chỉ
            var diaChiDayDu = $"{SoNha}, {PhuongXa}, {QuanHuyen}, {TinhThanh}";

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tạo Hóa Đơn
                    var hoaDon = new HoaDon
                    {
                        MaKh = maKh,
                        NgayDat = DateTime.Now,
                        HoTenNguoiNhan = HoTen,
                        DiaChiNguoiNhan = diaChiDayDu,
                        SdtnguoiNhan = taiKhoan.Sdt, // Lấy SĐT chính chủ
                        GhiChu = GhiChu, 
                        MaTrangThai = 0, // Mới đặt
                        PhiVanChuyen = 30000 
                    };

                    _context.HoaDons.Add(hoaDon);
                    _context.SaveChanges();

                    // 2. Tạo Chi Tiết & Trừ Tồn Kho
                    foreach (var item in Cart)
                    {
                        var product = _context.HangHoas.Find(item.MaHh);
                        
                        // Kiểm tra lại tồn kho lần cuối (tránh race condition)
                        if (product == null || (product.SoLuong ?? 0) < item.SoLuong)
                        {
                            throw new Exception($"Sản phẩm {item.TenHh} không đủ số lượng trong kho.");
                        }

                        // Trừ tồn kho
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
                    transaction.Commit(); // Xác nhận giao dịch

                    // Xóa giỏ hàng
                    HttpContext.Session.Remove(CART_KEY);
                    
                    return View("Success");
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Hoàn tác nếu lỗi
                    TempData["Loi"] = "Lỗi đặt hàng: " + ex.Message;
                    return RedirectToAction("Index"); // Quay về giỏ hàng
                }
            }
        }
    }
}