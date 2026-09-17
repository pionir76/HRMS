# Communication 모듈

TCP 통신 모듈 + 프로토콜 모듈.

- 압축기별 TCP 연결 관리 (연결/해제/재사용)
- 전용 프로토콜 파서 (요청 패킷 생성, 응답 패킷 파싱, 비정상 패킷 검출)
- 응답 타임아웃 처리 및 자동 재접속
- 압축기별 통신 상태 관리 (연결됨/끊김/재접속중)
- 압축기 통신 장애가 다른 압축기 수집에 영향을 주지 않도록 격리

## 내부 구성

- `PcLinkClient.cs` — PC-Link 프로토콜 송수신/파싱(체크섬 검증, 16비트 2의 보수). 연결을 유지·재사용하지 않고 폴링 사이클마다 새로 연결하고 끊는다
- `CompressorPollingService.cs` — 3초 주기 폴링 백그라운드 서비스. 수집 대상은 **장비 상태가 `운영`인 장비의 압축기만**(`CollectionTargets`, 2026-09-17 사양 확정 — overview.md 4.1). 사이클이 끝까지 성공할 때마다 `LastCycleCompletedAt`을 갱신한다(`GET /api/system/status`의 수집 서비스 점검용). 채널값 갱신, 통신 상태 관리, 경보 판정(`Modules/Alarm`) 호출, 장비 단위 집계(`EquipmentStatusAggregator`) 트리거

## 통신 단절 시 채널값 처리 (사양 확정 2026-09-16)

**1) 끊긴 동안에는 직전 성공값을 그대로 유지한다.**
폴링이 실패하면 `UpdateCurrentValuesAsync`를 아예 호출하지 않으므로(`results.Where(r => r.Ok)`) `CompressorSensorCurrent`가 갱신되지 않고, 마지막 정상 수신값이 남는다. 실패 시 갱신되는 것은 통신 관련 필드뿐이다 — `CommunicationStatus`(연결됨/끊김/재접속중), `DisconnectedSince`, `HasCommunicationAlarm`.

그 결과 **끊김 구간의 채널값은 `null`이 아니다.** 1분 단위 트렌드(`TrendRecordingService`)도 최신값 테이블을 그대로 복사하므로 같은 값이 계속 기록되고, 그래프에서는 평평한 직선으로 보인다. **통신 여부는 값이 아니라 `IsConnected`로 판단해야 한다.**

값이 `null`이 되는 경우는 **그 압축기의 최신값 행이 아예 없을 때**(= 한 번도 통신에 성공한 적 없는 압축기)뿐이다.

**2) 하루가 시작되는데 여전히 끊긴 상태면 전 채널을 0으로 초기화한다.**
`ResetStaleValuesAtDayStartAsync`가 처리한다. 전날 값을 다음 날까지 끌고 가면 "어제 값으로 오늘 하루가 채워지는" 문제가 생기기 때문이다(사용자 결정).

- 판정 기준: 폴링이 실패한 압축기 중 **`MeasuredAt`이 오늘(한국 시간) 00:00 이전**인 채널
- `MeasuredAt`은 일부러 갱신하지 않는다 — 실제 측정 시각이 아니고, 그대로 둬야 다음 사이클에도 같은 판정이 나와 0이 유지된다(이미 0이면 EF가 변경 없음으로 처리해 쓰기도 발생하지 않는다)
- 통신이 복구되면 다음 성공 폴링에서 실제 값으로 덮인다
- 경보 상태(`AlarmStatus`)는 이때 재평가하지 않는다 — 통신 장애는 `HasCommunicationAlarm`으로 별도 관리하기 때문이다
