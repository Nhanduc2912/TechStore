using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("ChiTietHoaDon")]
public partial class ChiTietHoaDon
{
    [Key]
    [Column("MaCT")]
    public int MaCt { get; set; }

    [Column("MaHD")]
    public int? MaHd { get; set; }

    [Column("MaHH")]
    public int? MaHh { get; set; }

    public double DonGia { get; set; }

    public int SoLuong { get; set; }

    public double? GiamGia { get; set; }

    [ForeignKey("MaHd")]
    [InverseProperty("ChiTietHoaDons")]
    public virtual HoaDon? MaHdNavigation { get; set; }

    [ForeignKey("MaHh")]
    [InverseProperty("ChiTietHoaDons")]
    public virtual HangHoa? MaHhNavigation { get; set; }
}
