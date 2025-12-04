using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("HangHoa")]
public partial class HangHoa
{
    [Key]
    [Column("MaHH")]
    public int MaHh { get; set; }

    [Column("TenHH")]
    [StringLength(200)]
    public string TenHh { get; set; } = null!;

    [StringLength(200)]
    public string? TenAlias { get; set; }

    [Column("MaDM")]
    public int? MaDm { get; set; }

    [Column("MaTH")]
    public int? MaTh { get; set; }

    public double DonGia { get; set; }

    [StringLength(100)]
    public string? HinhAnh { get; set; }

    [StringLength(500)]
    public string? MoTaNgan { get; set; }

    [Column(TypeName = "ntext")]
    public string? MoTaChiTiet { get; set; }

    [Column("NgaySX", TypeName = "datetime")]
    public DateTime? NgaySx { get; set; }

    public int? SoLuotXem { get; set; }

    [StringLength(50)]
    public string? MauSac { get; set; }

    [StringLength(50)]
    public string? BoNho { get; set; }

    [StringLength(50)]
    public string? Ram { get; set; }

    [StringLength(50)]
    public string? HeDieuHanh { get; set; }

    public double? GiaNhap { get; set; }

    public int? SoLuong { get; set; }

    // --- THÊM CỘT HIỆU LỰC (SOFT DELETE) ---
    // Mặc định nên là true (Hiển thị)
    public bool? HieuLuc { get; set; }
    // ---------------------------------------

    [InverseProperty("MaHhNavigation")]
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    [ForeignKey("MaDm")]
    [InverseProperty("HangHoas")]
    public virtual DanhMuc? MaDmNavigation { get; set; }

    [ForeignKey("MaTh")]
    [InverseProperty("HangHoas")]
    public virtual ThuongHieu? MaThNavigation { get; set; }
}