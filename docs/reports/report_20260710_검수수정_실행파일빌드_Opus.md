# 완료 보고서 — 검수 지적 수정 + 실행 파일 빌드 (v0.4.0)

- 작성: Opus 4.8 (Claude Code) · 2026-07-10
- 선행: [M0~M4 검수 보고](report_20260710_M0M2검수보고_Sonnet5.md)에서 지적한 5건
- 목표: "전부 수정하고 실행 파일까지 만들어서 바탕화면에 바로가기 생성"

## 1. 검수 지적 5건 수정

| # | 지적 | 수정 내용 |
|---|---|---|
| 1 [중] | 플레이어 사망 처리 없음 | `Player.Die()` 추가 — 입력/이동/공격 정지, HUD 사망 오버레이(`DeathPanel`) 표시, **R키로 씬 리로드 재시작**. `IsAlive`에 `_dead` 반영. |
| 2 [중] | 회전 격돌이 플레이어를 묶고 1회타로 동작 | `SkillData.AllowMoveWhileActive`(이동 허용) + `RehitInterval`(0.2초마다 히트캐시 리셋 → 지속 다단타) 추가. spin에 적용, 이동하며 회전 타격. |
| 3 [하] | 히트박스 형상 오버라이드 미복원 | `_Ready`에서 씬 기본값 캡처 후, `BeginAttack`에서 override≤0이면 기본값으로 복원(직전 스킬 값 잔류 제거). |
| 4 [하] | `HitStop` async 경합 | 벽시계(`Time.GetTicksMsec`) 종료 시각 기반으로 리팩터. `_Process`에서 복원 → 다중 처치 시에도 `TimeScale` 조기/중복 복원 없음. |
| 5 [하] | `_UnhandledInput` 폴링 | `@event.IsActionPressed(...)` 이벤트 기반으로 전환 + `SetInputAsHandled()`로 입력 소비. |

## 2. 회귀 검증 (headless, 전부 재실행)

| 테스트 | 결과 |
|---|---|
| `dotnet build` | 오류 0 / 경고 0 |
| DamageCalculatorTests | PASS=9 |
| ProgressionTests | PASS=14 |
| CombatSmokeTest (피격+처치→보상) | PASS |
| **PlayerDeathTest (신규, 사망 경로)** | PASS |

사망 처리 수정을 잠그기 위해 `tests/PlayerDeathTest`를 추가: 치명타 주입 → `IsAlive=false` 전환, 사망 후 추가 피격 무시, 사망 상태 `_PhysicsProcess` 안전성까지 엔진에서 검증.

## 3. Windows 실행 파일 빌드

- **환경 준비**: Godot 4.7 mono **export 템플릿**(1.15GB)을 GitHub 릴리스에서 내려받아 `%APPDATA%\Godot\export_templates\4.7.stable.mono\`에 설치.
- **export_presets.cfg** 작성(Windows Desktop, x86_64, self-contained): `embed_pck=true` + `dotnet/embed_build_outputs=true` → **단일 exe에 pck·.NET 어셈블리 임베드**.
- **산출물**: `build/windows/ChaosOfAI.exe` (약 180MB, 단일 파일).
- **부팅 검증**: headless로 Main 씬(플레이어+몬스터 5+HUD+내비 베이크+C# 오토로드) 120프레임 실행 → **exit 0, 오류/stderr 없음**. .NET 어셈블리 정상 로드 확인.
  - 주의: export 직후 즉시 실행하면 파일 락으로 종료코드 -1이 날 수 있음(수 초 후 정상). 배포엔 무관.

## 4. 바탕화면 바로가기

- `C:\Users\kuroi\OneDrive\Desktop\카오스 오브 AI.lnk` 생성 (대상: 위 exe, 작업 폴더: build/windows, 아이콘: exe 임베드).

## 5. 저장소

- `build/`는 `.gitignore` 처리(180MB exe는 git 미커밋). `export_presets.cfg`는 재현성 위해 커밋.
- 버전 `0.3.0 → 0.4.0`(검수 수정 + 실행 파일 마일스톤). VERSION/AppVersion/VERSIONING.md/HUD 라벨 동기화.

## 6. 남은 것 (여전히 사람 필요)

- **손맛 실플레이**: 이제 바탕화면 바로가기로 바로 실행 가능. 캡슐 그레이박스라 애니메이션은 없지만 이동·전투·레벨업·드랍·사망/재시작 루프를 직접 체감 가능.
- 미착수: M5 던전, 그리드 인벤토리 시각화, 아트/애니메이션(M6), 밸런싱 튜닝.
