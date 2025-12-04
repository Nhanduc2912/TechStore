using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("TrangThaiDonHang")]
public partial class TrangThaiDonHang
{
    [Key]
    public int MaTrangThai { get; set; }

    [StringLength(50)]
    public string? TenTrangThai { get; set; }

    [StringLength(100)]
    public string? MoTa { get; set; }

    [InverseProperty("MaTrangThaiNavigation")]
    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
