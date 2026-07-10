// scripts/combat/BalanceConstants.cs
// 전투/성장 밸런싱 상수 한 곳 모음. 밸런싱은 여기 + .tres 리소스에서만 조정한다(코드 산개 금지).
// PRD §4.2 / §5.2 규격을 상수화. 값은 초기값이며 M1 손맛 검증 중 튜닝 대상.

namespace ChaosOfAI.Combat
{
    public static class BalanceConstants
    {
        // ── 데미지 공식 (§4.2) ─────────────────────────────
        // 스탯 보너스 = floor(STR / StrPerDamage). 초기값 10 → STR 10당 +1 데미지.
        public const int StrPerDamage = 10;

        // 크리티컬: 기본 확률/배율. 격투가 패시브로 확률 상승 여지.
        public const float BaseCritChance = 0.05f; // 5%
        public const float CritMultiplier = 2.0f;  // 데미지 2배

        // ── 명중 판정 (§4.2) ──────────────────────────────
        // 명중률 = 2·AR/(AR+DEF) · myLevel/(myLevel+enemyLevel), clamp[Min,Max]
        public const float MinHitChance = 0.05f; // 5%
        public const float MaxHitChance = 0.95f; // 95%

        // ── 방어 감쇠 (§4.2 최종 데미지의 (1 - 적 방어 감쇠)) ──
        // 설계 노트: 디아2는 Defense가 "명중률"에만 관여하고 데미지는 감소시키지 않는다.
        // 그러나 PRD가 최종 데미지에 별도 방어 감쇠 항을 명시하므로, AR/DEF와 분리된
        // 물리 데미지 경감치(0~1)를 EnemyData.DamageReduction으로 둔다. 기본 낮게 유지.
        public const float MaxDamageReduction = 0.90f; // 경감 상한(즉사 방지)

        // ── 파생 스탯 계수 (§5.2) ──────────────────────────
        public const int BaseHp = 50;
        public const float HpPerVit = 4f;
        public const float HpPerLevel = 2f;

        public const int BaseMp = 20;
        public const float MpPerEne = 3f;

        public const float DefensePerDex = 0.25f; // DEX 4당 방어 +1
        public const float ArPerDex = 5f;         // DEX 1당 AR +5
        public const int BaseAttackRating = 20;

        // ── 몬스터 공격 판정 (M2) ─────────────────────────
        // Area3D.Monitoring을 켠 직후 같은 물리 프레임엔 겹침 목록이 비어있으므로(물리 서버가
        // 다음 스텝에서 갱신), 여러 프레임에 걸쳐 판정 윈도를 유지해야 실제 명중이 성립한다.
        public const float EnemyAttackWindowSeconds = 0.15f;

        // ── 타격감 (§4.3) ─────────────────────────────────
        // 히트스톱: 타격 성립 시 정지 프레임(초). 강타/처치에서 강조.
        public const float HitStopSeconds = 0.06f;
        public const float HeavyHitStopSeconds = 0.10f;

        // 넉백 기본 세기(월드 유닛). 스킬별 배율은 SkillData.KnockbackScale.
        public const float BaseKnockback = 1.5f;

        // ── 레벨업 (§5.5) ─────────────────────────────────
        public const int StatPointsPerLevel = 5;
        public const int SkillPointsPerLevel = 1;

        // 경험치 곡선: 다음 레벨까지 필요 XP = Base + (Level-1)·Growth. 초기값, 실플레이 후 튜닝 대상.
        public const int XpCurveBase = 20;
        public const int XpCurveGrowth = 15;

        // 스킬 강화(§5.5 스킬 포인트 사용처): 포인트 1개당 해당 스킬 DamageMultiplier 증가치.
        public const float SkillUpgradeDamageBonus = 0.15f;
    }
}
