using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("TaiKhoan")]
[Index("TenDangNhap", Name = "UQ__TaiKhoan__55F68FC0C88F7813", IsUnique = true)]
[Index("Email", Name = "UQ__TaiKhoan__A9D1053437952049", IsUnique = true)]
public partial class TaiKhoan
{
    [Key]
    [Column("MaTK")]
    public int MaTk { get; set; }

    [StringLength(50)]
    public string TenDangNhap { get; set; } = null!;

    [StringLength(100)]
    public string MatKhau { get; set; } = null!;

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Column("SDT")]
    [StringLength(15)]
    public string? Sdt { get; set; }

    public bool? TrangThai { get; set; }

    public int? MaVaiTro { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayTao { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LanDangNhapCuoi { get; set; }

    [InverseProperty("MaTkNavigation")]
    public virtual KhachHang? KhachHang { get; set; }

    [ForeignKey("MaVaiTro")]
    [InverseProperty("TaiKhoans")]
    public virtual VaiTro? MaVaiTroNavigation { get; set; }

    [InverseProperty("MaTkNavigation")]
    public virtual NhanVien? NhanVien { get; set; }
    public bool? EmailDaXacThuc { get; set; }
    public bool? SDTDaXacThuc { get; set; }
}
