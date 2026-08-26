using HRMS.Modules.Alarm.Models;
using HRMS.Modules.Equipment.Models;

namespace HRMS.Modules.Alarm;

// 채널 하나의 경보 상태를 판정한다. CompressorPollingService가 값을 갱신한 직후 채널마다 호출한다.
// 상태 전이 (overview.md 4.6, 단 경보확인은 시스템 사양에 없어서 제외):
//   정상 --(범위 밖)--> 경보발생대기 --(AlarmDelaySeconds 경과)--> 경보발생
//   경보발생 --(범위 안)--> 정상복귀대기 --(AlarmClearDelaySeconds 경과)--> 정상
// 경보발생 상태에서는 사용자 확인 없이 값이 정상 범위로 돌아올 때까지 그대로 유지된다.
// 히스테리시스는 두지 않는다(이미 필드 자체를 뺀 상태) — 경계값 근처에서는 상태가 자주 바뀔 수 있다.
public static class AlarmEvaluator
{
    public static void Evaluate(CompressorSensorCurrent current, CompressorChannelSetting setting, DateTimeOffset now)
    {
        if (!setting.Enabled || !setting.AlarmEnabled)
        {
            current.AlarmStatus = AlarmStatus.경보비활성화;
            current.PendingSince = null;
            return;
        }

        bool inRange = current.Value >= (setting.LowerLimit ?? decimal.MinValue)
                    && current.Value <= (setting.UpperLimit ?? decimal.MaxValue);

        var delay = TimeSpan.FromSeconds(setting.AlarmDelaySeconds ?? 0);
        var clearDelay = TimeSpan.FromSeconds(setting.AlarmClearDelaySeconds ?? 0);

        switch (current.AlarmStatus)
        {
            case AlarmStatus.정상:
            case AlarmStatus.경보비활성화: // 방금 다시 활성화된 경우, 정상 판정부터 새로 시작
                if (!inRange)
                {
                    current.AlarmStatus = AlarmStatus.경보발생대기;
                    current.PendingSince = now;
                }
                else
                {
                    current.AlarmStatus = AlarmStatus.정상;
                    current.PendingSince = null;
                }
                break;

            case AlarmStatus.경보발생대기:
                if (!inRange)
                {
                    if (current.PendingSince is { } since && now - since >= delay)
                        current.AlarmStatus = AlarmStatus.경보발생;
                    // 지연시간 안 지났으면 계속 대기
                }
                else
                {
                    current.AlarmStatus = AlarmStatus.정상;
                    current.PendingSince = null;
                }
                break;

            case AlarmStatus.경보발생:
                if (inRange)
                {
                    current.AlarmStatus = AlarmStatus.정상복귀대기;
                    current.PendingSince = now;
                }
                // 범위 밖이면 확인 절차 없이 경보발생 그대로 유지
                break;

            case AlarmStatus.정상복귀대기:
                if (inRange)
                {
                    if (current.PendingSince is { } since && now - since >= clearDelay)
                    {
                        current.AlarmStatus = AlarmStatus.정상;
                        current.PendingSince = null;
                    }
                    // 해제 지연시간 안 지났으면 계속 대기
                }
                else
                {
                    // 복귀 대기 중 다시 범위를 벗어나면 즉시 경보발생으로 되돌아간다.
                    current.AlarmStatus = AlarmStatus.경보발생;
                    current.PendingSince = null;
                }
                break;
        }
    }
}
