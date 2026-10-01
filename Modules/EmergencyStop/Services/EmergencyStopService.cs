using System.Collections.Concurrent;
using HRMS.Infrastructure;
using HRMS.Modules.Communication;
using HRMS.Modules.Communication.Protocol;
using HRMS.Modules.Equipment.Models;
using HRMS.Modules.Logging;
using HRMS.Modules.Logging.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Modules.EmergencyStop.Services;

//--------------------------------------------------------------------------------//
// 비상정지 실행/해제. 이 시스템에서 장비에 무언가를 쓰는 유일한 동작이다(TLC 프로토콜이
// 제공하는 쓰기 기능 자체가 DO 접점 출력 하나뿐 — overview.md 4.8).
//
// 절차(overview.md 4.8의 처리 절차를 그대로 따른다):
//   대상 장비 확인 → 중복 실행 차단 → D1805 쓰기 → 응답 확인 → D1805 재확인 → 이력 기록
// 권한 확인과 사용자 재확인은 이 서비스가 아니라 컨트롤러/프론트 몫이다.
//
// **장비 단위 기능이고, 1번 압축기만 본다**(사양 확정 2026-09-29). 모든 압축기에 비상정지
// 회로가 붙어 있지만, 시스템 단순화를 위해 쓰기도 상태 판정도 그 장비의 1번 압축기
// (SequenceNo 최소)가 붙은 TLC 하나로만 한다. 2번 이후 압축기는 비상정지에 관해 무시한다.
// LG 냉동기도 예외 없이 같다.
//--------------------------------------------------------------------------------//
public class EmergencyStopService(AppDbContext db, IConfiguration configuration, ILogger<EmergencyStopService> logger)
{
    //--------------------------------------------------------------------------------//
    // TLC(IP)별 실행 잠금. "쓰기 명령의 중복 실행을 방지해야 한다"(overview.md 4.8)는 요구사항이며,
    // 두 사용자가 동시에 눌렀을 때 같은 레지스터로 명령이 겹쳐 나가는 것을 막는다.
    //
    // 장비가 아니라 IP를 키로 쓰는 이유: LG 배터리 #1~#7은 1번 압축기가 모두 같은 TLC
    // (10.90.87.217)에 붙어 있어 사실상 D1805 하나를 공유한다. 장비별로 잠그면 "#1 정지"와
    // "#2 해제"가 같은 레지스터에 동시에 쓰일 수 있다. 일반 장비는 1번 압축기의 IP가 곧
    // 장비 하나에 대응하므로 장비별 잠금과 똑같이 동작한다.
    //
    // 폴링 읽기와는 직렬화하지 않는다. 폴링은 요청마다 새 TCP 연결을 열고, 실장비 시험에서
    // TLC 1대에 42개 동시 접속도 견뎠다(2026-09-28). 읽기까지 이 잠금에 묶으면 LG 냉동기
    // 42대가 순차 처리되어 폴링 주기(3초)를 못 지킨다 — 얻는 것보다 잃는 게 크다.
    //--------------------------------------------------------------------------------//
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TlcLocks = new();

