# 검수 보고서 — M0~M4 그레이박스 구현 (v0.3.0)

> 2026-07-10 갱신: M3(경험치/레벨업)·M4(드랍/장비, 간소화) 추가 완료. §7 참고.

- 작성: Sonnet 5 (Claude Code) · 2026-07-10
- 이어받은 지점: Opus의 [M0~M1 작업지시서](../handoff/handoff_20260710_M0_M1_작업지시서_ClaudeCode.md) (코드 뼈대만 있고 씬/에셋 없음, 환경 미설치)
- 목표: "최종 검수 전까지 개발을 죽 진행" — 사람 플레이테스트 직전까지 밀어붙임

## 1. 이번 세션에서 한 일

### 환경 구축
- **.NET 8 SDK**, **Godot 4.7 (Mono/.NET 빌드)** winget으로 설치 완료.
- Godot 실행 파일: `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_...\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe`
  (winget 별칭 `godot`은 관리자 권한 없이 생성 안 됨 — PATH에 없으면 위 경로 직접 사용)
- Godot가 프로젝트를 열며 `ChaosOfAI.csproj`의 `Godot.NET.Sdk`를 4.3.0→4.7.0으로 자동 마이그레이션(정상 동작, 설치된 엔진 버전과 일치).

### 코드 정리
- `dotnet build` 기준 **nullable 경고 18개 → 0개**로 정리(Godot 노드 참조는 `null!` 관용구, 실제 옵셔널 필드는 `?`로 명시).
- `Player.cs`를 `"player"` 그룹에, `Hud.cs`를 `"hud"` 그룹에 등록해 `EnemyAI`/`Player`가 서로를 참조 없이 탐색 가능하게 함.

### 씬/데이터 작성 (텍스트 포맷으로 직접 저작 — 에디터 GUI 없이)
- `scenes/Player.tscn`, `scenes/Enemy.tscn`, `scenes/Main.tscn`
- `ui/Hud.tscn`, `ui/DamageNumber.tscn`
- `data/skills/{strike,crush,spin}.tres`, `data/enemies/{glitch_drone,rust_walker}.tres`
- 물리 레이어(§작업지시서 §6 표) 그대로 적용: Player=layer2, Enemy=layer3, 각 히트박스는 상대 본체 레이어만 감지.
- 등각 카메라: 단순 -45° 피치(요 회전 없음)로 우선 구현. **진짜 대각선 아이소메트릭(요 45°)은 아트 패스에서 검토 권장** (작업지시서에 이미 명시된 사항).
- `NavigationRegion3D`는 사전 베이크된 `.tres` 대신 **런타임 동기 베이크**(`MainBootstrap.cs`)로 처리 — 바닥 형상이 바뀌어도 항상 유효하고 headless 실행에서도 동일하게 동작.

### M2 몬스터 AI
- `EnemyAI`에 Idle→Chase→Attack 상태머신 구현. `NavigationAgent3D` 추격, hysteresis로 추격/공격 진동 방지.

### HUD
- 체력(빨강)·마나(파랑) 구슬: `TextureProgressBar` 대신 **`ProgressBar`+StyleBoxFlat**로 전환(텍스처 없이도 그레이박스 단계에서 바로 보이게).
- 설정 패널에 "업데이트 내역" 추가 — `AppVersion.History`를 최신순으로 렌더, `Esc`(`toggle_settings`)로 토글. 전역 CLAUDE.md 규칙 충족.
- `DamageNumberSpawner`(autoload)로 피격/미스 팝업을 Player/Enemy 양쪽에 연결.

## 2. ★ 실전 버그 발견 및 수정 ★

**증상**: `EnemyAI.PerformAttack()`이 `MeleeHitbox.BeginSwing() → Strike() → EndSwing()`을 **같은 물리 프레임 안에서 전부 실행**하고 있었음.

