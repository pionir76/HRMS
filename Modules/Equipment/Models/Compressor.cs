using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;

namespace HRMS.Modules.Equipment.Models;

public class Compressor
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public CommunicationStatus CommunicationStatus { get; set; }
    public AlarmStatus AlarmStatus { get; set; }
}
