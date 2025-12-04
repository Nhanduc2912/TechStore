using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Data;

public partial class TechStoreContext : DbContext
{
    public TechStoreContext()
    {
    }

    public TechStoreContext(DbContextOptions<TechStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<HangHoa> HangHoas { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<LoaiKhachHang> LoaiKhachHangs { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<ThuongHieu> ThuongHieus { get; set; }

    public virtual DbSet<TrangThaiDonHang> TrangThaiDonHangs { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<AdminLog> AdminLogs { get; set; }

    public virtual DbSet<SystemSettings> SystemSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-7EN18NR;Database=TechStoreDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => e.MaCt).HasName("PK__ChiTietH__27258E744C0E6DB8");

            entity.Property(e => e.GiamGia).HasDefaultValue(0.0);

            entity.HasOne(d => d.MaHdNavigation).WithMany(p => p.ChiTietHoaDons)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ChiTietHoa__MaHD__5FB337D6");

            entity.HasOne(d => d.MaHhNavigation).WithMany(p => p.ChiTietHoaDons).HasConstraintName("FK__ChiTietHoa__MaHH__60A75C0F");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.MaDm).HasName("PK__DanhMuc__2725866E609FEE31");
        });

        modelBuilder.Entity<HangHoa>(entity =>
        {
            entity.HasKey(e => e.MaHh).HasName("PK__HangHoa__2725A6E42250DC43");

            entity.Property(e => e.SoLuotXem).HasDefaultValue(0);

            entity.HasOne(d => d.MaDmNavigation).WithMany(p => p.HangHoas).HasConstraintName("FK__HangHoa__MaDM__5165187F");

            entity.HasOne(d => d.MaThNavigation).WithMany(p => p.HangHoas).HasConstraintName("FK__HangHoa__MaTH__52593CB8");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHd).HasName("PK__HoaDon__2725A6E06F3A3198");

            entity.Property(e => e.MaTrangThai).HasDefaultValue(0);
            entity.Property(e => e.NgayDat).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PhiVanChuyen).HasDefaultValue(0.0);

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.HoaDons).HasConstraintName("FK__HoaDon__MaKH__5BE2A6F2");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.HoaDons).HasConstraintName("FK__HoaDon__MaNV__5CD6CB2B");

            entity.HasOne(d => d.MaTrangThaiNavigation).WithMany(p => p.HoaDons).HasConstraintName("FK__HoaDon__MaTrangT__59FA5E80");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKh).HasName("PK__KhachHan__2725CF1EF138E0CE");

            entity.Property(e => e.DiemTichLuy).HasDefaultValue(0);
            entity.Property(e => e.MaLoaiKh).HasDefaultValue(1);

            entity.HasOne(d => d.MaLoaiKhNavigation).WithMany(p => p.KhachHangs).HasConstraintName("FK__KhachHang__MaLoa__49C3F6B7");

            entity.HasOne(d => d.MaTkNavigation).WithOne(p => p.KhachHang).HasConstraintName("FK__KhachHang__MaTK__4AB81AF0");
        });

        modelBuilder.Entity<LoaiKhachHang>(entity =>
        {
            entity.HasKey(e => e.MaLoaiKh).HasName("PK__LoaiKhac__12250B7E675248F7");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70AB6FDBB41");

            entity.Property(e => e.NgayVaoLam).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaTkNavigation).WithOne(p => p.NhanVien).HasConstraintName("FK__NhanVien__MaTK__4222D4EF");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.MaTk).HasName("PK__TaiKhoan__272500702BBF4105");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.TaiKhoans).HasConstraintName("FK__TaiKhoan__MaVaiT__3C69FB99");
        });

        modelBuilder.Entity<ThuongHieu>(entity =>
        {
            entity.HasKey(e => e.MaTh).HasName("PK__ThuongHi__27250075B9BC7164");
        });

        modelBuilder.Entity<TrangThaiDonHang>(entity =>
        {
            entity.HasKey(e => e.MaTrangThai).HasName("PK__TrangTha__AADE413853DE2289");

            entity.Property(e => e.MaTrangThai).ValueGeneratedNever();
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CF052D43EC");

            entity.Property(e => e.MaVaiTro).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