**원인**: Godot의 `Area3D.Monitoring`을 켠 직후, 겹침 목록(`GetOverlappingBodies()`)은 **다음 물리 스텝**에서야 갱신된다. 같은 프레임에 바로 `Strike()`를 호출하면 겹침 목록이 항상 비어 있어 **몬스터 공격이 영원히 명중하지 않는** 잠재 버그였다. (Player의 공격은 원래부터 `ActiveWindowStart~End`에 걸쳐 여러 프레임 동안 `Strike()`를 반복 호출하는 구조라 이 문제가 없었음 — Enemy만 단일 프레임 구현이었던 게 원인.)

**수정**: `EnemyAI`에 `_attacking`/`_attackWindowTimer` 상태를 추가해 `BalanceConstants.EnemyAttackWindowSeconds`(0.15초) 동안 매 프레임 `Strike()`를 반복 호출하도록 리팩터링(Player와 동일 패턴).

**검증**: 이 버그는 순수 로직 단위 테스트로는 절대 잡을 수 없다(Area3D 물리 타이밍 문제). 그래서 `tests/CombatSmokeTest`를 새로 만들어 **실제 Godot 엔진에서 Enemy가 Player를 물리적으로 때리는지**를 headless로 검증했고, 수정 전엔 8초 타임아웃까지 데미지가 전혀 안 들어가는 것을 확인 → 수정 후 첫 공격 윈도(0.1초)에 즉시 명중 확인.

## 3. 성능 이슈 발견 및 수정

런타임 내비게이션 베이크가 기본값(`ParsedGeometryType.MeshInstances`)이라 GPU에 있는 시각 메시 데이터를 CPU로 끌어와야 했음(엔진이 경고 출력: "significant performance issue"). `NavigationMesh.GeometryParsedGeometryType = StaticColliders`로 바꿔 충돌 형상(`CollisionShape3D`) 기준으로 파싱하도록 수정 — 경고 사라짐, 물리 형상과 항상 일치하므로 더 정확하기도 함.

## 4. 검증 결과 (전부 headless 자동화, 이 세션에서 실행 완료)

| 항목 | 결과 |
|---|---|
| `dotnet build ChaosOfAI.csproj` | **오류 0, 경고 0** |
| `godot --headless --import` | 정상 완료, `SkillData`/`ItemData`/`EnemyData` GlobalClass 등록 확인 |
| `tests/DamageCalculatorTests.tscn` (순수 로직 §4.2 공식 회귀) | **PASS=9 FAIL=0** |
| `tests/CombatSmokeTest.tscn` (엔진 통합: Enemy가 실제로 Player를 타격) | **PASS** (0.1초 만에 첫 명중, HP 132→129) |
| `scenes/Main.tscn` 300프레임 headless 구동(약 5초, 군집 5마리 + 내비게이션 추격) | 오류/경고 없이 정상 종료 |

회귀 테스트는 두 개 모두 `--headless` 단독 실행 가능(`godot --headless --path . res://tests/<이름>.tscn`), 종료 코드로 pass/fail 판별(0/1). CI 연동 시 그대로 사용 가능.

## 5. 아직 사람이 해야 하는 것 (진짜 "최종 검수")

이 세션은 **그레이박스(캡슐 메시 + 발광 머티리얼, 텍스처/애니메이션 없음)** 상태를 headless로만 검증했다. 아래는 자동화로 대체 불가능한 항목:

1. **§4.4 손맛 합격 기준은 주관적 판단** — Godot 에디터로 직접 열어 플레이해야 한다.
   `godot --path C:\Projects\ChaosOfAI` (또는 방금 설치한 실행 파일 직접 실행) → `Main.tscn` 재생.
   - 클릭→이동→접근→타격 지연이 자연스러운가
   - 부채꼴 범위가 시각 스윙과 일치하는가 (지금은 캡슐이라 스윙 애니메이션 자체가 없음 — 아트 붙기 전엔 판정 형상/사거리만 체감 가능)
   - 군집 처리 손맛, 프레임 드랍 여부
