namespace HRMS.Modules.Equipment.Models;

public class Equipment
{
    public int Id { get; set; }

    public required string Region { get; set; }
    public required string BuildingName { get; set; }
    public required string Name { get; set; }
    public EquipmentStatus Status { get; set; }

    public string? ModelName { get; set; }
    public decimal? RatedPower { get; set; }
    public decimal? RatedVoltage { get; set; }
    public string? CompressorType { get; set; }
    public decimal? CompressorCapacity { get; set; }
    public string? CoolingTowerType { get; set; }
    public decimal? CoolingTowerCapacity { get; set; }
    public decimal? LegalRefrigerationCapacity { get; set; }
    public decimal? UsRefrigerationCapacity { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? InstallDate { get; set; }
    public string? PermitNumber { get; set; }
    public string? Refrigerant { get; set; }
    public decimal? ChargeAmount { get; set; }
    public string? KgsManagementNumber { get; set; }
    public string? Manufacturer { get; set; }
    public decimal? HighPressureTestPressure { get; set; }
    public decimal? LowPressureTestPressure { get; set; }
    public decimal? OverPressureCutoff { get; set; }
    public decimal? SafetyValveSetPointCondenser { get; set; }
    public decimal? SafetyValveSetPointEvaporator { get; set; }

    public decimal? RunningCurrentThreshold { get; set; }
}
