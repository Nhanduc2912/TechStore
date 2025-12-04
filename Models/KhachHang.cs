using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("KhachHang")]
[Index("MaTk", Name = "UQ__KhachHan__27250071B613AFF9", IsUnique = true)]
public partial class KhachHang
{
    [Key]
    [Column("MaKH")]
    public int MaKh { get; set; }

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(255)]
    public string? DiaChi { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public int? DiemTichLuy { get; set; }

    [Column("MaLoaiKH")]
    public int? MaLoaiKh { get; set; }

    [Column("MaTK")]
    public int? MaTk { get; set; }
    public string? Email { get; set; }

    [InverseProperty("MaKhNavigation")]
    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    [ForeignKey("MaLoaiKh")]
    [InverseProperty("KhachHangs")]
    public virtual LoaiKhachHang? MaLoaiKhNavigation { get; set; }

    [ForeignKey("MaTk")]
    [InverseProperty("KhachHang")]
    public virtual TaiKhoan? MaTkNavigation { get; set; }
}
