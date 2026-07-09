# CLAUDE.md — 카오스 오브 AI (ChaosOfAI)

## 프로젝트 개요

- **프로젝트명**: 카오스 오브 AI (CHAOS OF AI)
- **장르**: 다크 액션 RPG (핵앤슬래시 / 디아블로 2 시스템 규격 이식, 오리지널 IP)
- **엔진/언어**: Godot 4 + C#
- **목표 플랫폼**: Windows 네이티브 (1차) → Android / iOS / macOS 포팅 (후속)
- **현재 범위**: 프롤로그 MVP (격투가 1클래스, 핵심 전투 손맛 검증)
- **상세 기획**: [docs/prd/](docs/prd/) — 프롤로그 MVP PRD v0.1
- **핵심 원칙**: 리스크 큰 근접 전투 판정 로직(M1)을 최우선 검증 후 나머지 확장.

## 버전 규칙

- Semantic Versioning `MAJOR.MINOR.PATCH`, `0.1.0` 시작. 현재 버전은 루트 `VERSION` 파일 참조.
- PATCH+1 = 버그픽스/소기능, MINOR+1 = 기능/UI 마일스톤, MAJOR+1 = 호환 깨짐/데이터 구조 변경. RC 완료 시 `v1.0.0`.
- 자세한 표와 업데이트 내역은 [VERSIONING.md](VERSIONING.md) 참고.

## 히스토리 메뉴 요구사항 (UI)

- 창 제목 + 화면 상단 모서리에 `vX.Y.Z` 상시 표시.
- 설정/정보 메뉴에 **"업데이트 내역"** 항목 — 버전별 날짜·요약을 최신순, 버전당 2~4줄로 앱 안에서 바로 확인 가능하게.
- 코드에서 `APP_VERSION` 상수 + `VERSION_HISTORY` 리스트로 관리, 기능 추가 시마다 갱신.

## 문서 이중 저장 규칙

완료보고서·사용자요청문서 등 산출물은 아래 **두 곳에 동시 저장**한다:

1. 로컬: `C:\Projects\ChaosOfAI\docs\` (성격에 맞는 하위 폴더)
2. Vault: `G:\내 드라이브\SVIL Vault\03_PRJ\ChaosOfAI\`

- 두 곳이 항상 같은 상태가 되도록 동기화한다.
- 파일명 규칙: `카테고리_YYYYMMDD_내용_작업자.md`, 공백 금지(언더스코어), UTF-8.
- 문서 저장은 Markdown 기준(Notion 미사용). 초기 기획 원본은 SVIL Outline 위키에 있으며 로컬 `docs/`로 동기화됨.

## docs/ 구조

```
docs/
  prd/           # PRD, 스펙 (프롤로그 MVP PRD v0.1)
  architecture/  # 아키텍처 문서
  storyboard/    # 스토리보드
  handoff/       # 작업지시서
  reports/       # 완료보고서 (이중 저장 대상)
```

## 접근성 / 가독성 기준

- 다크 배경 위주 아트지만 UI/HUD는 큰 폰트·고대비. 캐릭터는 림라이트로 배경에서 분리.
- 색상만으로 상태 구분하지 않고 텍스트 라벨 병행. 데미지 넘버·아이템 등급 라벨은 외곽선으로 가독성 확보.
