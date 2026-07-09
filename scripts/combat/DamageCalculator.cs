// scripts/combat/DamageCalculator.cs
// ★ 핵심 로직: 데미지/명중/크리티컬 계산 (PRD §4.2, 리스크 최우선 확정 대상) ★
// 엔진 비의존 순수 static → 단위 테스트로 규격을 못 박는다(tests/DamageCalculatorTests 참고).
// RNG를 주입받아 결정론적으로 검증 가능하게 한다.

using System;

namespace ChaosOfAI.Combat
{
    /// <summary>공격 측 입력값. 스킬/스탯이 조합돼 여기로 전달된다.</summary>
    public readonly struct AttackProfile
    {
        public readonly int MinDamage;      // 무기/주먹 최소
        public readonly int MaxDamage;      // 무기/주먹 최대
        public readonly int Strength;       // 스탯 보너스 산출용
        public readonly float SkillMultiplier; // 스킬/버프 배율 (0 = 배율 없음 → ×1)
        public readonly float AttackRating; // 명중 계산용
        public readonly int Level;
        public readonly float CritChance;   // 0~1

        public AttackProfile(int minDamage, int maxDamage, int strength,
            float skillMultiplier, float attackRating, int level, float critChance)
        {
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            Strength = strength;
            SkillMultiplier = skillMultiplier;
            AttackRating = attackRating;
            Level = level;
            CritChance = critChance;
        }
    }

    /// <summary>방어 측 입력값.</summary>
    public readonly struct DefenderProfile
    {
        public readonly float Defense;         // 명중 계산용 (AR vs DEF)
        public readonly float DamageReduction; // 0~1, 최종 데미지 경감(§4.2)
        public readonly int Level;

        public DefenderProfile(float defense, float damageReduction, int level)
        {
            Defense = defense;
            DamageReduction = damageReduction;
            Level = level;
        }
    }

    public readonly struct DamageResult
    {
        public readonly bool Hit;
        public readonly bool IsCritical;
        public readonly float Amount; // Hit=false면 0

        public DamageResult(bool hit, bool isCritical, float amount)
        {
            Hit = hit; IsCritical = isCritical; Amount = amount;
        }

        public static readonly DamageResult Miss = new DamageResult(false, false, 0f);
    }

    public static class DamageCalculator
    {
        /// <summary>
        /// 명중률 = 2·AR/(AR+DEF) · myLevel/(myLevel+enemyLevel), clamp[5%,95%] (§4.2).
        /// </summary>
        public static float ComputeHitChance(float attackRating, float defense,
            int attackerLevel, int defenderLevel)
        {
            float ar = Math.Max(1f, attackRating);
            float def = Math.Max(0f, defense);
            int al = Math.Max(1, attackerLevel);
            int dl = Math.Max(1, defenderLevel);

            float raw = 2f * ar / (ar + def) * ((float)al / (al + dl));
            return Clamp(raw, BalanceConstants.MinHitChance, BalanceConstants.MaxHitChance);
        }

        /// <summary>
        /// 최종 데미지 = (Random(min,max) + floor(STR/K)) × (1 + 스킬배율) × (1 - 방어감쇠) (§4.2).
        /// 크리티컬 성립 시 × CritMultiplier. 명중 실패 시 DamageResult.Miss.
        /// </summary>
        public static DamageResult Resolve(in AttackProfile atk, in DefenderProfile def, Random rng)
        {
            // 1) 명중 판정
            float hitChance = ComputeHitChance(atk.AttackRating, def.Defense, atk.Level, def.Level);
            if (rng.NextDouble() >= hitChance)
                return DamageResult.Miss;

            // 2) 기본 데미지 = Random(min,max) + 스탯 보너스
            int min = Math.Min(atk.MinDamage, atk.MaxDamage);
            int max = Math.Max(atk.MinDamage, atk.MaxDamage);
            int rolled = min == max ? min : rng.Next(min, max + 1);
            int statBonus = atk.Strength / BalanceConstants.StrPerDamage; // floor(STR/K)
            float baseDamage = rolled + statBonus;

            // 3) 스킬/버프 배율, 방어 감쇠
            float multiplier = 1f + Math.Max(0f, atk.SkillMultiplier);
            float reduction = Clamp(def.DamageReduction, 0f, BalanceConstants.MaxDamageReduction);
            float damage = baseDamage * multiplier * (1f - reduction);

            // 4) 크리티컬
            bool crit = rng.NextDouble() < atk.CritChance;
            if (crit) damage *= BalanceConstants.CritMultiplier;

            if (damage < 1f) damage = 1f; // 최소 1 보장
            return new DamageResult(true, crit, damage);
        }

        private static float Clamp(float v, float lo, float hi)
            => v < lo ? lo : (v > hi ? hi : v);
    }
}
