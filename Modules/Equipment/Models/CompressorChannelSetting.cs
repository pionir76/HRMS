namespace HRMS.Modules.Equipment.Models;

public class CompressorChannelSetting
{
    public int CompressorId { get; set; }
    public ChannelNo ChannelNo { get; set; }

    public bool Enabled { get; set; } = true;
    public decimal? LowerLimit { get; set; }
    public decimal? UpperLimit { get; set; }
    public bool AlarmEnabled { get; set; } = true;
    public int? AlarmDelaySeconds { get; set; }
    public int? AlarmClearDelaySeconds { get; set; }
}
