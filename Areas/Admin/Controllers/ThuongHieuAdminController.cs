using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Models;
using TechStore.Areas.Admin.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/thuonghieu")]
    [AdminOnly]
    public class ThuongHieuAdminController : Controller
    {
        private readonly TechStoreContext _db;

        public ThuongHieuAdminController(TechStoreContext context)
        {
            _db = context;
        }

        [Route("")]
        [Route("index")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Include để đếm số sản phẩm thuộc thương hiệu
            var brands = await _db.ThuongHieus
                .Include(b => b.HangHoas) 
                .ToListAsync();
            return View(brands);
        }

        [Route("create")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Route("store")]
        [HttpPost]
        public async Task<IActionResult> Store([Bind("TenTh,QuocGia")] ThuongHieu model, IFormFile? logo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (logo != null && logo.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(logo.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/thuonghieu", fileName);
                        
                        var dir = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await logo.CopyToAsync(stream);
                        }
                        model.Logo = fileName;
                    }

                    _db.ThuongHieus.Add(model);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Thêm thương hiệu thành công!";
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

        [Route("edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _db.ThuongHieus.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [Route("update/{id}")]
        [HttpPost]
        public async Task<IActionResult> Update(int id, [Bind("MaTh,TenTh,QuocGia,Logo")] ThuongHieu model, IFormFile? logo)
        {
            if (id != model.MaTh) return NotFound();

            try
            {
                // Giữ lại logo cũ nếu không chọn mới
                var existingBrand = await _db.ThuongHieus.AsNoTracking().FirstOrDefaultAsync(x => x.MaTh == id);
                if (existingBrand == null) return NotFound();

                if (ModelState.IsValid)
                {
                    if (logo != null && logo.Length > 0)
                    {
                        // Xóa logo cũ
                        if (!string.IsNullOrEmpty(existingBrand.Logo))
                        {
                            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/thuonghieu", existingBrand.Logo);
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        var fileName = Guid.NewGuid() + Path.GetExtension(logo.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/thuonghieu", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await logo.CopyToAsync(stream);
                        }
                        model.Logo = fileName;
                    }
                    else
                    {
                        model.Logo = existingBrand.Logo;
                    }

                    _db.Update(model);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thương hiệu thành công!";
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

        [Route("delete/{id}")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _db.ThuongHieus
                    .Include(b => b.HangHoas)
                    .FirstOrDefaultAsync(b => b.MaTh == id);

                if (brand == null) return NotFound();

                // Ràng buộc: Không xóa nếu có sản phẩm
                if (brand.HangHoas.Any())
                {
                    return Json(new { success = false, message = $"Không thể xóa! Thương hiệu này đang có {brand.HangHoas.Count} sản phẩm." });
                }

                if (!string.IsNullOrEmpty(brand.Logo))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hinh/thuonghieu", brand.Logo);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

                _db.ThuongHieus.Remove(brand);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa thương hiệu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}