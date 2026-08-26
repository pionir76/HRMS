namespace HRMS.Modules.Alarm.Models;

public enum AlarmStatus
{
    정상,
    경보발생대기,
    경보발생,
    경보확인,
    정상복귀대기,
    경보해제,
    경보비활성화
}
