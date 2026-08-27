using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Communication.Models;

namespace HRMS.Modules.Equipment.Models;

// 압축기. 실제 TLC 장비와 1:1로 통신하는 단위다.
// 압축기명/포트/타임아웃 등은 의도적으로 두지 않았다 — 수집 주기·프로토콜·포트는 시스템 공통값이고
// (PcLinkClient.Port 등), 응답 제한시간·재접속 주기 같은 세부 통신 설정도 아직은 필요 없어 뺐다.
// 요구사항 정의 당시엔 더 많은 필드가 있었지만 "간단하게 가자"는 방침으로 이 5개만 남겼다.
public class Compressor
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }
    public string? IpAddress { get; set; } // 원본 자산 목록(Doc/CompList.md)에 IP가 없는 압축기가 있어 nullable
    public string? MacAddress { get; set; }
    public CommunicationStatus CommunicationStatus { get; set; } // CompressorPollingService가 3초마다 갱신
    public AlarmStatus AlarmStatus { get; set; } // 소속 채널 7개 중 가장 심각한 상태로 EquipmentStatusAggregator가 3초마다 갱신
}
