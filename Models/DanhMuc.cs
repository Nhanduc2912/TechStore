using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models;

[Table("DanhMuc")]
public partial class DanhMuc
{
    [Key]
    [Column("MaDM")]
    public int MaDm { get; set; }

    [Column("TenDM")]
    [StringLength(100)]
    public string TenDm { get; set; } = null!;

    [StringLength(100)]
    public string? HinhAnh { get; set; }

    [InverseProperty("MaDmNavigation")]
    public virtual ICollection<HangHoa> HangHoas { get; set; } = new List<HangHoa>();
}
