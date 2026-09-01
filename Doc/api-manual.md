# HRMS API 매뉴얼 (프론트엔드용)

프론트엔드에서 호출할 수 있는 REST API 전체 목록. 새 API가 추가/변경되면 **이 문서도 반드시 같이 갱신**한다 (CLAUDE.md에 규칙으로 명시되어 있음).

## 공통 사항

- **로그인 필요**. `/api/auth/login`을 제외한 모든 API는 요청 헤더에 `Authorization: Bearer {token}`을 포함해야 한다. 토큰이 없거나 유효하지 않으면 `401 Unauthorized`.
- 토큰은 로그인 응답의 `token` 값이며, 발급 후 **12시간 뒤 만료**된다 (refresh 기능 없음 — 만료되면 재로그인). 만료된 토큰으로 호출해도 `401`.
- 개발 서버 주소: `http://localhost:5018` (인증서 문제 없이 바로 호출 가능, 기본 실행 시 이 주소) 또는 `https://localhost:7253` (자체 서명 인증서 — 브라우저에서 그 주소로 먼저 접속해 경고를 수락해야 fetch 가능)
- 개발 환경(`Development`)에서는 모든 origin에 대해 CORS가 열려있다. 프론트를 백엔드와 같은 PC의 다른 포트(Vite/CRA 개발 서버 등)에서 띄워도 별도 설정 없이 바로 호출 가능하다. 운영 환경에서는 CORS가 비활성화되어 있으므로, 실제 배포 시 프론트 origin을 확정해서 추가해야 한다.
- 모든 응답은 JSON. 필드명은 camelCase.
- **enum 필드는 전부 사람이 읽을 수 있는 한글 문자열**로 내려간다 (숫자 코드 아님). 각 필드가 가질 수 있는 값은 아래 "enum 값 목록" 참고.
- **날짜/시간 파라미터(`date`, `from`, `to`)는 한국 시간(KST) 기준**으로 해석된다.
- **압축기 채널값(`value`, `ch01`~`ch07`)은 전부 TLC 원시값(raw int16)이다.** 서버는 소수점 변환을 하지 않는다 — 실제 값으로 표시하는 건 프론트가 담당한다. 채널별 소수점 자리수(dp)는 `GET /api/compressors/{id}/channel-settings`(5-1번)로 조회한다.
- 존재하지 않는 리소스를 조회하면 `404 Not Found`를 반환한다 (본문 없음).

## enum 값 목록

| 필드 | 가능한 값 |
|---|---|
| 장비 `status` | 운영, 미운영, 수리중, 점검중, 철거예정, 철거, 사용중지, 기타 |
| `communicationStatus`(장비/압축기 목록 API) | 연결됨, 끊김, 재접속중 |
| `channelNo` | CH01, CH02, CH03, CH04, CH05, CH06, CH07 |

운전 상태는 처음부터 "운전/정지" 둘 뿐이라 enum 문자열이 아니라 **boolean `isRunning`**으로 내려간다. 경보 상태도 마찬가지로 장비/압축기 목록 API에서는 세부 단계(경보발생대기/정상복귀대기 등) 없이 **boolean `hasAlarm`**(경보 확정 여부)로만 내려간다 — 프론트는 확정된 경보 여부만 필요하다는 결정. 트렌드 API(`/trend`)도 같은 이유로 `communicationStatus`를 `isConnected`(boolean)로 대신한다 — 6번 항목 참고.

---

## 0. `POST /api/auth/login`

로그인. 인증이 필요 없는 유일한 API.

**요청**
```json
{ "username": "admin", "password": "admin1234" }
```

**응답 예시 (200)**
```json
{ "token": "eyJhbGciOi...", "username": "admin", "role": "시스템관리자" }
```

이후 요청에는 `Authorization: Bearer {token}` 헤더를 붙인다.

`role`은 `시스템관리자` / `안전관리총괄자` / `안전관리책임자` / `안전관리원` / `일반관리자` 중 하나. `시스템관리자`만 전체 권한을 갖고 나머지 4개는 현재 전부 조회만 가능하다(역할 간 권한 차이 없음 — 추후 보고서 결재 기능에서 차이가 생길 예정). 비상정지 권한은 아직 별도 플래그로 존재하지 않으며, 나중에 역할 기준(예: "안전관리책임자만 가능")으로 일괄 적용할 예정이다.

**오류**: 아이디/비밀번호가 틀리거나 비활성화된 계정이면 `401 Unauthorized` (본문 없음).