    public async Task<EmergencyStopResult> ExecuteAsync(
        int equipmentId, bool stop, string? username, CancellationToken cancellationToken = default)
    {
        var equipment = await db.Equipments.FindAsync([equipmentId], cancellationToken);
        if (equipment is null)
            return EmergencyStopResult.NotFound();

        //--------------------------------------------------------------------------------//
        // 명령을 보낼 압축기 = 그 장비의 1번(SequenceNo 최소). 1번에 IP가 없으면 2번 이후로
        // 넘어가지 않고 실패로 끝낸다 — "무조건 1번 압축기 기준"이 사양이고, 상태 판정도 1번만
        // 보므로 다른 압축기로 명령을 보내면 화면에 반영되지 않는다.
        //--------------------------------------------------------------------------------//
        var target = await db.Compressors
            .Where(c => c.EquipmentId == equipmentId)
            .OrderBy(c => c.SequenceNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (target is null)
            return EmergencyStopResult.Fail("이 장비에는 압축기가 없어 명령을 보낼 수 없습니다.", equipment.IsEmergencyStopped);

        if (target.IpAddress is null)
            return EmergencyStopResult.Fail("이 장비의 1번 압축기에 통신 주소(IP)가 설정되지 않아 명령을 보낼 수 없습니다.", equipment.IsEmergencyStopped);

        var gate = TlcLocks.GetOrAdd(target.IpAddress, _ => new SemaphoreSlim(1, 1));
        if (!await gate.WaitAsync(0, cancellationToken))
            return EmergencyStopResult.Fail("같은 TLC에 대한 비상정지 명령이 이미 처리 중입니다. 잠시 후 다시 시도하세요.", equipment.IsEmergencyStopped);

        try
        {
            return await SendAsync(equipment, target, stop, username, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<EmergencyStopResult> SendAsync(
        Equipment.Models.Equipment equipment, Compressor target, bool stop, string? username,
        CancellationToken cancellationToken)
    {
        string action = stop ? "비상정지" : "비상정지 해제";
        int timeoutMs = configuration.GetValue("Communication:TimeoutMs", PcLinkClient.DefaultTimeoutMs);

        //--------------------------------------------------------------------------------//
        // 테스트 모드에서는 TLC가 없으므로 실제 쓰기 없이 상태만 바꾼다. 폴링도 테스트 모드에서는
        // 비상정지 상태를 덮어쓰지 않으므로(CompressorPollingService 참고) 화면 검증이 가능하다.
        //--------------------------------------------------------------------------------//
        // RealDeviceIps에 있는 TLC라면 테스트 모드여도 실제로 쓴다(CommunicationMode) — 폴링도
        // 그 압축기만 실제로 읽으므로, 명령과 화면이 같은 장비를 보게 된다.
        if (CommunicationMode.IsSimulated(configuration, target.IpAddress))
        {
            await ApplyAsync(equipment, target, stop, cancellationToken);
            await LogAsync(equipment, target, action, username, "(테스트 모드 — 실제 명령 전송 안 함)", true, cancellationToken);
            return EmergencyStopResult.Success(stop, true, $"{action} 처리했습니다. (테스트 모드)");
        }

        var write = await PcLinkClient.WriteRegisterAsync(
            target.IpAddress!, PcLinkClient.DoOutputRegister, stop ? (ushort)1 : (ushort)0, timeoutMs, cancellationToken);

        if (!write.Ok)
        {
            logger.LogWarning("장비 {EquipmentId} {Action} 실패: {Reason}", equipment.Id, action, write.Reason);
            await LogAsync(equipment, target, action, username,
                $"명령 {write.SentCommand} / 실패({write.Reason})", false, cancellationToken);

            return EmergencyStopResult.Fail($"{action} 명령 전송에 실패했습니다. (사유: {write.Reason})", equipment.IsEmergencyStopped);
        }

        //--------------------------------------------------------------------------------//
        // "명령 수행 후 실제로 바뀌었는지 확인"(overview.md 4.8). D1805를 되읽어 판정한다.
        // 되읽기가 끝내 안 맞아도 명령 자체는 성공이므로 실패로 만들지 않는다 — 폴링이 3초
        // 안에 다시 읽어 상태를 맞춘다. 확인 여부는 confirmed로 구분해서 돌려준다.
        //--------------------------------------------------------------------------------//
        bool confirmed = await VerifyAsync(target.IpAddress!, stop, timeoutMs, cancellationToken);

        //--------------------------------------------------------------------------------//
        // 확인이 안 됐어도 명령대로 반영해 둔다. 다음 폴링이 장비가 실제로 보고하는 값으로
        // 덮어쓰므로 오래 어긋나 있지 않는다.
        //--------------------------------------------------------------------------------//
        await ApplyAsync(equipment, target, stop, cancellationToken);

        await LogAsync(equipment, target, action, username,
            $"명령 {write.SentCommand} / 응답 OK / 상태확인 {(confirmed ? "완료" : "미확인")}", true, cancellationToken);

        return EmergencyStopResult.Success(stop, confirmed, confirmed
            ? $"{action} 명령을 전송하고 장비 상태를 확인했습니다."
            : $"{action} 명령은 전송됐지만 장비 상태 확인에 실패했습니다. 잠시 후 현황을 다시 확인하세요.");
    }

    //--------------------------------------------------------------------------------//
    // TLC는 쓰기에 OK를 먼저 돌려주고, 레지스터에 실제로 반영하는 건 0.4~0.8초 뒤다
    // (2026-09-29 샘플 TLC 실측: 400ms까지는 예전 값, 800ms에는 새 값). 그래서 OK 직후
    // 한 번만 읽으면 항상 예전 값이 나와 "미확인"이 된다. 250ms 간격으로 최대 2초까지
    // 다시 읽고, 기대한 값이 보이면 바로 끝낸다(보통 3~4번째에 확인된다).
    //--------------------------------------------------------------------------------//
    private static readonly TimeSpan VerifyInterval = TimeSpan.FromMilliseconds(250);
    private const int VerifyAttempts = 8;

    private static async Task<bool> VerifyAsync(string ipAddress, bool stop, int timeoutMs, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < VerifyAttempts; attempt++)
        {
            await Task.Delay(VerifyInterval, cancellationToken);

            var read = await PcLinkClient.ReadEmergencyStopAsync(ipAddress, timeoutMs, cancellationToken);
            if (read.Ok && PcLinkClient.IsEmergencyStopped(read.Values[0]) == stop)
                return true;
        }

        return false;
    }

    //--------------------------------------------------------------------------------//
    // 명령 결과를 DB에 즉시 반영한다. 폴링을 기다리면 최대 3초 동안 화면이 예전 상태를 보여준다.
    // 1번 압축기와 장비만 바꾼다 — 집계(EquipmentStatusAggregator)가 1번 압축기 값만 보기 때문이다.
    //--------------------------------------------------------------------------------//
    private async Task ApplyAsync(
        Equipment.Models.Equipment equipment, Compressor target, bool stopped, CancellationToken cancellationToken)
    {
        target.IsEmergencyStopped = stopped;
        equipment.IsEmergencyStopped = stopped;
        await db.SaveChangesAsync(cancellationToken);
    }

    //--------------------------------------------------------------------------------//
    // 이력은 EventLogs에 남긴다(사용자 결정 2026-09-29 — 전용 테이블은 두지 않는다).
    // 요청 시각은 CreatedAt, 사용자는 Username, 대상은 EquipmentId/CompressorId, 전송한 명령과
    // 응답·상태 확인 결과는 Message에 담는다.
    //--------------------------------------------------------------------------------//
    private async Task LogAsync(
        Equipment.Models.Equipment equipment, Compressor target, string action, string? username,
        string detail, bool succeeded, CancellationToken cancellationToken)
    {
        string outcome = succeeded ? "수행했습니다" : "시도했으나 실패했습니다";
        await EventLogger.LogAsync(db, EventLogCategory.EmergencyStop,
            $"{username}님이 {equipment.BuildingName} {equipment.Name}({target.SequenceNo}번 압축기 {target.IpAddress})에 " +
            $"{action} 명령을 {outcome}. {detail}",
            username, equipment.Id, target.Id);
    }
}

//--------------------------------------------------------------------------------//
// 실행 결과. Confirmed는 명령 후 D1805 되읽기로 실제 상태까지 확인했는지다(명령 성공과 별개).
//--------------------------------------------------------------------------------//
public record EmergencyStopResult(bool Succeeded, bool IsEmergencyStopped, bool Confirmed, string Message)
{
    // 장비 자체가 없는 경우. 명령 실패(409)와 달리 404로 내보내려고 따로 구분한다.
    public bool EquipmentNotFound { get; private init; }

    public static EmergencyStopResult NotFound() =>
        new(false, false, false, "존재하지 않는 장비입니다.") { EquipmentNotFound = true };

    // 실패해도 현재 상태(currentState)는 그대로 알려준다 — 정지된 장비의 해제가 실패했는데
    // 화면이 "해제됨"으로 보이면 안 된다.
    public static EmergencyStopResult Fail(string message, bool currentState = false) =>
        new(false, currentState, false, message);

    public static EmergencyStopResult Success(bool stopped, bool confirmed, string message) =>
        new(true, stopped, confirmed, message);
}
