using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("LoaiKhachHang")]
public partial class LoaiKhachHang
{
    [Key]
    [Column("MaLoaiKH")]
    public int MaLoaiKh { get; set; }

    [StringLength(50)]
    public string? TenLoai { get; set; }

    public int? DiemToiThieu { get; set; }

    public double? GiamGia { get; set; }

    [InverseProperty("MaLoaiKhNavigation")]
    public virtual ICollection<KhachHang> KhachHangs { get; set; } = new List<KhachHang>();
}
