using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    /// <summary>
    /// Model cài đặt hệ thống
    /// </summary>
    [Table("SystemSettings")]
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? SettingKey { get; set; }

        [StringLength(1000)]
        public string? SettingValue { get; set; }

        [StringLength(500)]
        public string? MoTa { get; set; }

        [StringLength(50)]
        public string? LoaiDuLieu { get; set; } // string, int, bool, decimal

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime NgayCapNhat { get; set; } = DateTime.Now;
    }
}
