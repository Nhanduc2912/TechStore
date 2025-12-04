namespace TechStore.ViewModels
{
    public class CartItem
    {
        public int MaHh { get; set; }
        // Thêm dấu ? (string?) để báo rằng cột này có thể null
        public string? TenHh { get; set; }
        public string? HinhAnh { get; set; }
        public double DonGia { get; set; }
        public int SoLuong { get; set; }
        public double ThanhTien => SoLuong * DonGia;
    }
}

