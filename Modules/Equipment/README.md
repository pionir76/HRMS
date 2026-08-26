# Equipment 모듈

장비 및 압축기 관리 모듈.

- 장비(Equipment) 등록/조회/수정, 운영상태 관리
- 압축기(Compressor) 등록/조회/수정, IP/포트/수집주기 등 통신 설정
- 센서 채널(Channel) 설정 관리
- 장비 운영상태(미운영/철거/사용중지 등)에 따른 수집 대상 제외 판단

## 내부 구성 (예정)

- `Controllers/` — 장비/압축기/채널 REST API
- `Models/` — 엔티티, DTO
- `Services/` — 등록/조회/수정 로직
