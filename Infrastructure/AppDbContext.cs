using Microsoft.EntityFrameworkCore;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Trend.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Logging.Models;

namespace HRMS.Infrastructure;

//--------------------------------------------------------------------------------//
// AppDbContext는 이 프로그램과 PostgreSQL 데이터베이스를 연결해주는 다리 역할을 하는 클래스.
// EF Core(Entity Framework Core)라는 라이브러리의 핵심 개념인 DbContext를 프로젝트에 맞게 
// 상속받아 만든다. DbContext는 데이터베이스와의 연결, 쿼리, 트랜잭션 등을 관리한다.
//--------------------------------------------------------------------------------//
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    //--------------------------------------------------------------------------------//
    // DbSet<T>는 테이블 하나에 대응. T는 테이블의 한 행(row)에 대응하는 C# 객체 타입.
    // public DbSet<Equipment> Equipments => Set<Equipment>();
    // Ex. var list = await db.Equipments.ToListAsync(); // Equipments 테이블 전체 조회
    //--------------------------------------------------------------------------------//
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Compressor> Compressors => Set<Compressor>();
    public DbSet<CompressorChannelSetting> CompressorChannelSettings => Set<CompressorChannelSetting>();
    public DbSet<CompressorSensorCurrent> CompressorSensorCurrents => Set<CompressorSensorCurrent>();
    public DbSet<CompressorMeasurement> CompressorMeasurements => Set<CompressorMeasurement>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserEquipment> UserEquipments => Set<UserEquipment>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //--------------------------------------------------------------------------------//
        // 압축기 시드 데이터가 "시설동명+장비명칭" 텍스트로 소속 장비를 가리키기 때문에
        // 이 조합이 유니크해야 안전하게 매칭된다 (Infrastructure/Seed/compressor_seed.sql 참고).
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<Equipment>()
            .HasIndex(e => new { e.BuildingName, e.Name })
            .IsUnique();

        modelBuilder.Entity<Compressor>()
            .HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(c => c.EquipmentId);

        //--------------------------------------------------------------------------------//
        // 압축기 1대는 항상 CH01~CH07 정확히 7행을 가지므로, 별도 Id 없이
        // (CompressorId, ChannelNo) 자체를 기본키로 쓴다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<CompressorChannelSetting>(b =>
        {
            b.HasKey(x => new { x.CompressorId, x.ChannelNo });
            b.HasOne<Compressor>()
                .WithMany()
                .HasForeignKey(x => x.CompressorId);
        });

        //--------------------------------------------------------------------------------//
        // CompressorSensorCurrent도 같은 이유로 (CompressorId, ChannelNo) 복합키.
        // 채널당 1행만 유지되는 "최신값" 테이블이며 이력이 쌓이지 않는다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<CompressorSensorCurrent>(b =>
        {
            b.HasKey(x => new { x.CompressorId, x.ChannelNo });
            b.HasOne<Compressor>()
                .WithMany()
                .HasForeignKey(x => x.CompressorId);
        });

        //--------------------------------------------------------------------------------//
        // CompressorMeasurement는 압축기 1대당 그 분(MeasuredAt)에 정확히 1행 — 계속 누적되는 이력 테이블.
        //--------------------------------------------------------------------------------//
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

        //--------------------------------------------------------------------------------//
        // 사용자-담당장비 다대다 관계. 별도 Id 없이 (UserId, EquipmentId) 자체를 기본키로 쓴다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<UserEquipment>(b =>
        {
            b.HasKey(x => new { x.UserId, x.EquipmentId });
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId);
        });
    }
}
