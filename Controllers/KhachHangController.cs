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

        // --- ĐĂNG KÝ ---
        [HttpPost]
        public IActionResult DangKy(string TenDangNhap, string MatKhau, string Email, string SDT, string HoTen)
        {
            try
            {
                TempData["Reg_HoTen"] = HoTen;
                TempData["Reg_TenDangNhap"] = TenDangNhap;
                TempData["Reg_Email"] = Email;
                TempData["Reg_SDT"] = SDT;

                if (string.IsNullOrEmpty(TenDangNhap) || TenDangNhap.Length < 3 || !Regex.IsMatch(TenDangNhap, "^[a-zA-Z0-9_]+$"))
                {
                    TempData["RegisterError"] = "Tên đăng nhập không hợp lệ (tối thiểu 3 ký tự, không dấu)!";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(MatKhau) || MatKhau.Length < 6)
                {
                    TempData["RegisterError"] = "Mật khẩu quá ngắn (tối thiểu 6 ký tự)!";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(Email) || !Regex.IsMatch(Email, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"))
                {
                    TempData["RegisterError"] = "Email không hợp lệ!";
                    return RedirectToAction("Index", "Home");
                }

                if (string.IsNullOrEmpty(SDT) || !Regex.IsMatch(SDT, @"^0\d{9}$"))
                {
                    TempData["RegisterError"] = "Số điện thoại không hợp lệ!";
                    return RedirectToAction("Index", "Home");
                }

                if (_context.TaiKhoans.Any(x => x.TenDangNhap == TenDangNhap))
                {
                    TempData["RegisterError"] = "Tên đăng nhập đã tồn tại!";
                    return RedirectToAction("Index", "Home");
                }

                if (_context.TaiKhoans.Any(x => x.Email == Email))
                {
                    TempData["RegisterError"] = "Email này đã được sử dụng!";
                    return RedirectToAction("Index", "Home");
                }

                TempData.Remove("Reg_HoTen");
                TempData.Remove("Reg_TenDangNhap");
                TempData.Remove("Reg_Email");
                TempData.Remove("Reg_SDT");

                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = TenDangNhap,
                    MatKhau = MatKhau,
                    Email = Email,
                    Sdt = SDT,
                    MaVaiTro = 3,
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

                HttpContext.Session.SetString("MaKh", khachHang.MaKh.ToString());
                HttpContext.Session.SetString("HoTen", khachHang.HoTen);
                HttpContext.Session.SetString("VaiTro", "KhachHang");

                TempData["Success"] = "Đăng ký thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["RegisterError"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // --- ĐĂNG NHẬP ---
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
                    HttpContext.Session.SetString("MaKh", tk.TenDangNhap);
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

        // --- ĐĂNG XUẤT ---
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // --- LỊCH SỬ ĐƠN HÀNG ---
        public IActionResult DonHang()
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");

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

        // --- HỒ SƠ CÁ NHÂN ---
        public IActionResult Profile()
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");

            if (int.TryParse(maKhStr, out int maKh))
            {
                var khachHang = _context.KhachHangs
                    .Include(k => k.MaTkNavigation)
                    .FirstOrDefault(k => k.MaKh == maKh);
                
                if (khachHang == null) return RedirectToAction("Index", "Home");

                return View(khachHang);
            }
            return RedirectToAction("Index", "Home");
        }

        // --- CẬP NHẬT THÔNG TIN ---
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
                        // SỬA LỖI CS0029: Chuyển đổi DateTime sang DateOnly
                        khachHang.NgaySinh = DateOnly.FromDateTime(dob);
                    }
                    
                    _context.SaveChanges();
                    
                    HttpContext.Session.SetString("HoTen", HoTen);
                    TempData["Success"] = "Cập nhật hồ sơ thành công!";
                }
            }
            return RedirectToAction("Profile");
        }

        // --- CẬP NHẬT ĐỊA CHỈ (MỚI) ---
        [HttpPost]
        public IActionResult UpdateAddress(string SoNha, string TinhThanh, string QuanHuyen, string PhuongXa)
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Index", "Home");

            if (int.TryParse(maKhStr, out int maKh))
            {
                var khachHang = _context.KhachHangs.Find(maKh);
                if (khachHang != null)
                {
                    // Gộp thành chuỗi địa chỉ
                    // Lưu ý: Các tham số TinhThanh, QuanHuyen... nhận được là Text (Tên) 
                    // do Script bên View đã xử lý chuyển value từ ID sang Text trước khi submit
                    khachHang.DiaChi = $"{SoNha}, {PhuongXa}, {QuanHuyen}, {TinhThanh}";
                    _context.SaveChanges();
                    TempData["Success"] = "Cập nhật địa chỉ nhận hàng thành công!";
                }
            }
            
            // Quay lại trang Profile
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

                    if (taiKhoan.MatKhau != OldPass)
                    {
                        TempData["PassError"] = "Mật khẩu cũ không đúng!";
                        return RedirectToAction("Profile");
                    }

                    if (NewPass.Length < 6)
                    {
                        TempData["PassError"] = "Mật khẩu mới quá ngắn!";
                        return RedirectToAction("Profile");
                    }

                    if (NewPass != ConfirmPass)
                    {
                        TempData["PassError"] = "Xác nhận mật khẩu không khớp!";
                        return RedirectToAction("Profile");
                    }

                    if (taiKhoan.EmailDaXacThuc != true && taiKhoan.SDTDaXacThuc != true)
                    {
                        TempData["PassError"] = "Vui lòng xác thực Email hoặc SĐT trước khi đổi mật khẩu!";
                        return RedirectToAction("Profile");
                    }

                    taiKhoan.MatKhau = NewPass;
                    _context.SaveChanges();
                    TempData["Success"] = "Đổi mật khẩu thành công!";
                }
            }
            return RedirectToAction("Profile");
        }

        // --- OTP ---
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