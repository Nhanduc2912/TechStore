using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace TechStore.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly TechStoreContext _context;

        public KhachHangController(TechStoreContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        // --- XỬ LÝ ĐĂNG KÝ (CÓ LƯU LẠI DỮ LIỆU KHI LỖI) ---
        [HttpPost]
        public IActionResult DangKy(string TenDangNhap, string MatKhau, string Email, string SDT, string HoTen)
        {
            try
            {
                // 1. LƯU TẠM DỮ LIỆU (Để fill lại form nếu lỗi)
                TempData["Reg_HoTen"] = HoTen;
                TempData["Reg_TenDangNhap"] = TenDangNhap;
                TempData["Reg_Email"] = Email;
                TempData["Reg_SDT"] = SDT;

                // 2. VALIDATION (Kiểm tra dữ liệu đầu vào)
                if (string.IsNullOrEmpty(TenDangNhap) || TenDangNhap.Length < 3 || !Regex.IsMatch(TenDangNhap, "^[a-zA-Z0-9_]+$"))
                {
                    TempData["RegisterError"] = "Tên đăng nhập phải từ 3 ký tự trở lên và không chứa ký tự đặc biệt!";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(MatKhau) || MatKhau.Length < 6)
                {
                    TempData["RegisterError"] = "Mật khẩu quá ngắn! Vui lòng nhập ít nhất 6 ký tự.";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(Email) || !Regex.IsMatch(Email, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"))
                {
                    TempData["RegisterError"] = "Email không hợp lệ!";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(SDT) || !Regex.IsMatch(SDT, @"^0\d{9}$"))
                {
                    TempData["RegisterError"] = "Số điện thoại không hợp lệ (phải gồm 10 số và bắt đầu bằng số 0)!";
                    return RedirectToAction("Index", "Home");
                }

                // 3. KIỂM TRA TRÙNG LẶP TRONG DB
                if (_context.TaiKhoans.Any(x => x.TenDangNhap == TenDangNhap))
                {
                    TempData["RegisterError"] = "Tên đăng nhập này đã có người sử dụng!";
                    return RedirectToAction("Index", "Home");
                }

                if (_context.TaiKhoans.Any(x => x.Email == Email))
                {
                    TempData["RegisterError"] = "Email này đã được đăng ký bởi tài khoản khác!";
                    return RedirectToAction("Index", "Home");
                }
                
                if (_context.TaiKhoans.Any(x => x.Sdt == SDT))
                {
                     TempData["RegisterError"] = "Số điện thoại này đã được sử dụng!";
                     return RedirectToAction("Index", "Home");
                }

                // 4. NẾU THÀNH CÔNG -> XÓA DỮ LIỆU TẠM
                TempData.Remove("Reg_HoTen");
                TempData.Remove("Reg_TenDangNhap");
                TempData.Remove("Reg_Email");
                TempData.Remove("Reg_SDT");

                // 5. TẠO TÀI KHOẢN MỚI
                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = TenDangNhap,
                    MatKhau = MatKhau, // Nên mã hóa MD5/SHA256 trong thực tế
                    Email = Email,
                    Sdt = SDT,
                    MaVaiTro = 3, // Khách hàng
                    TrangThai = true,
                    NgayTao = DateTime.Now,
                    EmailDaXacThuc = false,
                    SDTDaXacThuc = false
                };

                _context.TaiKhoans.Add(taiKhoan);
                _context.SaveChanges();

                var khachHang = new KhachHang
                {
                    HoTen = HoTen ?? TenDangNhap,
                    Email = Email,
                    MaTk = taiKhoan.MaTk,
                    DiemTichLuy = 0,
                    MaLoaiKh = 1
                };

                _context.KhachHangs.Add(khachHang);
                _context.SaveChanges();

                // Tự động đăng nhập
                HttpContext.Session.SetString("MaKh", khachHang.MaKh.ToString());
                HttpContext.Session.SetString("HoTen", khachHang.HoTen);
                HttpContext.Session.SetString("VaiTro", "KhachHang");

                TempData["Success"] = "Đăng ký thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["RegisterError"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public IActionResult DangNhap(string info, string matkhau)
        {
            var tk = _context.TaiKhoans.SingleOrDefault(p => 
                (p.TenDangNhap == info || p.Email == info) && p.MatKhau == matkhau && p.TrangThai == true);

            if (tk != null)
            {
                var kh = _context.KhachHangs.SingleOrDefault(p => p.MaTk == tk.MaTk);
                
                if (kh != null)
                {
                    HttpContext.Session.SetString("MaKh", kh.MaKh.ToString());
                    HttpContext.Session.SetString("HoTen", kh.HoTen);
                }
                else
                {
                    HttpContext.Session.SetString("MaKh", tk.TenDangNhap); // Dùng tạm tên đăng nhập làm ID nếu là Admin
                    HttpContext.Session.SetString("HoTen", "Quản trị viên");
                }

                if (tk.MaVaiTro == 1) 
                {
                    HttpContext.Session.SetString("VaiTro", "Admin");
                    return Redirect("/Admin/HomeAdmin");
                }
                
                return RedirectToAction("Index", "Home");
            }

            TempData["LoginError"] = "Tài khoản hoặc mật khẩu không đúng!";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult DonHang()
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home"); // Về trang chủ để bật modal

            if (int.TryParse(maKhStr, out int maKh))
            {
                var dsDonHang = _context.HoaDons
                    .Include(h => h.MaTrangThaiNavigation)
                    .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.MaHhNavigation)
                    .Where(h => h.MaKh == maKh)
                    .OrderByDescending(h => h.NgayDat)
                    .ToList();

                return View(dsDonHang);
            }
            
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");

            if (int.TryParse(maKhStr, out int maKh))
            {
                var khachHang = _context.KhachHangs
                    .Include(k => k.MaTkNavigation)
                    .FirstOrDefault(k => k.MaKh == maKh);
                
                // Nếu không tìm thấy khách hàng (VD bị xóa), quay về
                if (khachHang == null) return RedirectToAction("Index", "Home");

                return View(khachHang);
            }

            return RedirectToAction("Index", "Home");
        }
         // --- CẬP NHẬT THÔNG TIN CÁ NHÂN ---
        [HttpPost]
        public IActionResult UpdateProfile(string HoTen, string? NgaySinh, string? DiaChi, string? GioiTinh)
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");

            if (int.TryParse(maKhStr, out int maKh))
            {
                var khachHang = _context.KhachHangs.Find(maKh);
                if (khachHang != null)
                {
                    khachHang.HoTen = HoTen;
                    khachHang.DiaChi = DiaChi;
                    if (DateTime.TryParse(NgaySinh, out DateTime dob))
                    {
                        // Ép kiểu về DateOnly nếu model dùng DateOnly, hoặc DateTime tùy model
                        // Ở đây giả sử model dùng DateOnly (theo .NET 6+)
                        khachHang.NgaySinh = DateOnly.FromDateTime(dob); 
                    }
                    
                    _context.SaveChanges();
                    
                    // Cập nhật lại Session tên hiển thị
                    HttpContext.Session.SetString("HoTen", HoTen);
                    TempData["Success"] = "Cập nhật hồ sơ thành công!";
                }
            }
            return RedirectToAction("Profile");
        }

        // --- ĐỔI MẬT KHẨU ---
        [HttpPost]
        public IActionResult ChangePassword(string OldPass, string NewPass, string ConfirmPass)
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");

            if (int.TryParse(maKhStr, out int maKh))
            {
                var khachHang = _context.KhachHangs
                    .Include(k => k.MaTkNavigation)
                    .FirstOrDefault(k => k.MaKh == maKh);
                
                if (khachHang?.MaTkNavigation != null)
                {
                    var taiKhoan = khachHang.MaTkNavigation;

                    // 1. Kiểm tra mật khẩu cũ
                    if (taiKhoan.MatKhau != OldPass)
                    {
                        TempData["PassError"] = "Mật khẩu cũ không chính xác!";
                        return RedirectToAction("Profile");
                    }

                    // 2. Kiểm tra mật khẩu mới
                    if (NewPass.Length < 6)
                    {
                        TempData["PassError"] = "Mật khẩu mới quá ngắn (tối thiểu 6 ký tự)!";
                        return RedirectToAction("Profile");
                    }

                    if (NewPass != ConfirmPass)
                    {
                        TempData["PassError"] = "Xác nhận mật khẩu không khớp!";
                        return RedirectToAction("Profile");
                    }

                    // 3. Kiểm tra xem đã xác thực SĐT/Email chưa (Yêu cầu bảo mật cao)
                    if (taiKhoan.EmailDaXacThuc != true && taiKhoan.SDTDaXacThuc != true)
                    {
                        TempData["PassError"] = "Vui lòng xác thực Email hoặc Số điện thoại trước khi đổi mật khẩu!";
                        return RedirectToAction("Profile");
                    }

                    // 4. Lưu mật khẩu mới
                    taiKhoan.MatKhau = NewPass;
                    _context.SaveChanges();
                    TempData["Success"] = "Đổi mật khẩu thành công!";
                }
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult SendOtp(string type, string value)
        {
            var sessionKey = HttpContext.Session.GetString("MaKh");
            if (sessionKey == null) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString($"OTP_{type}", otp);
            HttpContext.Session.SetString($"Pending_{type}", value);

            return Json(new { success = true, message = $"Mã OTP giả lập: {otp}", code = otp });
        }

        [HttpPost]
        public IActionResult VerifyOtp(string type, string value, string code)
        {
            var sessionOtp = HttpContext.Session.GetString($"OTP_{type}");
            var sessionValue = HttpContext.Session.GetString($"Pending_{type}");

            if (sessionOtp == code && sessionValue == value)
            {
                var maKhStr = HttpContext.Session.GetString("MaKh");
                if (maKhStr == null) return Json(new { success = false, message = "Hết phiên đăng nhập!" });

                if (int.TryParse(maKhStr, out int maKh))
                {
                    var khachHang = _context.KhachHangs
                        .Include(k => k.MaTkNavigation)
                        .FirstOrDefault(k => k.MaKh == maKh);

                    if (khachHang?.MaTkNavigation != null)
                    {
                        var taiKhoan = khachHang.MaTkNavigation;
                        
                        if (type == "email")
                        {
                            taiKhoan.EmailDaXacThuc = true;
                            taiKhoan.Email = value; 
                            khachHang.Email = value; 
                        }
                        else if (type == "phone")
                        {
                            taiKhoan.SDTDaXacThuc = true;
                            taiKhoan.Sdt = value;
                        }
                        
                        _context.SaveChanges();
                        return Json(new { success = true, message = "Xác thực thành công!" });
                    }
                }
            }

            return Json(new { success = false, message = "Mã xác thực không đúng!" });
        }
    }
}