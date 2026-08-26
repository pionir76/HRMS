namespace HRMS.Modules.Operation.Models;

// 장비의 운전/정지 상태 (overview.md 4.7). "정지"가 0번이라, 운전전류 임계값이
// 아직 설정 안 된 장비는 판정 전 기본값도 안전하게 정지로 나온다.
public enum RunningStatus
{
    정지,
    운전
}
