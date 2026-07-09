# 작업지시서 — M0~M1 (Sonnet 핸드오프)

- 발신: Claude Code (Opus) → 수신: Sonnet 5 (메인 개발/구현)
- 날짜: 2026-07-10 · 버전: v0.1.0
- 범위: 프롤로그 MVP의 M0(셋업·클릭 이동), M1(★근접 전투 판정 손맛 검증)
- 근거: [PRD v0.1](../prd/prd_20260710_카오스오브AI_프롤로그MVP_유미.md), [아키텍처](../architecture/architecture_20260710_전투코어구조_ClaudeCode.md)

---

## 0. 지금 상태 (이미 되어 있는 것)

전투 **로직 계층**은 코드로 완성돼 있다. 씬/에셋만 얹으면 M1까지 검증 가능.

- ✅ Godot 4.3 + C# 프로젝트 파일: `project.godot`, `ChaosOfAI.csproj`, `.sln`, InputMap 액션, autoload
- ✅ 핵심 로직: `BalanceConstants`, `CombatStats`, `DamageCalculator`, `IDamageable`
- ✅ 전투 판정: `MeleeHitbox`(부채꼴), `CombatFeedback`(히트스톱/셰이크)
- ✅ 데이터: `SkillData`/`ItemData`/`EnemyData` + `SkillLibrary` 폴백
- ✅ 액터/UI 스켈레톤: `Player`, `EnemyAI`, `DamageNumber`, `Hud`
- ✅ 회귀 테스트: `tests/DamageCalculatorTests`

❗ **아직 없는 것**: 모든 `.tscn` 씬(에디터에서 생성), 에셋, 내비게이션 베이크. `run/main_scene`이
`res://scenes/Main.tscn`를 가리키므로 처음 열면 경고가 난다 — M0 첫 작업이 이 씬 생성이다.

---

## 1. 선행: 환경 준비

이 개발 PC에는 현재 **.NET SDK와 Godot .NET이 설치돼 있지 않다.** 먼저 설치:

