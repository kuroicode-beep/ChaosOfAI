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

## 5-1. v0.4.1 — 실플레이 치명 버그 2건 수정 (사용자 피드백 "안 보이고 안 됨")

exe를 실제로 실행하지 않고 headless만 믿은 것이 실수였다. 사용자가 "알아볼 수도 없고 조작도 안 돼"라고 피드백. **실제 창을 렌더링해 스크린샷으로 확인**하는 방식으로 재검증하며 2건의 치명 버그를 잡았다.

1. **화면 붕괴(카메라)**: Camera3D가 **회전하는 Player의 자식**이라, 플레이어가 방향을 바꿀 때마다 카메라가 통째로 회전 → 화면을 알아볼 수 없었다.
   - 수정: 카메라를 Player 밖(Main)으로 분리. `CameraFollow.cs`가 위치만 추종 + `LookAt`으로 항상 플레이어를 바라봄(약 53° 부감 등각). 셰이크는 Position이 아니라 프러스텀 오프셋(HOffset/VOffset)으로 처리해 추종과 충돌 제거.
2. **이동 불능(내비게이션)**: navmesh가 y≈0.5 높이에 베이크돼, `GetNextPathPosition()`이 플레이어와 XZ가 같고 Y만 다른 점을 반환 → 수평 방향이 0이 되어 **한 발도 못 움직임**. Y갭(0.5)이 `path_desired_distance`(0.3)보다 커서 경로가 다음 점으로 영영 안 넘어감. 몬스터 추격도 동일.
   - 수정: 평평한 무장애물 아레나에서는 내비게이션 대신 **직접 이동**(클릭 지점/플레이어를 향해 곧장). 내비게이션은 M5 던전(장애물 등장) 때 재도입.

**검증 방식 개선**: 실제 창을 렌더링해 뷰포트를 PNG로 저장하는 `tests/CaptureShot`(개발용)로 눈으로 확인 + `tests/MovementTest`(이동 회귀) 추가. 스크린샷으로 플레이어·몬스터·추격 AI가 모두 보이고 움직이는 것을 확인. headless 테스트는 5종으로 늘어 전부 통과.

## 6. 남은 것 (여전히 사람 필요)

- **손맛 실플레이**: 이제 바탕화면 바로가기로 바로 실행 가능. 캡슐 그레이박스라 애니메이션은 없지만 이동·전투·레벨업·드랍·사망/재시작 루프를 직접 체감 가능.
- 미착수: M5 던전, 그리드 인벤토리 시각화, 아트/애니메이션(M6), 밸런싱 튜닝.
