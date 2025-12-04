using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("ThuongHieu")]
public partial class ThuongHieu
{
    [Key]
    [Column("MaTH")]
    public int MaTh { get; set; }

    [Column("TenTH")]
    [StringLength(100)]
    public string TenTh { get; set; } = null!;

    [StringLength(100)]
    public string? Logo { get; set; }

    [StringLength(50)]
    public string? QuocGia { get; set; }

    [InverseProperty("MaThNavigation")]
    public virtual ICollection<HangHoa> HangHoas { get; set; } = new List<HangHoa>();
}
