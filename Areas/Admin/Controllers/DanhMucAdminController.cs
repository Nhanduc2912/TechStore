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
    [Route("admin/danhmuc")]
    [AdminOnly] // Đảm bảo chỉ Admin mới vào được
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
                .Include(d => d.HangHoas) // Include để đếm số sản phẩm con
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
        public async Task<IActionResult> Store([Bind("TenDm,HinhAnh")] DanhMuc model) 
        {
            // Lưu ý: Đã bỏ "MoTa" khỏi Bind vì DB không có cột này
            try
            {
                if (ModelState.IsValid)
                {
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
        public async Task<IActionResult> Update(int id, [Bind("MaDm,TenDm,HinhAnh")] DanhMuc model)
        {
            if (id != model.MaDm) return NotFound();

            try
            {
                if (ModelState.IsValid)
                {
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

        // --- XÓA (AJAX) ---
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

                // Kiểm tra ràng buộc: Nếu danh mục có sản phẩm thì không cho xóa
                if (category.HangHoas.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa! Danh mục này đang chứa sản phẩm." });
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