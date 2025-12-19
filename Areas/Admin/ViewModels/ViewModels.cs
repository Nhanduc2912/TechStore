namespace TechStore.ViewModels
{
    public class BaoCaoSanPhamVM
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; } = ""; // Fix: Gán mặc định rỗng
        public int SoLuong { get; set; }
        public double DoanhThu { get; set; }
        public int LanMua { get; set; }
    }

    public class BaoCaoKhachHangVM
    {
        public int MaKh { get; set; }
        public string HoTen { get; set; } = ""; // Fix
        public int TongDonHang { get; set; }
        public double TongTienMua { get; set; }
        public int LanMuaGanDay { get; set; }
    }

    public class BaoCaoTonKhoVM
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; } = ""; // Fix
        public string DanhMuc { get; set; } = ""; // Fix
        public int SoLuong { get; set; }
        public double GiaNhap { get; set; }
        public double GiaBan { get; set; }
        public double GiaTriTonKho { get; set; }
    }
}