using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("HoaDon")]
public partial class HoaDon
{
    [Key]
    [Column("MaHD")]
    public int MaHd { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayDat { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayGiao { get; set; }

    [StringLength(100)]
    public string? HoTenNguoiNhan { get; set; }

    [StringLength(255)]
    public string? DiaChiNguoiNhan { get; set; }

    [Column("SDTNguoiNhan")]
    [StringLength(20)]
    public string? SdtnguoiNhan { get; set; }

    public string? GhiChu { get; set; }

    public double? PhiVanChuyen { get; set; }

    public int? MaTrangThai { get; set; }

    [Column("MaKH")]
    public int? MaKh { get; set; }

    [Column("MaNV")]
    public int? MaNv { get; set; }

    [InverseProperty("MaHdNavigation")]
    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    [ForeignKey("MaKh")]
    [InverseProperty("HoaDons")]
    public virtual KhachHang? MaKhNavigation { get; set; }

    [ForeignKey("MaNv")]
    [InverseProperty("HoaDons")]
    public virtual NhanVien? MaNvNavigation { get; set; }

    [ForeignKey("MaTrangThai")]
    [InverseProperty("HoaDons")]
    public virtual TrangThaiDonHang? MaTrangThaiNavigation { get; set; }
}
