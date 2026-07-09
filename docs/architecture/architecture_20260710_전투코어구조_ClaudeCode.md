# 카오스 오브 AI — 전투 코어 아키텍처 (M0~M1)

- 작성: Claude Code (Opus) · 2026-07-10 · v0.1.0
- 대상: 프롤로그 MVP의 핵심 전투 판정(§4)과 스탯/데미지(§5) 코드 계층
- 원칙: 리스크 큰 핵심 로직을 엔진 비의존 순수 코드로 먼저 못 박고, 씬/연출은 그 위에 얹는다.

## 1. 계층 구조

```
[순수 로직 · 엔진 비의존 · 테스트 가능]
  BalanceConstants   밸런싱 상수 한 곳 (K, 크리, 클램프, 파생계수)
  CombatStats        1차 스탯 → 파생 스탯(HP/MP/방어/AR) + 런타임 HP/MP
  DamageCalculator   ★명중률·데미지·크리티컬 공식(§4.2). RNG 주입 → 결정론적

[엔진 결합 · Godot]
  IDamageable        피격 대상 계약(몬스터/플레이어 공통)
  MeleeHitbox        ★부채꼴 히트박스(Area3D). 광역 감지 후 각도·거리 필터
  CombatFeedback     히트스톱 + 카메라 셰이크 (Autoload 싱글턴)

[데이터 주도 · Resource(.tres)]
  SkillData          스킬 배율/윈도/넉백/형상
  ItemData           등급/슬롯/접사
  EnemyData          몬스터 스탯/전투/AI/드랍
  SkillLibrary       .tres 미할당 시 코드 폴백(강타/분쇄/회전)

[액터 · UI (스켈레톤, Sonnet 확장)]
  Player             클릭 이동 + 공격 파이프라인
  EnemyAI            피격 대상 + (M2)추격/공격 AI
  DamageNumber, Hud  데미지 팝업 / 체력·마나 구슬 + 버전 표시
```

파일 위치: `scripts/combat`, `scripts/resources`, `scripts/actors`, `scripts/core`, `ui`, `tests`.

## 2. 공격 파이프라인 (한 번의 스윙)

```
입력(skill_*) 
  → Player.BeginAttack: MP 소모, 마우스 방향 회전, 히트박스 형상 오버라이드
  → ActiveWindowStart 도달: MeleeHitbox.BeginSwing (히트 캐시 clear)
  → 윈도 동안 매 물리프레임: MeleeHitbox.Strike
       · 광역 Area3D 오버랩 수집
       · 각 대상: 거리 ≤ Range && 전방 dot ≥ cos(halfAngle) 필터 (부채꼴)
       · 한 스윙에 같은 적 1회만(_hitThisSwing 캐시 → 다단 히트 방지)
       · DamageCalculator.Resolve → IDamageable.ReceiveHit(데미지/넉백)
  → 명중 발생 시 CombatFeedback.HitStop + Shake
  → ActiveWindowEnd 도달: EndSwing
```

## 3. 핵심 설계 결정 (리뷰 포인트)

1. **부채꼴 = 광역 브로드페이즈 + 코드 각도 필터.**
   Godot에 정확한 부채꼴 콜리전 프리미티브가 없다. 넓은 구/실린더로 겹침만 잡고,
   `forward.Dot(toTarget) ≥ cos(halfAngle)` + 거리로 걸러 "억울한 헛방/부당 피격"(§4.4)을 줄인다.
   → 시각 스윙과 판정 일치는 `ConeHalfAngleDeg`/`Range`를 애니메이션에 맞춰 튜닝.

2. **Defense와 DamageReduction 분리.**
   디아2는 Defense가 명중률에만 관여하고 데미지는 감소시키지 않는다. 그러나 PRD §4.2가
   최종 데미지에 별도 `(1 - 적 방어 감쇠)` 항을 명시하므로, AR/DEF(명중)와 별개의
   물리 경감치 `EnemyData.DamageReduction`(0~1, 기본 0)을 둔다. 즉사 방지 상한 0.9.

3. **RNG 주입 → 결정론적 검증.**
   `DamageCalculator.Resolve(atk, def, Random)`. 씨드 고정으로 `tests/DamageCalculatorTests`가
   공식을 회귀 검증. 밸런싱 변경 시 이 테스트부터 통과 확인.

4. **데이터 주도(.tres) + 코드 폴백.**
   스킬/아이템/몬스터는 `.tres`로 저작해 코드 수정 없이 밸런싱(§7.2). 다만 `.tres` 미할당
   상태에서도 M1이 돌아가도록 `SkillLibrary` 코드 폴백을 제공(에디터 저작 시 자동 무시).

5. **액티브 윈도 모델.**
   타격 판정은 애니 "타격 프레임"에서만 켠다(`ActiveWindowStart~End`). 단발기는 짧은 윈도,
   회전 격돌(spin)은 긴 윈도 + 전방위(halfAngle 180°)로 표현.

## 4. 스탯/데미지 공식 (구현 확정치)

- 파생: `MaxHP = 50 + VIT·4 + Lv·2 + 장비`, `MaxMP = 20 + ENE·3`, `DEF = 장비 + DEX·0.25`, `AR = 20 + DEX·5 + 장비`
- 명중률 = `clamp( 2·AR/(AR+DEF) · Lv/(Lv+적Lv), 0.05, 0.95 )`
- 데미지 = `(rand(min,max) + floor(STR/10)) · (1 + 스킬배율) · (1 - 감쇠)`, 크리 시 ×2, 최소 1
- 상수는 전부 `BalanceConstants`. 초기값이며 M1 튜닝 대상.

## 5. 아직 코드가 만들지 않은 것 (씬/에셋 = Sonnet)

- `.tscn` 씬 전부(Main, Player, Enemy, HUD, DamageNumber). 노드 트리는 각 스크립트의 상단 주석에 기대치 명시.
- 애니메이션/모델/사운드 등 에셋.
- NavigationRegion3D 베이크, 실제 카메라 등각 셋업.
- 인벤토리 그리드 UI, 드랍/경험치/레벨업 UI 흐름.

구체 진행은 [handoff_20260710_M0_M1_작업지시서_ClaudeCode.md](../handoff/handoff_20260710_M0_M1_작업지시서_ClaudeCode.md) 참고.
