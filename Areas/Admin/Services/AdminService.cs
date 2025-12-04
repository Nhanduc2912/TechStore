using TechStore.Data;
using TechStore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Areas.Admin.Services
{
    /// <summary>
    /// Service xử lý các logic chung của Admin
    /// </summary>
    public class AdminService
    {
        private readonly TechStoreContext _context;

        public AdminService(TechStoreContext context)
        {
            _context = context;
        }

        #region Dashboard Stats

        /// <summary>
        /// Lấy thống kê dashboard (doanh thu, đơn, sản phẩm, khách)
        /// </summary>
        public async Task<dynamic> GetDashboardStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            fromDate ??= DateTime.Now.AddMonths(-1);
            toDate ??= DateTime.Now;

            var tongSanPham = await _context.HangHoas.CountAsync();
            var tongKhachHang = await _context.KhachHangs.CountAsync();
            var tongDonHang = await _context.HoaDons.CountAsync();

            var doanhThu = await _context.HoaDons
                .Where(h => h.NgayDat >= fromDate && h.NgayDat <= toDate)
                .Include(h => h.ChiTietHoaDons)
                .SelectMany(h => h.ChiTietHoaDons)
                .SumAsync(ct => ct.DonGia * ct.SoLuong);

            var donHangThang = await _context.HoaDons
                .Where(h => h.NgayDat >= fromDate && h.NgayDat <= toDate)
                .CountAsync();

            return new
            {
                TongSanPham = tongSanPham,
                TongKhachHang = tongKhachHang,
                TongDonHang = tongDonHang,
                DoanhThu = doanhThu,
                DonHangThang = donHangThang
            };
        }

        #endregion

        #region Audit Logging

        /// <summary>
        /// Ghi log hoạt động của Admin
        /// </summary>
        public async Task LogActivityAsync(string tenAdmin, string hanhDong, string module, 
            string chiTiet, string ipAddress, string userAgent, int trangThai = 1, string? errorMsg = null)
        {
            var adminLog = new AdminLog
            {
                TenAdmin = tenAdmin,
                HanhDong = hanhDong,
                Module = module,
                ChiTiet = chiTiet,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                ThoiGian = DateTime.Now,
                TrangtaiHanhDong = trangThai,
                ErrorMessage = errorMsg
            };

            _context.AdminLogs.Add(adminLog);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy lịch sử hoạt động
        /// </summary>
        public async Task<List<AdminLog>> GetActivityLogsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.AdminLogs
                .OrderByDescending(x => x.ThoiGian)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Đếm tổng activity logs
        /// </summary>
        public async Task<int> CountActivityLogsAsync()
        {
            return await _context.AdminLogs.CountAsync();
        }

        #endregion

        #region Chart Data

        /// <summary>
        /// Lấy dữ liệu biểu đồ doanh thu theo ngày
        /// </summary>
        public async Task<dynamic> GetRevenueChartDataAsync(int days = 7)
        {
            var fromDate = DateTime.Now.AddDays(-days);
            var toDate = DateTime.Now;

            var data = await _context.HoaDons
                .Where(h => h.NgayDat >= fromDate && h.NgayDat <= toDate)
                .GroupBy(h => h.NgayDat!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("dd/MM"),
                    Revenue = g.SelectMany(h => h.ChiTietHoaDons)
                        .Sum(ct => ct.DonGia * ct.SoLuong)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new
            {
                Labels = data.Select(x => x.Date).ToList(),
                Data = data.Select(x => x.Revenue).ToList()
            };
        }

        /// <summary>
        /// Lấy dữ liệu Top sản phẩm bán chạy
        /// </summary>
        public async Task<dynamic> GetTopProductsAsync(int limit = 5)
        {
            var data = await _context.ChiTietHoaDons
                .GroupBy(ct => ct.MaHh)
                .Select(g => new
                {
                    MaHh = g.Key,
                    TenHh = g.First().MaHhNavigation!.TenHh,
                    SoLuong = g.Sum(ct => ct.SoLuong),
                    DoanhThu = g.Sum(ct => ct.DonGia * ct.SoLuong)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(limit)
                .ToListAsync();

            return data;
        }

        /// <summary>
        /// Lấy dữ liệu trạng thái đơn hàng
        /// </summary>
        public async Task<dynamic> GetOrderStatusChartDataAsync()
        {
            var data = await _context.HoaDons
                .GroupBy(h => h.MaTrangThai)
                .Select(g => new
                {
                    Status = g.First().MaTrangThaiNavigation!.TenTrangThai,
                    Count = g.Count()
                })
                .ToListAsync();

            return new
            {
                Labels = data.Select(x => x.Status).ToList(),
                Data = data.Select(x => x.Count).ToList()
            };
        }

        #endregion

        #region System Settings

        /// <summary>
        /// Lấy cài đặt hệ thống
        /// </summary>
        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);
            return setting?.SettingValue ?? defaultValue;
        }

        /// <summary>
        /// Lưu/cập nhật cài đặt hệ thống
        /// </summary>
        public async Task SaveSettingAsync(string key, string value, string moTa = "", string loaiDuLieu = "string")
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting == null)
            {
                setting = new SystemSettings
                {
                    SettingKey = key,
                    SettingValue = value,
                    MoTa = moTa,
                    LoaiDuLieu = loaiDuLieu,
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
                setting.NgayCapNhat = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Format số tiền sang định dạng VND
        /// </summary>
        public string FormatCurrency(decimal? amount)
        {
            return (amount ?? 0).ToString("#,##0") + "₫";
        }

        /// <summary>
        /// Format ngày giờ
        /// </summary>
        public string FormatDateTime(DateTime? date)
        {
            return date?.ToString("dd/MM/yyyy HH:mm") ?? "-";
        }

        /// <summary>
        /// Lấy badge class cho trạng thái
        /// </summary>
        public string GetStatusBadgeClass(int? status)
        {
            return status switch
            {
                0 => "badge bg-warning text-dark",      // Mới đặt
                1 => "badge bg-info",                   // Đã duyệt
                2 => "badge bg-primary",                // Đang giao
                3 => "badge bg-success",                // Hoàn thành
                -1 => "badge bg-secondary",             // Hủy
                _ => "badge bg-light text-dark"
            };
        }

        #endregion
    }
}
