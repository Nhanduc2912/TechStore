using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using TechStore.Areas.Admin.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/danhmuc")]
    [AdminOnly]
    public class DanhMucAdminController : Controller
    {
        private readonly TechStoreContext _db;

        public DanhMucAdminController(TechStoreContext context)
        {
            _db = context;
        }

        // --- DANH SÁCH ---
        [Route("")]
        [Route("index")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _db.DanhMucs
                .Include(d => d.HangHoas)
                .ToListAsync();
            return View(categories);
        }

        // --- TẠO MỚI ---
        [Route("create")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Route("store")]
        [HttpPost]
        public async Task<IActionResult> Store([Bind("TenDm,HinhAnh")] DanhMuc model, IFormFile? hinhanh)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Xử lý upload ảnh
                    if (hinhanh != null && hinhanh.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(hinhanh.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/danhmuc", fileName);
                        
                        // FIX LỖI: Tạo thư mục nếu chưa có
                        var dir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) 
                            Directory.CreateDirectory(dir);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await hinhanh.CopyToAsync(stream);
                        }
                        model.HinhAnh = fileName;
                    }

                    _db.DanhMucs.Add(model);
                    await _db.SaveChangesAsync();

                    TempData["Success"] = "Thêm danh mục thành công!";
                    return RedirectToAction("Index");
                }
                return View("Create", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return View("Create", model);
            }
        }

        // --- CẬP NHẬT ---
        [Route("edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.DanhMucs.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [Route("update/{id}")]
        [HttpPost]
        public async Task<IActionResult> Update(int id, [Bind("MaDm,TenDm,HinhAnh")] DanhMuc model, IFormFile? hinhanh)
        {
            if (id != model.MaDm) return NotFound();

            try
            {
                // Load lại entity cũ để lấy ảnh cũ (nếu không chọn ảnh mới)
                var existingCategory = await _db.DanhMucs.AsNoTracking().FirstOrDefaultAsync(x => x.MaDm == id);
                if (existingCategory == null) return NotFound();

                if (ModelState.IsValid)
                {
                    if (hinhanh != null && hinhanh.Length > 0)
                    {
                        // 1. Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingCategory.HinhAnh))
                        {
                            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/danhmuc", existingCategory.HinhAnh);
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        // 2. Lưu ảnh mới
                        var fileName = Guid.NewGuid() + Path.GetExtension(hinhanh.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/danhmuc", fileName);
                        
                        // FIX LỖI: Tạo thư mục nếu chưa có
                        var dir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) 
                            Directory.CreateDirectory(dir);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await hinhanh.CopyToAsync(stream);
                        }
                        model.HinhAnh = fileName;
                    }
                    else
                    {
                        // Giữ nguyên ảnh cũ
                        model.HinhAnh = existingCategory.HinhAnh;
                    }

                    _db.Update(model);
                    await _db.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction("Index");
                }
                return View("Edit", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
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
                var category = await _db.DanhMucs
                    .Include(d => d.HangHoas)
                    .FirstOrDefaultAsync(d => d.MaDm == id);

                if (category == null) return NotFound();

                // Kiểm tra ràng buộc
                if (category.HangHoas.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa! Danh mục này đang chứa " + category.HangHoas.Count + " sản phẩm." });
                }

                // Xóa ảnh
                if (!string.IsNullOrEmpty(category.HinhAnh))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/danhmuc", category.HinhAnh);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

                _db.DanhMucs.Remove(category);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}