2. **카메라**: 현재 -45° 피치, 요 회전 없음. 진짜 등각(요 45°)으로 다듬을지 결정 필요.
3. **에셋 전무**: 모델/애니메이션/사운드/이펙트. M6(아트/폴리싱) 단계.
4. **M3~M5 미착수**: 레벨업/스탯 UI, 인벤토리/아이템 습득, 프롤로그 던전 레이아웃.
5. **밸런싱**: `BalanceConstants`/`.tres` 수치는 전부 초기값 — 실제 플레이 후 튜닝 필요.

## 6. 다음 세션을 위한 참고

- 이 PC에 `.NET 8 SDK` + `Godot 4.7 Mono`가 이미 설치되어 있음 — 재설치 불필요.
- 회귀 테스트는 코드/밸런싱 변경 시마다 먼저 돌려볼 것(`docs/architecture` 문서에도 안내됨).

## 7. 추가 세션 — M3(경험치/레벨업) + M4(드랍/장비, 간소화)

같은 날 이어서 진행. "개발을 최대한 끝까지 밀어붙인다"는 목표에 따라 M0~M2에서 멈추지 않고
코드로 검증 가능한 범위(M3/M4)까지 계속 진행했다.

### 구현
- `PlayerProgression`(XP 누적 → 레벨업 → 스탯/스킬 포인트, §5.5): `BalanceConstants.XpCurveBase/Growth`로 곡선 정의.
  레벨업 연쇄(한 번에 여러 레벨 상승) 처리 포함.
- 스탯 포인트 분배: 키 4/5/6/7 = STR/DEX/VIT/ENE(`alloc_str` 등 InputMap 액션). 그리드 UI 대신 즉시 반영 방식.
- `LootTable`(코드 정의 아이템 풀 3종, §5.4 접사 예시) + `EnemyAI.Die()`에서 `DropChance` 굴림.
- **범위 축소(의도적)**: PRD §5.4는 그리드 인벤토리 + 바닥 드랍 오브젝트를 명시하지만, 이번 패스는
  **처치 즉시 장비 보너스 자동 적용**으로 단순화했다. "처치→XP→레벨업→드랍→장비강화" 진행 루프의
  **체감 자체**를 먼저 검증하는 데 집중했고, 그리드 시각화/바닥 픽업 오브젝트는 다음 단계로 미룸.
- HUD에 `Lv.N XP x/y`, 미배분 포인트 표시 추가.

### 버그 재확인
- `EnemyAI.Die()`가 이제 `Player.Progression`/`Player.PickupItem`을 실제로 호출하므로, §2의 물리 판정 버그
  수정과 별개로 **처치 경로 자체**가 예외 없이 도는지 별도 검증이 필요했음 → `CombatSmokeTest`를 2단계로
  확장(Phase 1: 피격 확인, Phase 2: `ReceiveHit`로 강제 처치 후 `Die()`→보상 지급 확인). PASS 확인.

### 검증 결과 추가

| 항목 | 결과 |
|---|---|
| `tests/ProgressionTests.tscn` (XP 곡선·레벨업 연쇄·스탯 분배·장비 보너스·드랍 확률 14건) | **PASS=14 FAIL=0** |
| `tests/CombatSmokeTest.tscn` (2단계: 피격 확인 + 강제 처치→보상 지급 검증) | **PASS** (`enemyFreed=True xp=8 level=1`) |
| 전체 재빌드 | 오류 0, 경고 0 |

### 남은 것 (§5 목록에 추가)
6. **M4 그리드 인벤토리/바닥 드랍 오브젝트 시각화** — 지금은 처치 즉시 자동 적용이라 "드랍 확인 → 주우러 감" 손맛이 빠져 있음.
7. **스탯 분배 UI** — 지금은 키 입력(4/5/6/7) 텍스트 표시뿐, 버튼/그래픽 없음.
8. **밸런싱**: XP 곡선(`XpCurveBase=20, XpCurveGrowth=15`)과 드랍 풀(3종)도 초기값 — 실플레이 후 튜닝 대상.

버전은 `0.3.0`으로 상향(§VERSIONING.md 규칙상 마일스톤 단위 MINOR). `AppVersion.History`에 상세 반영.
