using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    /// <summary>
    /// Model ghi lại mọi hoạt động của Admin để audit trail
    /// </summary>
    [Table("AdminLog")]
    public class AdminLog
    {
        [Key]
        public int MaLog { get; set; }

        [Required]
        [StringLength(50)]
        public string? TenAdmin { get; set; }

        [Required]
        [StringLength(100)]
        public string? HanhDong { get; set; } // Create, Update, Delete, Export, Import

        [Required]
        [StringLength(100)]
        public string? Module { get; set; } // SanPham, DonHang, KhachHang...

        [StringLength(500)]
        public string? ChiTiet { get; set; } // Chi tiết hành động (VD: "Sửa sản phẩm ID: 5")

        [StringLength(500)]
        public string? DuLieuCu { get; set; } // Dữ liệu cũ (trước khi thay đổi)

        [StringLength(500)]
        public string? DuLieuMoi { get; set; } // Dữ liệu mới (sau khi thay đổi)

        [StringLength(50)]
        public string? IPAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [Required]
        public DateTime ThoiGian { get; set; } = DateTime.Now;

        public int? TrangtaiHanhDong { get; set; } // 1: Success, 0: Failed

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
    }
}
