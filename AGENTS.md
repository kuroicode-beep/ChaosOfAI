# AGENTS.md — 카오스 오브 AI (ChaosOfAI)

모든 AI 에이전트(Cursor, Codex 등)를 위한 범용 지침. 상세 내용은 [CLAUDE.md](CLAUDE.md)를 정본으로 참고한다.

## 프로젝트 요약

- 카오스 오브 AI: Godot 4 + C# 다크 액션 RPG (디아블로 2 시스템 규격 이식, 오리지널 IP).
- Windows 1차 → 모바일/macOS 포팅. 현재 프롤로그 MVP 단계.
- 기획 정본: [docs/prd/](docs/prd/). 개발 순서는 M1(근접 전투 판정) 최우선 검증 후 확장.

## 반드시 지킬 규칙

1. **버전**: SemVer `MAJOR.MINOR.PATCH`, `0.1.0` 시작. 현재 버전은 `VERSION` 파일. 규칙은 [VERSIONING.md](VERSIONING.md).
2. **히스토리 메뉴**: 앱 UI에 `vX.Y.Z` 상시 표시 + 설정 메뉴 "업데이트 내역"(최신순, 버전당 2~4줄). 코드에서 `APP_VERSION` + `VERSION_HISTORY`로 관리.
3. **문서 이중 저장**: 완료보고서 등은 로컬 `docs/`와 Vault `G:\내 드라이브\SVIL Vault\03_PRJ\ChaosOfAI\`에 동시 저장, 동기화 유지.
4. **파일명**: `카테고리_YYYYMMDD_내용_작업자.md`, 공백 금지, UTF-8.
5. **코드**: 파일 경로 주석 + 함수 상단 한 줄 주석, DRY, 에러 핸들링 포함, 민감정보(키·토큰) 노출 금지.
6. **한글 경로 파일 조작은 PowerShell 사용** (Vault 경로에 한글 포함).

자세한 내용과 docs/ 구조·접근성 기준은 CLAUDE.md 참고.