## 0-1. `POST /api/auth/logout`

로그아웃 이력만 기록한다 (토큰 자체는 서버가 무효화하지 않으므로, 프론트에서 저장해둔 토큰을 버리는 것이 실질적인 로그아웃이다). `Authorization` 헤더 필요.

**응답**: `200 OK` (본문 없음)

---

## 1. `GET /api/equipments`

장비 전체 목록.

**응답 예시**
```json
[
  { "id": 1, "region": "A지구", "buildingName": "PT배기환경시험동", "name": "인증환경챔버&쇼크룸", "status": "운영", "isRunning": true, "communicationStatus": "연결됨", "hasAlarm": false },
  { "id": 2, "region": "A지구", "buildingName": "환경선행연구동", "name": "연료냉각칠러 1호기", "status": "운영", "isRunning": false, "communicationStatus": "연결됨", "hasAlarm": true }
]
```

`isRunning`/`communicationStatus`/`hasAlarm`은 관리자가 설정하는 `status`와는 별개로, 소속 압축기 데이터로부터 매 폴링 사이클 자동 집계되는 실시간 파생 상태다.

## 2. `GET /api/equipments/{id}`

장비 단건 조회.

**응답 예시**
```json
{ "id": 1, "region": "A지구", "buildingName": "PT배기환경시험동", "name": "인증환경챔버&쇼크룸", "status": "운영", "isRunning": true, "communicationStatus": "연결됨", "hasAlarm": false }
```

**오류**: 없는 `id`면 `404`.

## 3. `GET /api/equipments/{id}/compressors`

해당 장비 소속 압축기 목록.

**응답 예시**
```json
[
  { "id": 1, "sequenceNo": 1, "ipAddress": "10.93.78.201", "macAddress": "00:06:AC:E0:0D:5A", "communicationStatus": "연결됨", "hasAlarm": false },
  { "id": 2, "sequenceNo": 2, "ipAddress": "10.93.78.202", "macAddress": "98:06:37:70:00:1B", "communicationStatus": "끊김", "hasAlarm": true }
]
```

`sequenceNo`는 소속 장비 내 압축기 순번(1부터, 화면에 "압축기 1"처럼 표시할 때 사용). 응답은 이 값 기준으로 정렬되어 나간다. `ipAddress`/`macAddress`는 원본 자산 목록에 값이 없는 압축기의 경우 `null`일 수 있다.

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
    "hasAlarm": false
  }
]
```

## 5. `GET /api/compressors/{id}/channels`

해당 압축기의 CH01~07 현재값 (3초 주기로 갱신되는 최신값, 최대 7개 반환).

**응답 예시**
```json
[
  { "channelNo": "CH01", "value": -123, "measuredAt": "2026-08-26T05:00:00+00:00" },
  { "channelNo": "CH02", "value": 456, "measuredAt": "2026-08-26T05:00:00+00:00" }
]
```

`value`는 **TLC로부터 받은 원시값(raw int16) 그대로**다. 서버는 소수점 변환을 전혀 하지 않으며, 실제 표시값으로 바꾸는 건 프론트 책임이다 (예: 위 `-123`은 실제로는 `-12.3`을 뜻함). 채널별 소수점 자리수(dp)는 아래 5-1번 API로 조회한다.

`measuredAt`은 UTC로 내려간다 (프론트에서 필요시 로컬시각으로 변환).

**오류**: 없는 압축기 `id`면 `404`.

## 5-1. `GET /api/compressors/{id}/channel-settings`

해당 압축기의 CH01~07 채널 설정(경보 상/하한, 표시 소수점 자리수 등). `value`를 실제 표시값으로 바꾸려면 이 API로 받은 `decimalPlaces`만큼 소수점을 적용하면 된다 (예: raw `-123`, `decimalPlaces: 1` → `-12.3`).

**응답 예시**
```json
[
  {
    "channelNo": "CH01", "channelName": "저온", "unit": "℃",
    "enabled": true, "lowerLimit": 0, "upperLimit": 1000,
    "alarmEnabled": true, "alarmDelaySeconds": 30, "alarmClearDelaySeconds": 30,
    "decimalPlaces": 1
  }
]
```

- `lowerLimit`/`upperLimit`도 채널값과 같은 **raw 스케일**이다.
- 채널 설정은 자주 바뀌지 않으니, 매번 새로 조회하기보다 프론트에서 적당히 캐싱해도 된다.

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
    "ch01": -123, "ch02": 456, "ch03": 301, "ch04": 52, "ch05": 87, "ch06": 34, "ch07": 125,
    "isRunning": true, "hasAlarm": false, "isConnected": true
  }
]
```

