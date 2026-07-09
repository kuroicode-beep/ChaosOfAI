// scripts/resources/EnemyData.cs
// 몬스터(고장난 AI) 데이터. 스탯/전투/드랍/AI 파라미터를 .tres로 정의(§5, §7.2).

using Godot;
using ChaosOfAI.Combat;

namespace ChaosOfAI.Resources
{
    [GlobalClass]
    public partial class EnemyData : Resource
    {
        [Export] public string Id { get; set; } = "";
        [Export] public string DisplayName { get; set; } = "";
        [Export] public int Level { get; set; } = 1;

        // 1차 스탯(§5.1)
        [Export] public int Strength { get; set; } = 10;
        [Export] public int Dexterity { get; set; } = 10;
        [Export] public int Vitality { get; set; } = 15;
        [Export] public int Energy { get; set; } = 5;

        // 공격 프로필
        [Export] public int MinDamage { get; set; } = 2;
        [Export] public int MaxDamage { get; set; } = 5;
        [Export] public float AttackCooldown { get; set; } = 1.2f;
        [Export] public float AttackRange { get; set; } = 1.8f;

        // 방어 감쇠(§4.2). 디아2와 달리 PRD가 별도 경감 항을 두므로 몬스터별로 노출. 기본 낮게.
        [Export] public float DamageReduction { get; set; } = 0f;

        // AI(§ M2)
        [Export] public float MoveSpeed { get; set; } = 3.0f;
        [Export] public float DetectionRange { get; set; } = 10f;

        // 보상
        [Export] public int XpReward { get; set; } = 10;
        [Export] public float DropChance { get; set; } = 0.2f;

        /// <summary>이 데이터로 런타임 CombatStats 인스턴스를 만든다.</summary>
        public CombatStats CreateStats()
        {
            return new CombatStats(Level,
                new PrimaryAttributes(Strength, Dexterity, Vitality, Energy));
        }

        public DefenderProfile CreateDefenderProfile(CombatStats stats)
            => new DefenderProfile(stats.Defense, DamageReduction, Level);
    }
}