1. **.NET SDK 8.0** (x64) — https://dotnet.microsoft.com/download
2. **Godot 4.3 — .NET(C#) 빌드** — https://godotengine.org/download (Mono/.NET 버전)
3. 프로젝트 열기: Godot에서 `C:\Projects\ChaosOfAI\project.godot` import → 에디터가 `.godot/` 생성
4. 빌드 확인: 에디터 상단 **Build**(망치) → 컴파일 에러 0 확인
5. 로직 테스트: `tests/DamageCalculatorTests`를 임시 씬 루트에 붙이고 실행 → 출력창 `PASS=n FAIL=0` 확인

---

## 2. M0 — 셋업 · 등각 카메라 · 클릭 이동

목표 산출물: **클릭한 지점으로 걸어가는 캐릭터.**

1. **Main.tscn** 생성 (`scenes/Main.tscn`, 루트 `Node3D`):
   - `DirectionalLight3D` + `WorldEnvironment`(어두운 톤, clear color는 project.godot 기본값)
   - `NavigationRegion3D` + 바닥 `MeshInstance3D`(평면, y=0) + `StaticBody3D`/`CollisionShape3D`(world 레이어) → 내비 베이크
   - `Player` 인스턴스 배치
2. **등각 카메라**: `Camera3D`를 디아2식 쿼터뷰로. `Projection=Orthographic`, 회전 대략 `(-45°, 45°, 0)`,
   `Size` 12~16. Player의 자식으로 두거나 Main에 두고 Player를 추적. `Player.CameraPath`에 연결.
3. **Player.tscn** 생성 (`scenes/Player.tscn`, 루트 `CharacterBody3D`, 스크립트 `Player.cs`):
   - 자식 `NavigationAgent3D` (이름 그대로)
   - 자식 `MeleeHitbox`(스크립트 `MeleeHitbox.cs`, 이름 그대로) + `CollisionShape3D`(반경 ~2.5 구/실린더).
     충돌 마스크를 enemy_hitbox/enemy 대상에 맞춤(아래 6. 레이어 참고)
   - 임시 몸통 `MeshInstance3D`(캡슐) — 밝은 명도 + 림라이트 지향(§6)
   - `CollisionShape3D`(캐릭터 본체, player 레이어)
4. **검증**: 실행 → 좌클릭 지점으로 이동, 경로 자연스러움.

---

## 3. M1 — ★근접 전투 판정 (최우선 손맛 검증)★

목표 산출물: **강타로 몬스터를 때리는 손맛 "합격" 판정** (§4.4 기준 충족).

1. **Enemy 표적 만들기** (`scenes/Enemy.tscn`, 루트 `CharacterBody3D`, 스크립트 `EnemyAI.cs`):
   - `EnemyData.tres`를 `data/enemies/`에 만들어 `Data`에 할당 (Lv1, MinDmg 2 / MaxDmg 5 등)
   - 본체 `MeshInstance3D` + `CollisionShape3D` (enemy 레이어) — 히트박스에 잡히도록 레이어/마스크 정합
   - M1 단계에선 가만히 서 있는 더미로 충분 (AI는 M2)
2. **Main에 더미 3~5마리** 배치 → 군집 처리 손맛 확인(§4.4).
3. **스킬 연결**: Player의 `StrikeSkill/CrushSkill/SpinSkill`은 비워두면 `SkillLibrary` 폴백이 자동 적용.
   원하면 `data/skills/*.tres`로 만들어 할당(그럼 폴백 무시).
4. **입력**: `1`=강타, `2`=분쇄, `3`=회전 격돌 (InputMap에 매핑돼 있음). 마우스 방향으로 타격.
5. **타격감 얹기**(§4.3) — 스켈레톤에 TODO로 표시된 지점:
   - `EnemyAI.ReceiveHit`: flinch 애니 + 피격 발광, `DamageNumber` 팝업(result.Amount, IsCritical, Miss)
   - `CombatFeedback`는 이미 히트스톱/셰이크 제공 — autoload 등록돼 있음(`CombatFeedback.Instance`)
   - 넉백은 현재 간이 위치 이동 → 필요 시 물리 기반으로 개선
6. **밸런싱**: 손맛이 안 나면 `BalanceConstants`(히트스톱 길이, 넉백, StrPerDamage 등)와
   `SkillLibrary`/`.tres`의 윈도·배율·형상(Range/ConeHalfAngle)을 조정. **변경 후 `DamageCalculatorTests` 통과 확인.**

### M1 합격 기준 (§4.4 — 이게 통과여야 M2로)
- [ ] 클릭→이동→접근→타격 지연이 자연스럽다
- [ ] 부채꼴 범위가 시각 스윙과 일치(억울한 헛방/부당 피격 없음)
- [ ] 군집(3~5)을 강타로 쓸어담는 손맛
- [ ] 60fps에서 히트박스 판정 프레임 드랍 없음

---

## 4. 이후 (M2+ 개요, 이번 지시 범위 밖)

M2 몬스터 AI(추격→공격) · M3 스탯/레벨업 UI · M4 아이템 드랍/인벤토리 · M5 프롤로그 던전 · M6 아트/폴리싱.
`EnemyAI._PhysicsProcess`, `Hud`(구슬·업데이트 내역 화면)에 TODO 훅이 있다.

---

## 5. 프로젝트 규칙 (반드시 준수)

- **버전**: SemVer, 현재 `0.1.0`. 기능 추가 시 `VERSION` + `AppVersion`(APP_VERSION/VERSION_HISTORY) + `VERSIONING.md` 함께 갱신.
- **히스토리 메뉴(UI)**: 화면 상단 모서리 `vX.Y.Z` 상시 표시(→ `Hud`), 설정에 "업데이트 내역"(→ `AppVersion.History`).
- **문서 이중 저장**: 완료보고서 등은 `docs/`와 `G:\내 드라이브\SVIL Vault\03_PRJ\ChaosOfAI\` 동시 저장. 파일명 `카테고리_YYYYMMDD_내용_작업자.md`.
- **코드**: 파일 경로 주석 + 함수 상단 한 줄 주석, DRY, 에러 핸들링, 민감정보 노출 금지.
- 상세는 루트 [CLAUDE.md](../../CLAUDE.md) / [AGENTS.md](../../AGENTS.md).

## 6. 물리 레이어 (project.godot 정의)

| 레이어 | 용도 |
|-----|-----|
| 1 world | 지형/바닥 |
| 2 player | 플레이어 본체 |
| 3 enemy | 몬스터 본체 |
| 4 player_hitbox | 플레이어 공격 판정 |
| 5 enemy_hitbox | 몬스터 공격 판정 |

- Player의 `MeleeHitbox`는 **enemy(3)** 를 감지하도록 마스크 설정 → `GetOverlappingBodies()`가 `EnemyAI`를 반환.
- 몬스터 공격 판정은 대칭으로 player(2)를 감지.

---

문의/설계 판단이 필요하면 이 문서와 아키텍처 문서를 근거로 진행하고, 공식 변경은 반드시 테스트로 못 박을 것.
