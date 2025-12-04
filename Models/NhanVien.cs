using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("NhanVien")]
[Index("MaTk", Name = "UQ__NhanVien__27250071308C7F76", IsUnique = true)]
public partial class NhanVien
{
    [Key]
    [Column("MaNV")]
    public int MaNv { get; set; }

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    [StringLength(255)]
    public string? DiaChi { get; set; }

    public DateOnly? NgayVaoLam { get; set; }

    [Column("MaTK")]
    public int? MaTk { get; set; }

    [InverseProperty("MaNvNavigation")]
    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    [ForeignKey("MaTk")]
    [InverseProperty("NhanVien")]
    public virtual TaiKhoan? MaTkNavigation { get; set; }
}
