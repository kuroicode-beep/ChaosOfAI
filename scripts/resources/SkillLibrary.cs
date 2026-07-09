// scripts/resources/SkillLibrary.cs
// 격투가 MVP 스킬 3종(§5.3) 코드 폴백. 최종적으로는 data/skills/*.tres로 저작하는 것이 원칙이나,
// .tres 미할당 상태에서도 M1 손맛 검증이 가능하도록 기본값을 제공한다.
// Sonnet: 에디터에서 .tres로 만들어 Player에 할당하면 이 폴백은 사용되지 않는다.

namespace ChaosOfAI.Resources
{
    public static class SkillLibrary
    {
        // 1) 기본 강타: 단일/부채꼴, 마나 없음.
        public static SkillData Strike() => new SkillData
        {
            Id = "strike", DisplayName = "기본 강타",
            Targeting = SkillTargeting.ConeMelee,
            DamageMultiplier = 0f, ManaCost = 0f,
            RangeOverride = 2.2f, ConeHalfAngleOverride = 60f,
            KnockbackScale = 1f, HeavyHitStop = false,
            ActiveWindowStart = 0.10f, ActiveWindowEnd = 0.18f, Cooldown = 0.15f,
        };

        // 2) 분쇄: 강한 단일 타격 + 넉백 + 배율(디아2 Bash).
        public static SkillData Crush() => new SkillData
        {
            Id = "crush", DisplayName = "분쇄",
            Targeting = SkillTargeting.ConeMelee,
            DamageMultiplier = 0.8f, ManaCost = 3f,
            RangeOverride = 2.0f, ConeHalfAngleOverride = 35f,
            KnockbackScale = 2.2f, HeavyHitStop = true,
            ActiveWindowStart = 0.14f, ActiveWindowEnd = 0.22f, Cooldown = 0.35f,
        };

        // 3) 회전 격돌: 주위 광역 회전(디아2 Whirlwind 축소판).
        public static SkillData Spin() => new SkillData
        {
            Id = "spin", DisplayName = "회전 격돌",
            Targeting = SkillTargeting.RadialAoe,
            DamageMultiplier = 0.4f, ManaCost = 6f,
            RangeOverride = 2.6f, ConeHalfAngleOverride = 180f, // 전방위
            KnockbackScale = 0.6f, HeavyHitStop = false,
            ActiveWindowStart = 0.05f, ActiveWindowEnd = 0.55f, Cooldown = 0.2f,
        };
    }
}
