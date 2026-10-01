using Microsoft.EntityFrameworkCore;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Trend.Models;
using HRMS.Modules.Auth.Models;
using HRMS.Modules.Logging.Models;
using HRMS.Modules.InspectionReport.Models;
using HRMS.Modules.OperationReport.Models;
using HRMS.Modules.Attachment.Models;
using HRMS.Modules.EquipmentInspectionHistory.Models;
using HRMS.Modules.RepairLog.Models;
using HRMS.Modules.TrainingLog.Models;
using HRMS.Modules.Notice.Models;

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
    public DbSet<InspectionLog> InspectionLogs => Set<InspectionLog>();
    public DbSet<InspectionResult> InspectionResults => Set<InspectionResult>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<OperationItemValue> OperationItemValues => Set<OperationItemValue>();
    public DbSet<OperationReferenceValue> OperationReferenceValues => Set<OperationReferenceValue>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<EquipmentInspectionHistory> EquipmentInspectionHistories => Set<EquipmentInspectionHistory>();
    public DbSet<RepairLog> RepairLogs => Set<RepairLog>();
    public DbSet<TrainingLog> TrainingLogs => Set<TrainingLog>();
    public DbSet<Notice> Notices => Set<Notice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //--------------------------------------------------------------------------------//
        // 압축기 시드 데이터가 "시설동명+장비명칭" 텍스트로 소속 장비를 가리키기 때문에
        // 같은 시설동에 동명 장비를 만들 수 없게 하는 제약이다 (Doc/overview.md 4.1).
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<Equipment>()
            .HasIndex(e => new { e.BuildingName, e.Name })
            .IsUnique();

        modelBuilder.Entity<Compressor>()
            .HasOne<Equipment>()
            .WithMany(e => e.Compressors)
            .HasForeignKey(c => c.EquipmentId);

        //--------------------------------------------------------------------------------//
        // 압축기 1대는 항상 CH01~CH07 정확히 7행을 가지므로, 별도 Id 없이
        // (CompressorId, ChannelNo) 자체를 기본키로 쓴다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<CompressorChannelSetting>(b =>
        {
            b.HasKey(x => new { x.CompressorId, x.ChannelNo });
            b.HasOne<Compressor>()
                .WithMany(c => c.ChannelSettings)
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

        //--------------------------------------------------------------------------------//
        // 점검일지 1건 = 장비 1개 + 주(일요일 날짜) 1개.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<InspectionLog>(b =>
        {
            b.HasIndex(x => new { x.EquipmentId, x.WeekStartDate }).IsUnique();
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId);
        });

        //--------------------------------------------------------------------------------//
        // 점검일지 1건 안에 점검항목(ItemNo)당 정확히 1행 — 별도 Id 없이 복합키로 둔다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<InspectionResult>(b =>
        {
            b.HasKey(x => new { x.LogId, x.ItemNo });
            b.HasOne<InspectionLog>().WithMany(x => x.Results).HasForeignKey(x => x.LogId);
        });

        //--------------------------------------------------------------------------------//
        // 운전일지 1건 = 장비 1개 + 날짜 1개.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<OperationLog>(b =>
        {
            b.HasIndex(x => new { x.EquipmentId, x.Date }).IsUnique();
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId);
        });

        //--------------------------------------------------------------------------------//
        // (LogId, ItemKey, CompressorId) 유일성은 NULL이 섞인 복합키라 DB 제약 대신 앱 코드가
        // 보장한다(OperationReport/README.md 참고) — 그래서 별도 Id를 기본키로 쓴다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<OperationItemValue>(b =>
        {
            b.HasIndex(x => new { x.LogId, x.ItemKey, x.CompressorId });
            b.HasOne<OperationLog>().WithMany(x => x.Items).HasForeignKey(x => x.LogId);
            b.HasOne<Compressor>().WithMany().HasForeignKey(x => x.CompressorId).IsRequired(false);
        });

        modelBuilder.Entity<OperationReferenceValue>(b =>
        {
            b.HasIndex(x => new { x.LogId, x.ItemKey });
            b.HasOne<OperationLog>().WithMany(x => x.References).HasForeignKey(x => x.LogId);
        });

        //--------------------------------------------------------------------------------//
        // 같은 (OwnerType, OwnerId, Slot) 조합은 유일해야 한다(장비 사진 슬롯당 1장 규칙을
        // DB 레벨에서도 강제). Slot이 null인 다건 첨부(게시판 등)는 이 제약에서 제외한다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<Attachment>(b =>
        {
            b.HasIndex(x => new { x.OwnerType, x.OwnerId });
            b.HasIndex(x => new { x.OwnerType, x.OwnerId, x.Slot })
                .IsUnique()
                .HasFilter("\"Slot\" IS NOT NULL");
        });

        //--------------------------------------------------------------------------------//
        // 장비 검사이력 — 장비 1개당 이력이 자유롭게 여러 건 쌓이는 로그성 테이블(주/일 단위
        // 유니크 제약 없음). 목록 조회가 항상 EquipmentId로 걸리므로 인덱스만 둔다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<EquipmentInspectionHistory>(b =>
        {
            b.HasIndex(x => x.EquipmentId);
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId);
        });

        //--------------------------------------------------------------------------------//
        // 수리일지 — 검사이력과 같은 로그성 테이블(장비당 여러 건, 유니크 제약 없음).
        // 결재 데이터는 이 엔티티의 인라인 컬럼(Level1~3)이라 별도 설정이 필요 없다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<RepairLog>(b =>
        {
            b.HasIndex(x => x.EquipmentId);
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId);
        });

        // TrainingLog(교육훈련 일지)와 Notice(공지사항)는 여기 설정이 없다 — 장비에 매달리지
        // 않는 전사 문서라 FK가 없고, 조회도 "전체를 정렬해서 반환"뿐이라 인덱스를 둘 필요가
        // 없다(빠뜨린 게 아님).

        //--------------------------------------------------------------------------------//
        // EventLog — 계속 쌓이기만 하는 대용량 테이블이라 조회 경로에 인덱스가 필요하다
        // (2026-09-28 추가. 그 전에는 PK뿐이라 아래 두 쿼리가 풀스캔 + 정렬이었다).
        //   - CreatedAt: 실시간 피드(GET /api/events?since=)가 주기적으로 호출한다
        //   - (EquipmentId, CreatedAt): 자료조회의 장비별 하루치 이벤트(GET /api/equipments/{id}/events)
        // EquipmentId/CompressorId에 FK는 의도적으로 두지 않는다 — 장비가 철거되어도 그 장비의
        // 경보/통신 이력은 감사 기록으로 남아야 하기 때문이다.
        //--------------------------------------------------------------------------------//
        modelBuilder.Entity<EventLog>(b =>
        {
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => new { x.EquipmentId, x.CreatedAt });
        });
    }
}
