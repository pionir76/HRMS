# Operation 모듈

운전 상태 처리 모듈.

- 전류값 기준 운전시작/정지 임계값 판정 (실제 판정 로직은 `Modules/Equipment/EquipmentStatusAggregator.cs`에 있음 — 통신/경보 집계와 같은 흐름에서 처리하는 게 더 단순해서 그쪽에 통합했다)
- 장비별 가동률 산출 (기간 지정 조회)
- 운전시작/정지 시각 기록, 히스테리시스, 지속시간 로직은 추후 결정

## 내부 구성

- `Models/` — `RunningStatus` enum, `UtilizationDto`
- `Controllers/` — `UtilizationController` (가동률 조회 API)
