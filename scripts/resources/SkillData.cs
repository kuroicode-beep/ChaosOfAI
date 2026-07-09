// scripts/resources/SkillData.cs
// 스킬 데이터(§5.3). 코드가 아니라 .tres 리소스로 정의 → 밸런싱/추가를 코드 수정 없이(§7.2).
// data/skills/*.tres 로 저작.

using Godot;

namespace ChaosOfAI.Resources
{
    public enum SkillTargeting
    {
        ConeMelee, // 전방 부채꼴 단일/다중 (기본 강타, 분쇄)
        RadialAoe  // 주위 광역 회전 (회전 격돌)
    }

    [GlobalClass]
    public partial class SkillData : Resource
    {
        [Export] public string Id { get; set; } = "strike";
        [Export] public string DisplayName { get; set; } = "기본 강타";
        [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

        [Export] public SkillTargeting Targeting { get; set; } = SkillTargeting.ConeMelee;

        // 데미지 배율: 최종 = 기본 × (1 + DamageMultiplier). 0 = 순수 기본 데미지.
        [Export] public float DamageMultiplier { get; set; } = 0f;
        [Export] public float ManaCost { get; set; } = 0f;

        // 히트박스 형상 오버라이드(0 이하이면 MeleeHitbox 기본값 사용)
        [Export] public float RangeOverride { get; set; } = 0f;
        [Export] public float ConeHalfAngleOverride { get; set; } = 0f;

        // 타격감(§4.3)
        [Export] public float KnockbackScale { get; set; } = 1f;
        [Export] public bool HeavyHitStop { get; set; } = false;

        // 액티브 윈도 타이밍(초): 애니 시작 후 언제부터 언제까지 타격 판정을 켜는가.
        [Export] public float ActiveWindowStart { get; set; } = 0.1f;
        [Export] public float ActiveWindowEnd { get; set; } = 0.2f;

        // 후딜/쿨다운
        [Export] public float Cooldown { get; set; } = 0f;
    }
}
