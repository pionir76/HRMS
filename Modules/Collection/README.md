# Collection 모듈

데이터 수집 모듈.

- 압축기별 독립적인 비동기 수집 루프 (1~2초 주기)
- Communication 모듈을 통해 수집한 원시 데이터를 Realtime/History/Alarm/Operation 모듈로 전달
- 수집 대상 압축기 목록 관리 (Equipment 모듈의 운영상태 반영)

## 내부 구성 (예정)

- `Services/` — 수집 루프(BackgroundService), 스케줄링
