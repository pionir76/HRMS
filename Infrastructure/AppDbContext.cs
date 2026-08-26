using Microsoft.EntityFrameworkCore;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Trend.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Logging.Models;

namespace HRMS.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Compressor> Compressors => Set<Compressor>();
    public DbSet<CompressorChannelSetting> CompressorChannelSettings => Set<CompressorChannelSetting>();
    public DbSet<CompressorSensorCurrent> CompressorSensorCurrents => Set<CompressorSensorCurrent>();
    public DbSet<CompressorMeasurement> CompressorMeasurements => Set<CompressorMeasurement>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 압축기 시드 데이터가 "시설동명+장비명칭" 텍스트로 소속 장비를 가리키기 때문에
        // 이 조합이 유니크해야 안전하게 매칭된다 (Infrastructure/Seed/compressor_seed.sql 참고).
        modelBuilder.Entity<Equipment>()
            .HasIndex(e => new { e.BuildingName, e.Name })
            .IsUnique();

        modelBuilder.Entity<Compressor>()
            .HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(c => c.EquipmentId);

        // 압축기 1대는 항상 CH01~CH07 정확히 7행을 가지므로, 별도 Id 없이
        // (CompressorId, ChannelNo) 자체를 기본키로 쓴다.
        modelBuilder.Entity<CompressorChannelSetting>(b =>
        {
            b.HasKey(x => new { x.CompressorId, x.ChannelNo });
            b.HasOne<Compressor>()
                .WithMany()
                .HasForeignKey(x => x.CompressorId);
        });

        // CompressorSensorCurrent도 같은 이유로 (CompressorId, ChannelNo) 복합키.
        // 채널당 1행만 유지되는 "최신값" 테이블이며 이력이 쌓이지 않는다.
        modelBuilder.Entity<CompressorSensorCurrent>(b =>
        {
            b.HasKey(x => new { x.CompressorId, x.ChannelNo });
            b.HasOne<Compressor>()
                .WithMany()
                .HasForeignKey(x => x.CompressorId);
        });

        // CompressorMeasurement는 압축기 1대당 그 분(MeasuredAt)에 정확히 1행 — 계속 누적되는 이력 테이블.
        modelBuilder.Entity<CompressorMeasurement>(b =>
        {
            b.HasKey(x => new { x.CompressorId, x.MeasuredAt });
            b.HasOne<Compressor>()
                .WithMany()
                .HasForeignKey(x => x.CompressorId);
        });

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}
