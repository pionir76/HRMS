using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoredPath = table.Column<string>(type: "text", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "integer", nullable: false),
                    UploadedByUserName = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Region = table.Column<string>(type: "text", nullable: false),
                    BuildingNumber = table.Column<string>(type: "text", nullable: true),
                    BuildingName = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    PermitNumber = table.Column<string>(type: "text", nullable: true),
                    ManagementNumber = table.Column<string>(type: "text", nullable: true),
                    LegalRefrigerationCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    UsRefrigerationCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    Refrigerant = table.Column<string>(type: "text", nullable: true),
                    ChargeAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CompressorManufacturer = table.Column<string>(type: "text", nullable: true),
                    CompressorType = table.Column<string>(type: "text", nullable: true),
                    CompressorCount = table.Column<int>(type: "integer", nullable: true),
                    CompressorCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    CoolingTowerManufacturer = table.Column<string>(type: "text", nullable: true),
                    CoolingTowerType = table.Column<string>(type: "text", nullable: true),
                    CoolingTowerCount = table.Column<int>(type: "integer", nullable: true),
                    CoolingTowerCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                    CondenserType = table.Column<string>(type: "text", nullable: true),
                    EvaporatorType = table.Column<string>(type: "text", nullable: true),
                    DesignPressure = table.Column<decimal>(type: "numeric", nullable: true),
                    OverPressureCutoff = table.Column<decimal>(type: "numeric", nullable: true),
                    RatedVoltage = table.Column<decimal>(type: "numeric", nullable: true),
                    RatedCurrent = table.Column<decimal>(type: "numeric", nullable: true),
                    SafetyValve = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    HasCoolingWater = table.Column<bool>(type: "boolean", nullable: false),
                    CoolingWaterInletMin = table.Column<decimal>(type: "numeric", nullable: true),
                    CoolingWaterInletMax = table.Column<decimal>(type: "numeric", nullable: true),
                    CoolingWaterOutletMin = table.Column<decimal>(type: "numeric", nullable: true),
                    CoolingWaterOutletMax = table.Column<decimal>(type: "numeric", nullable: true),
                    HasBrine = table.Column<bool>(type: "boolean", nullable: false),
                    BrineInletMin = table.Column<decimal>(type: "numeric", nullable: true),
                    BrineInletMax = table.Column<decimal>(type: "numeric", nullable: true),
                    BrineOutletMin = table.Column<decimal>(type: "numeric", nullable: true),
                    BrineOutletMax = table.Column<decimal>(type: "numeric", nullable: true),
                    HasVoltage = table.Column<bool>(type: "boolean", nullable: false),
                    VoltageMin = table.Column<decimal>(type: "numeric", nullable: true),
                    VoltageMax = table.Column<decimal>(type: "numeric", nullable: true),
                    RunningCurrentThreshold = table.Column<short>(type: "smallint", nullable: true),
                    IsRunning = table.Column<bool>(type: "boolean", nullable: false),
                    AlarmStatus = table.Column<int>(type: "integer", nullable: false),
                    CommunicationStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    EquipmentId = table.Column<int>(type: "integer", nullable: true),
                    CompressorId = table.Column<int>(type: "integer", nullable: true),
                    ChannelNo = table.Column<int>(type: "integer", nullable: true),
                    Value = table.Column<short>(type: "smallint", nullable: true),
                    LowerLimit = table.Column<short>(type: "smallint", nullable: true),
                    UpperLimit = table.Column<short>(type: "smallint", nullable: true),
                    DecimalPlaces = table.Column<int>(type: "integer", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Instructor = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Attendees = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level1ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level1ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level1ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level2ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level2ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level2ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level3ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level3ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level3ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: true),
                    Position = table.Column<string>(type: "text", nullable: true),
                    Phone1 = table.Column<string>(type: "text", nullable: true),
                    Phone2 = table.Column<string>(type: "text", nullable: true),
                    BackupPersonName = table.Column<string>(type: "text", nullable: true),
                    IsAppointed = table.Column<bool>(type: "boolean", nullable: false),
                    LegalTrainingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextTrainingDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Compressors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    SequenceNo = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    MacAddress = table.Column<string>(type: "text", nullable: true),
                    CommunicationStatus = table.Column<int>(type: "integer", nullable: false),
                    AlarmStatus = table.Column<int>(type: "integer", nullable: false),
                    DisconnectedSince = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HasCommunicationAlarm = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compressors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compressors_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentInspectionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentInspectionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentInspectionHistories_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Opinion = table.Column<string>(type: "text", nullable: true),
                    Level1ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level1ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level1ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level2ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level2ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level2ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level3ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level3ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level3ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionLogs_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Level1ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level1ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level1ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level2ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level2ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level2ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level3ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level3ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level3ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationLogs_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    PerformedBy = table.Column<string>(type: "text", nullable: true),
                    Target = table.Column<string>(type: "text", nullable: true),
                    StateBefore = table.Column<string>(type: "text", nullable: true),
                    StateAfter = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: true),
                    FailureCause = table.Column<string>(type: "text", nullable: true),
                    PreventiveAction = table.Column<string>(type: "text", nullable: true),
                    Opinion = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserName = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level1ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level1ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level1ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level2ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level2ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level2ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Level3ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Level3ApproverName = table.Column<string>(type: "text", nullable: true),
                    Level3ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairLogs_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEquipments",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEquipments", x => new { x.UserId, x.EquipmentId });
                    table.ForeignKey(
                        name: "FK_UserEquipments_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEquipments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompressorChannelSettings",
                columns: table => new
                {
                    CompressorId = table.Column<int>(type: "integer", nullable: false),
                    ChannelNo = table.Column<int>(type: "integer", nullable: false),
                    ChannelName = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    RegisterAddress = table.Column<int>(type: "integer", nullable: true),
                    LowerLimit = table.Column<short>(type: "smallint", nullable: true),
                    UpperLimit = table.Column<short>(type: "smallint", nullable: true),
                    AlarmEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AlarmDelaySeconds = table.Column<int>(type: "integer", nullable: true),
                    AlarmClearDelaySeconds = table.Column<int>(type: "integer", nullable: true),
                    DecimalPlaces = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompressorChannelSettings", x => new { x.CompressorId, x.ChannelNo });
                    table.ForeignKey(
                        name: "FK_CompressorChannelSettings_Compressors_CompressorId",
                        column: x => x.CompressorId,
                        principalTable: "Compressors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompressorMeasurements",
                columns: table => new
                {
                    CompressorId = table.Column<int>(type: "integer", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Ch01 = table.Column<short>(type: "smallint", nullable: true),
                    Ch02 = table.Column<short>(type: "smallint", nullable: true),
                    Ch03 = table.Column<short>(type: "smallint", nullable: true),
                    Ch04 = table.Column<short>(type: "smallint", nullable: true),
                    Ch05 = table.Column<short>(type: "smallint", nullable: true),
                    Ch06 = table.Column<short>(type: "smallint", nullable: true),
                    Ch07 = table.Column<short>(type: "smallint", nullable: true),
                    IsRunning = table.Column<bool>(type: "boolean", nullable: false),
                    HasAlarm = table.Column<bool>(type: "boolean", nullable: false),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompressorMeasurements", x => new { x.CompressorId, x.MeasuredAt });
                    table.ForeignKey(
                        name: "FK_CompressorMeasurements_Compressors_CompressorId",
                        column: x => x.CompressorId,
                        principalTable: "Compressors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompressorSensorCurrents",
                columns: table => new
                {
                    CompressorId = table.Column<int>(type: "integer", nullable: false),
                    ChannelNo = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<short>(type: "smallint", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AlarmStatus = table.Column<int>(type: "integer", nullable: false),
                    PendingSince = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompressorSensorCurrents", x => new { x.CompressorId, x.ChannelNo });
                    table.ForeignKey(
                        name: "FK_CompressorSensorCurrents_Compressors_CompressorId",
                        column: x => x.CompressorId,
                        principalTable: "Compressors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionResults",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "integer", nullable: false),
                    ItemNo = table.Column<string>(type: "text", nullable: false),
                    Sun = table.Column<string>(type: "text", nullable: true),
                    Mon = table.Column<string>(type: "text", nullable: true),
                    Tue = table.Column<string>(type: "text", nullable: true),
                    Wed = table.Column<string>(type: "text", nullable: true),
                    Thu = table.Column<string>(type: "text", nullable: true),
                    Fri = table.Column<string>(type: "text", nullable: true),
                    Sat = table.Column<string>(type: "text", nullable: true),
                    Memo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionResults", x => new { x.LogId, x.ItemNo });
                    table.ForeignKey(
                        name: "FK_InspectionResults_InspectionLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "InspectionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationItemValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "text", nullable: false),
                    CompressorId = table.Column<int>(type: "integer", nullable: true),
                    Time0900 = table.Column<string>(type: "text", nullable: true),
                    Time1300 = table.Column<string>(type: "text", nullable: true),
                    Time1600 = table.Column<string>(type: "text", nullable: true),
                    Time2100 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationItemValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationItemValues_Compressors_CompressorId",
                        column: x => x.CompressorId,
                        principalTable: "Compressors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperationItemValues_OperationLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "OperationLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationReferenceValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "text", nullable: false),
                    ReferenceText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationReferenceValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationReferenceValues_OperationLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "OperationLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_OwnerType_OwnerId",
                table: "Attachments",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_OwnerType_OwnerId_Slot",
                table: "Attachments",
                columns: new[] { "OwnerType", "OwnerId", "Slot" },
                unique: true,
                filter: "\"Slot\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compressors_EquipmentId",
                table: "Compressors",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentInspectionHistories_EquipmentId",
                table: "EquipmentInspectionHistories",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_BuildingName_Name",
                table: "Equipments",
                columns: new[] { "BuildingName", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionLogs_EquipmentId_WeekStartDate",
                table: "InspectionLogs",
                columns: new[] { "EquipmentId", "WeekStartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationItemValues_CompressorId",
                table: "OperationItemValues",
                column: "CompressorId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationItemValues_LogId_ItemKey_CompressorId",
                table: "OperationItemValues",
                columns: new[] { "LogId", "ItemKey", "CompressorId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_EquipmentId_Date",
                table: "OperationLogs",
                columns: new[] { "EquipmentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationReferenceValues_LogId_ItemKey",
                table: "OperationReferenceValues",
                columns: new[] { "LogId", "ItemKey" });

            migrationBuilder.CreateIndex(
                name: "IX_RepairLogs_EquipmentId",
                table: "RepairLogs",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEquipments_EquipmentId",
                table: "UserEquipments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "CompressorChannelSettings");

            migrationBuilder.DropTable(
                name: "CompressorMeasurements");

            migrationBuilder.DropTable(
                name: "CompressorSensorCurrents");

            migrationBuilder.DropTable(
                name: "EquipmentInspectionHistories");

            migrationBuilder.DropTable(
                name: "EventLogs");

            migrationBuilder.DropTable(
                name: "InspectionResults");

            migrationBuilder.DropTable(
                name: "Notices");

            migrationBuilder.DropTable(
                name: "OperationItemValues");

            migrationBuilder.DropTable(
                name: "OperationReferenceValues");

            migrationBuilder.DropTable(
                name: "RepairLogs");

            migrationBuilder.DropTable(
                name: "TrainingLogs");

            migrationBuilder.DropTable(
                name: "UserEquipments");

            migrationBuilder.DropTable(
                name: "InspectionLogs");

            migrationBuilder.DropTable(
                name: "Compressors");

            migrationBuilder.DropTable(
                name: "OperationLogs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Equipments");
        }
    }
}