`ch01`~`ch07`도 `/channels`와 마찬가지로 **원시값(raw int16)**이다. 소수점 변환은 프론트가 담당하며, 자리수는 `channel-settings`(5-1번) 응답의 `decimalPlaces`를 쓴다.

`isRunning`/`hasAlarm`/`isConnected`는 **압축기 자신이 아니라 그 압축기가 소속된 장비의 집계 상태**이며 셋 다 boolean이다. 같은 장비에 압축기가 여러 대면 그 압축기들의 이 세 값은 전부 동일하다. `hasAlarm`/`isConnected`는 세부 단계(경보발생대기/정상복귀대기, 끊김/재접속중 등) 없이 "장비에 경보가 있는지"/"장비가 연결되어 있는지"만 나타낸다.

**통신 장애 구간 구분 방법**: 통신이 끊기면 채널값이 그 자리에서 멈춘 값으로 계속 반복된다. 그래프 선이 평평한 구간을 발견하면 그 시점들의 `isConnected`를 같이 확인해서, 실제로 값이 안정된 것인지(`true`) 통신 장애로 값이 멈춘 것인지(`false`)를 구분해서 표시할 수 있다(예: 회색 음영 처리).

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

## 8. `GET /api/summary`

실시간 현황 화면 상단 카운트용 집계. 조건 없이 전체 기준.

**응답 예시**
```json
{
  "totalEquipmentCount": 105,
  "totalCompressorCount": 244,
  "runningEquipmentCount": 97,
  "communicationFailedCompressorCount": 3
}
```

- `runningEquipmentCount`는 장비 단위 운전 여부(`isRunning`) 기준이다. **압축기 단위 운전 중 수량은 제공하지 않는다** — 운전 판정 자체가 장비 단위로만 존재한다.
- `communicationFailedCompressorCount`는 압축기의 `communicationStatus`가 `연결됨`이 아닌(끊김 또는 재접속중) 압축기 수다.

## 9. `GET /api/events?since=&take=`

이벤트 로그(로그인/로그아웃, 경보 발생·해제, 통신 장애 등) 조회. 실시간 현황 화면의 이벤트 피드용.

| 파라미터 | 필수 | 설명 |
|---|---|---|
| `since` | 아니오 | 이 시각(ISO 8601, UTC) 이후의 이벤트만 조회. 생략 시 최신 이벤트부터 조회 |
| `take` | 아니오 | 최대 반환 개수. 생략 시 100, 최대 500 |

**응답 예시**
```json
[
  {
    "id": 15689,
    "category": "Alarm",
    "message": "C지구 환경차개발시험3동의 환경챔버2 쇼크룸의 압축기 1번 오일온도값이 범위를 벗어났습니다.",
    "username": null,
    "equipmentId": 101,
    "compressorId": 232,
    "channelNo": "CH03",
    "createdAt": "2026-08-31T07:15:47.876368+00:00"
  }
]
```

- `category`는 `UserAccess`/`EmergencyStop`/`Communication`/`Alarm`/`System` 중 하나. (아직 `EmergencyStop`은 실제로 기록되지 않음)
- `equipmentId`/`compressorId`/`channelNo`는 해당 없으면 `null`(예: 로그인 이벤트).
- **정렬 방향이 `since` 여부에 따라 다르다**: `since` 없이 호출(최초 조회)하면 **최신순**으로 잘라서 반환하고, `since`를 주면(이어서 폴링) **오래된 순**으로 반환한다 — 폴링 사이에 이벤트가 몰려서 `take` 개수를 넘기더라도, 최신순으로 자르면 오래된 이벤트가 영영 안 보일 수 있어서다. 프론트는 매번 응답의 마지막 항목(또는 첫 항목, 방향에 따라)의 `createdAt`을 다음 호출의 `since`로 넘기면 된다.

---

## 아직 없는 API (참고)

- 장비/압축기 등록·수정 (현재는 조회만 가능)
- 비상정지
- 보고서/게시판
- 사용자 관리(계정 생성/수정) — 현재는 최초 관리자 계정만 자동 시드되고, 이후 계정 추가는 DB에 직접 넣어야 한다
