# HRMS API 매뉴얼 (프론트엔드용)

프론트엔드에서 호출할 수 있는 REST API 전체 목록. 새 API가 추가/변경되면 **이 문서도 반드시 같이 갱신**한다 (CLAUDE.md에 규칙으로 명시되어 있음).

## 공통 사항

- 현재 **인증 없음** — 모든 API가 로그인 없이 호출 가능하다 (추후 로그인 기능 추가 시 변경될 예정).
- 개발 서버 주소: `https://localhost:7253` (자체 서명 인증서 사용 — 브라우저에서 경고가 뜨면 진행 필요)
- 모든 응답은 JSON. 필드명은 camelCase.
- **enum 필드는 전부 사람이 읽을 수 있는 한글 문자열**로 내려간다 (숫자 코드 아님). 각 필드가 가질 수 있는 값은 아래 "enum 값 목록" 참고.
- **날짜/시간 파라미터(`date`, `from`, `to`)는 한국 시간(KST) 기준**으로 해석된다.
- 존재하지 않는 리소스를 조회하면 `404 Not Found`를 반환한다 (본문 없음).

## enum 값 목록

| 필드 | 가능한 값 |
|---|---|
| 장비 `status` | 운영, 미운영, 수리중, 점검중, 철거예정, 철거, 사용중지, 기타 |
| `communicationStatus` | 연결됨, 끊김, 재접속중 |
| `alarmStatus` | 정상, 경보발생대기, 경보발생, 정상복귀대기, 경보비활성화 (경보확인/경보해제는 없음) |
| `runningStatus` | 운전, 정지 |
| `channelNo` | CH01, CH02, CH03, CH04, CH05, CH06, CH07 |

---

## 1. `GET /api/equipments`

장비 전체 목록.

**응답 예시**
```json
[
  { "id": 1, "region": "A지구", "buildingName": "PT배기환경시험동", "name": "인증환경챔버&쇼크룸", "status": "운영" },
  { "id": 2, "region": "A지구", "buildingName": "환경선행연구동", "name": "연료냉각칠러 1호기", "status": "운영" }
]
```

## 2. `GET /api/equipments/{id}`

장비 단건 조회.

**응답 예시**
```json
{ "id": 1, "region": "A지구", "buildingName": "PT배기환경시험동", "name": "인증환경챔버&쇼크룸", "status": "운영" }
```

**오류**: 없는 `id`면 `404`.

> 참고: 장비의 실시간 파생 상태(`runningStatus`/`alarmStatus`/`communicationStatus`, 관리자가 설정하는 `status`와는 별개)는 아직 이 응답에 포함되어 있지 않다. DB(`Equipments` 테이블)에는 이미 있으니, 필요해지면 DTO에 추가하면 된다.

## 3. `GET /api/equipments/{id}/compressors`

해당 장비 소속 압축기 목록.

**응답 예시**
```json
[
  { "id": 1, "ipAddress": "10.93.78.201", "macAddress": "00:06:AC:E0:0D:5A", "communicationStatus": "연결됨", "alarmStatus": "정상" },
  { "id": 2, "ipAddress": "10.93.78.202", "macAddress": "98:06:37:70:00:1B", "communicationStatus": "끊김", "alarmStatus": "경보발생" }
]
```

`ipAddress`/`macAddress`는 원본 자산 목록에 값이 없는 압축기의 경우 `null`일 수 있다.

**오류**: 없는 장비 `id`면 `404`.

## 4. `GET /api/compressors`

압축기 전체 목록(소속 장비명 조인 포함). 대시보드에서 압축기 테이블을 한 번에 보여줄 때 사용.

**응답 예시**
```json
[
  {
    "id": 1,
    "buildingName": "PT배기환경시험동",
    "equipmentName": "인증환경챔버&쇼크룸",
    "ipAddress": "10.93.78.201",
    "macAddress": "00:06:AC:E0:0D:5A",
    "communicationStatus": "연결됨",
    "alarmStatus": "정상"
  }
]
```

## 5. `GET /api/compressors/{id}/channels`

해당 압축기의 CH01~07 현재값 (1초 주기로 갱신되는 최신값, 최대 7개 반환).

**응답 예시**
```json
[
  { "channelNo": "CH01", "value": -12.3, "measuredAt": "2026-08-26T05:00:00+00:00" },
  { "channelNo": "CH02", "value": 45.6, "measuredAt": "2026-08-26T05:00:00+00:00" }
]
```

`measuredAt`은 UTC로 내려간다 (프론트에서 필요시 로컬시각으로 변환).

**오류**: 없는 압축기 `id`면 `404`.

## 6. `GET /api/compressors/{id}/trend?date=yyyy-MM-dd`

해당 압축기의 하루치 트렌드(1분 단위, 최대 1,440개). 그래프 그릴 때 사용.

| 파라미터 | 필수 | 설명 |
|---|---|---|
| `date` | 아니오 | 조회할 날짜(한국 시간 기준). 생략 시 오늘 |

**응답 예시**
```json
[
  {
    "measuredAt": "2026-08-25T15:00:00+00:00",
    "ch01": -12.3, "ch02": 45.6, "ch03": 30.1, "ch04": 5.2, "ch05": 8.7, "ch06": 3.4, "ch07": 12.5,
    "runningStatus": "운전", "alarmStatus": "정상", "communicationStatus": "연결됨"
  }
]
```

**통신 장애 구간 구분 방법**: 통신이 끊기면 채널값이 그 자리에서 멈춘 값으로 계속 반복된다. 그래프 선이 평평한 구간을 발견하면 그 시점들의 `communicationStatus`를 같이 확인해서, 실제로 값이 안정된 것인지(`연결됨`) 통신 장애로 값이 멈춘 것인지(`끊김`/`재접속중`)를 구분해서 표시할 수 있다(예: 회색 음영 처리).

**오류**: 없는 압축기 `id`면 `404`.

## 7. `GET /api/equipments/{id}/utilization?from=yyyy-MM-dd&to=yyyy-MM-dd`

지정 기간의 장비 가동률(%) — 그 기간 중 장비가 "운전" 상태였던 시간의 비율.

| 파라미터 | 필수 | 설명 |
|---|---|---|
| `from` | 아니오 | 시작 날짜(한국 시간, 포함). 생략 시 오늘 |
| `to` | 아니오 | 종료 날짜(한국 시간, 포함). 생략 시 `from`과 동일한 하루 |

**응답 예시**
```json
{
  "equipmentId": 1,
  "from": "2026-08-01",
  "to": "2026-08-31",
  "totalMinutes": 44640,
  "runningMinutes": 38977,
  "utilizationPercent": 87.3
}
```

- `totalMinutes`는 "요청한 기간 전체 분"이 아니라 **실제로 트렌드 기록이 존재하는 분**이다. 서버 다운타임 등 기록 자체가 없는 구간은 분모에서 빠진다. 이 값이 기대보다 작다면 그 기간에 데이터가 비어있는 구간이 있었다는 뜻이다.
- 해당 기간에 기록이 전혀 없으면 `totalMinutes: 0`, `utilizationPercent: null`이 반환된다(에러 아님).
- 압축기가 하나도 없는 장비도 위와 동일하게 `null`로 반환된다.

**오류**: 없는 장비 `id`면 `404`.

---

## 아직 없는 API (참고)

- 로그인/인증
- 장비/압축기 등록·수정 (현재는 조회만 가능)
- 비상정지
- 보고서/게시판
