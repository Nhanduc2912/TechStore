using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using TechStore.Areas.Admin.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using TechStore.Helpers;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/sanpham")]
    [AdminOnly]
    public class SanPhamAdminController : Controller
    {
        private readonly TechStoreContext _db;

        public SanPhamAdminController(TechStoreContext context)
        {
            _db = context;
        }

        // ... (Các hàm Index, Create, Store giữ nguyên) ...

        // === HELPER ===
        private void LoadViewBag(int? selectedDm = null, int? selectedTh = null)
        {
            var danhMucs = _db.DanhMucs.OrderBy(d => d.TenDm).ToList();
            var thuongHieus = _db.ThuongHieus.OrderBy(t => t.TenTh).ToList();

            ViewBag.Categories = new SelectList(danhMucs, "MaDm", "TenDm", selectedDm);
            ViewBag.Brands = new SelectList(thuongHieus, "MaTh", "TenTh", selectedTh);
        }

        // ... (Hàm Index giữ nguyên) ...
        [Route("")]
        [Route("index")]
        [HttpGet]
        public async Task<IActionResult> Index(string search = "", int category = 0, int page = 1)
        {
            const int pageSize = 10;
            var query = _db.HangHoas.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.TenHh.Contains(search));
                ViewBag.Search = search;
            }

            if (category > 0)
            {
                query = query.Where(p => p.MaDm == category);
                ViewBag.Category = category;
            }

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var products = await query
                .Include(p => p.MaDmNavigation)
                .Include(p => p.MaThNavigation)
                .Include(p => p.ChiTietHoaDons)
                .OrderByDescending(p => p.MaHh)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            LoadViewBag(category); 
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;

            return View(products);
        }

        [Route("create")]
        [HttpGet]
        public IActionResult Create()
        {
            LoadViewBag();
            return View();
        }

        [Route("store")]
        [HttpPost]
        public async Task<IActionResult> Store(HangHoa model, IFormFile hinhanh)
        {
            ModelState.Remove("TenAlias");
            ModelState.Remove("MaDmNavigation");
            ModelState.Remove("MaThNavigation");
            
            // Khi tạo mới thì HinhAnh là bắt buộc nếu bạn muốn (hoặc không)
            // Ở đây tôi để lỏng validation để tránh lỗi, check manual
            ModelState.Remove("HinhAnh"); 

            if (!ModelState.IsValid)
            {
                LoadViewBag(model.MaDm, model.MaTh);
                return View("Create", model);
            }

            try
            {
                if (hinhanh != null)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(hinhanh.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/hanghoa", fileName);
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await hinhanh.CopyToAsync(stream);
                    }
                    model.HinhAnh = fileName;
                }

                model.TenAlias = MyUtil.ToUrlFriendly(model.TenHh);
                model.NgaySx = DateTime.Now;
                model.SoLuotXem = 0;
                model.HieuLuc = true; // Mặc định hiện

                _db.HangHoas.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi lưu DB: " + ex.Message;
                LoadViewBag(model.MaDm, model.MaTh);
                return View("Create", model);
            }
        }

        // --- EDIT (GET) ---
        [Route("edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.HangHoas.FindAsync(id);
            if (product == null) return NotFound();

            LoadViewBag(product.MaDm, product.MaTh);
            return View(product);
        }

        // --- UPDATE (POST) - ĐÃ FIX LỖI ẢNH VÀ TRẠNG THÁI ---
        [Route("update/{id}")]
        [HttpPost]
        public async Task<IActionResult> Update(int id, HangHoa model, IFormFile hinhanh)
        {
            if (id != model.MaHh) return NotFound();

            // 1. Loại bỏ Validation cho các trường không nhập hoặc tự sinh
            ModelState.Remove("TenAlias");
            ModelState.Remove("MaDmNavigation");
            ModelState.Remove("MaThNavigation");
            ModelState.Remove("HinhAnh"); // FIX: Bỏ validate ảnh vì Edit không bắt buộc chọn ảnh mới

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu nhập vào không hợp lệ!";
                LoadViewBag(model.MaDm, model.MaTh);
                return View("Edit", model);
            }

            try
            {
                // Lấy sản phẩm cũ từ DB ra để cập nhật
                var existingProduct = await _db.HangHoas.FindAsync(id);
                if (existingProduct == null) return NotFound();

                // 2. Xử lý ảnh: Chỉ cập nhật nếu người dùng chọn file mới
                if (hinhanh != null)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existingProduct.HinhAnh))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/hanghoa", existingProduct.HinhAnh);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    // Lưu ảnh mới
                    var fileName = Guid.NewGuid() + Path.GetExtension(hinhanh.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/hanghoa", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await hinhanh.CopyToAsync(stream);
                    }
                    existingProduct.HinhAnh = fileName;
                }
                // Nếu hinhanh == null -> Giữ nguyên existingProduct.HinhAnh cũ

                // 3. Cập nhật các thông tin khác
                existingProduct.TenHh = model.TenHh;
                existingProduct.MoTaNgan = model.MoTaNgan;
                existingProduct.MoTaChiTiet = model.MoTaChiTiet;
                existingProduct.GiaNhap = model.GiaNhap;
                existingProduct.DonGia = model.DonGia;
                existingProduct.SoLuong = model.SoLuong;
                existingProduct.MaDm = model.MaDm;
                existingProduct.MaTh = model.MaTh;
                existingProduct.Ram = model.Ram;
                existingProduct.BoNho = model.BoNho;
                existingProduct.MauSac = model.MauSac;
                existingProduct.HeDieuHanh = model.HeDieuHanh;
                
                // FIX: Cập nhật trạng thái Hiệu lực từ form (Checkbox switch)
                // Lưu ý: Nếu checkbox không được check, model.HieuLuc có thể là null hoặc false.
                // Ta cần đảm bảo nó lấy đúng giá trị gửi lên.
                existingProduct.HieuLuc = model.HieuLuc; 
                
                existingProduct.TenAlias = MyUtil.ToUrlFriendly(model.TenHh);

                _db.Update(existingProduct);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = "Lỗi cập nhật: " + msg;
                LoadViewBag(model.MaDm, model.MaTh);
                return View("Edit", model);
            }
        }

        // ... (Các hàm Detail, Delete, ToggleStatus giữ nguyên) ...
        [Route("detail/{id}")]
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _db.HangHoas
                .Include(p => p.MaDmNavigation)
                .Include(p => p.MaThNavigation)
                .FirstOrDefaultAsync(p => p.MaHh == id);

            if (product == null) return NotFound();
            return View(product);
        }

        [Route("delete/{id}")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try 
            {
                var product = await _db.HangHoas.Include(p => p.ChiTietHoaDons).FirstOrDefaultAsync(p => p.MaHh == id);
                if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

                if (product.ChiTietHoaDons.Any())
                {
                    product.HieuLuc = false;
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "Sản phẩm đã bán, chuyển sang trạng thái Ẩn." });
                }

                if (!string.IsNullOrEmpty(product.HinhAnh))
                {
                    var filePath = Path.Combine("wwwroot/hinh/hanghoa", product.HinhAnh);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

                _db.HangHoas.Remove(product);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa thành công!" }); 
            }
            catch(Exception ex)
            {
                 return Json(new { success = false, message = ex.Message });
            }
        }

        [Route("ToggleStatus/{id}")]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var product = await _db.HangHoas.FindAsync(id);
                if (product == null) return Json(new { success = false, message = "Không tìm thấy" });

                product.HieuLuc = !(product.HieuLuc ?? true);
                _db.Update(product);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}   