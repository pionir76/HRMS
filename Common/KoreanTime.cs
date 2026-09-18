namespace HRMS.Common;

//--------------------------------------------------------------------------------//
// 한국 시간(KST) 기준 시각 계산용 공용 값. 이 시스템의 "하루"는 전부 KST 기준이다
// (트렌드/이벤트 조회의 date 파라미터, 운전일지 트리거 시각, 통신 단절 시 일자 경계 초기화 등).
// 예전에는 서비스·컨트롤러 6곳이 각자 `KstOffset = TimeSpan.FromHours(9)`를 따로 선언하고
// 있어서 여기로 모았다. 서머타임이 없는 고정 오프셋이라 TimeZoneInfo 대신 상수로 충분하다.
//--------------------------------------------------------------------------------//
public static class KoreanTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(9);
}
