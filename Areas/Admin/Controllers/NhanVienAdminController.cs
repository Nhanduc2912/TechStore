using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using TechStore.Areas.Admin.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/nhanvien")]
    [AdminOnly]
    public class NhanVienAdminController : Controller
    {
        private readonly TechStoreContext _db;

        public NhanVienAdminController(TechStoreContext context)
        {
            _db = context;
        }

        // --- DANH SÁCH ---
        [Route("")]
        [Route("index")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var employees = await _db.NhanViens
                .Include(nv => nv.MaTkNavigation)
                .OrderByDescending(nv => nv.MaNv)
                .ToListAsync();
            return View(employees);
        }

        // --- THÊM MỚI ---
        [Route("create")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Route("store")]
        [HttpPost]
        public async Task<IActionResult> Store(NhanVien model, string TenDangNhap, string MatKhau, string Email, string Sdt)
        {
            // Kiểm tra trùng lặp
            if (await _db.TaiKhoans.AnyAsync(tk => tk.TenDangNhap == TenDangNhap || tk.Email == Email))
            {
                TempData["Error"] = "Tên đăng nhập hoặc Email đã tồn tại!";
                return View("Create", model);
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tạo Tài khoản (Vai trò 2: Staff)
                    var taiKhoan = new TaiKhoan
                    {
                        TenDangNhap = TenDangNhap,
                        MatKhau = MatKhau, 
                        Email = Email,
                        Sdt = Sdt,
                        MaVaiTro = 2, 
                        TrangThai = true,
                        NgayTao = DateTime.Now
                    };
                    _db.TaiKhoans.Add(taiKhoan);
                    await _db.SaveChangesAsync();

                    // 2. Tạo Nhân viên
                    model.MaTk = taiKhoan.MaTk;
                    model.NgayVaoLam = model.NgayVaoLam ?? DateOnly.FromDateTime(DateTime.Now);
                    
                    _db.NhanViens.Add(model);
                    await _db.SaveChangesAsync();

                    transaction.Commit();
                    TempData["Success"] = "Thêm nhân viên thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Lỗi: " + ex.Message;
                    return View("Create", model);
                }
            }
        }

        // --- CHI TIẾT ---
        [Route("detail/{id}")]
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var nhanVien = await _db.NhanViens
                .Include(n => n.MaTkNavigation)
                .FirstOrDefaultAsync(n => n.MaNv == id);

            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // --- CẬP NHẬT ---
        [Route("edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _db.NhanViens
                .Include(n => n.MaTkNavigation)
                .FirstOrDefaultAsync(n => n.MaNv == id);

            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        [Route("update/{id}")]
        [HttpPost]
        public async Task<IActionResult> Update(int id, NhanVien model, string Email, string Sdt, bool TrangThai, string? MatKhauMoi)
        {
            if (id != model.MaNv) return NotFound();

            try
            {
                var nhanVien = await _db.NhanViens
                    .Include(n => n.MaTkNavigation)
                    .FirstOrDefaultAsync(n => n.MaNv == id);

                if (nhanVien == null) return NotFound();

                // 1. Cập nhật thông tin nhân viên
                nhanVien.HoTen = model.HoTen;
                nhanVien.DiaChi = model.DiaChi;
                nhanVien.NgaySinh = model.NgaySinh;
                nhanVien.NgayVaoLam = model.NgayVaoLam;

                // 2. Cập nhật thông tin tài khoản liên kết
                if (nhanVien.MaTkNavigation != null)
                {
                    // Kiểm tra trùng Email/SĐT với người khác (trừ chính mình)
                    var exists = await _db.TaiKhoans.AnyAsync(t => 
                        (t.Email == Email || t.Sdt == Sdt) && t.MaTk != nhanVien.MaTk);
                    
                    if (exists)
                    {
                        TempData["Error"] = "Email hoặc Số điện thoại đã được sử dụng bởi tài khoản khác!";
                        return View("Edit", nhanVien);
                    }

                    nhanVien.MaTkNavigation.Email = Email;
                    nhanVien.MaTkNavigation.Sdt = Sdt;
                    nhanVien.MaTkNavigation.TrangThai = TrangThai; // Khóa/Mở khóa

                    // Đổi mật khẩu nếu có nhập
                    if (!string.IsNullOrEmpty(MatKhauMoi))
                    {
                        nhanVien.MaTkNavigation.MatKhau = MatKhauMoi;
                    }
                }

                _db.Update(nhanVien);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Cập nhật thông tin nhân viên thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi cập nhật: " + ex.Message;
                return View("Edit", model);
            }
        }

        // --- XÓA ---
        [Route("delete/{id}")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var nv = await _db.NhanViens.Include(n => n.MaTkNavigation).FirstOrDefaultAsync(n => n.MaNv == id);
                if (nv == null) return Json(new { success = false, message = "Không tìm thấy" });

                // Xóa cả tài khoản liên quan
                if (nv.MaTkNavigation != null) _db.TaiKhoans.Remove(nv.MaTkNavigation);
                _db.NhanViens.Remove(nv);
                
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa nhân viên và tài khoản liên quan." });
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}