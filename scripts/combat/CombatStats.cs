// scripts/combat/CombatStats.cs
// 캐릭터 스탯 런타임 상태 + 파생 스탯 계산 (PRD §5.1, §5.2).
// 엔진 비의존 순수 클래스 → 단위 테스트 가능. Godot Node는 이 클래스를 소유(compose)한다.

using System;

namespace ChaosOfAI.Combat
{
    /// <summary>1차 스탯. STR/DEX/VIT/ENE (디아2 규격).</summary>
    public struct PrimaryAttributes
    {
        public int Strength;
        public int Dexterity;
        public int Vitality;
        public int Energy;

        public PrimaryAttributes(int str, int dex, int vit, int ene)
        {
            Strength = str; Dexterity = dex; Vitality = vit; Energy = ene;
        }
    }

    /// <summary>장비/버프로 합산된 보정치. 접사(affix) 합계가 여기로 모인다(§5.4).</summary>
    public struct EquipmentAggregate
    {
        public int BonusStrength;
        public int FlatDefense;
        public int FlatAttackRating;
        public int FlatMaxHp;
        public int FlatMinDamage;
        public int FlatMaxDamage;
    }

    /// <summary>
    /// 한 액터의 전투 스탯. 1차 스탯 + 장비 합산치로 파생 스탯을 계산하고
    /// 현재 HP/MP를 런타임 상태로 보유한다.
    /// </summary>
    public sealed class CombatStats
    {
        public int Level { get; private set; }
        public PrimaryAttributes Attributes;
        public EquipmentAggregate Equipment;

        public float CurrentHp { get; private set; }
        public float CurrentMp { get; private set; }

        public CombatStats(int level, PrimaryAttributes attributes)
        {
            Level = Math.Max(1, level);
            Attributes = attributes;
            Equipment = default;
            CurrentHp = MaxHp;
            CurrentMp = MaxMp;
        }

        // ── 유효 1차 스탯(장비 포함) ──────────────────────
        public int EffectiveStrength => Attributes.Strength + Equipment.BonusStrength;

        // ── 파생 스탯 (§5.2) ──────────────────────────────
        public float MaxHp =>
            BalanceConstants.BaseHp
            + Attributes.Vitality * BalanceConstants.HpPerVit
            + Level * BalanceConstants.HpPerLevel
            + Equipment.FlatMaxHp;

        public float MaxMp =>
            BalanceConstants.BaseMp
            + Attributes.Energy * BalanceConstants.MpPerEne;

        public float Defense =>
            Equipment.FlatDefense
            + Attributes.Dexterity * BalanceConstants.DefensePerDex;

        public float AttackRating =>
            BalanceConstants.BaseAttackRating
            + Attributes.Dexterity * BalanceConstants.ArPerDex
            + Equipment.FlatAttackRating;

        public bool IsAlive => CurrentHp > 0f;

        // ── 상태 변경 ─────────────────────────────────────
        /// <summary>데미지 적용. 실제 깎인 양을 반환(오버킬 제외).</summary>
        public float ApplyDamage(float amount)
        {
            if (amount <= 0f) return 0f;
            float before = CurrentHp;
            CurrentHp = Math.Max(0f, CurrentHp - amount);
            return before - CurrentHp;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }

        public bool SpendMp(float amount)
        {
            if (amount <= 0f) return true;
            if (CurrentMp < amount) return false;
            CurrentMp -= amount;
            return true;
        }

        public void RestoreMp(float amount)
        {
            if (amount <= 0f) return;
            CurrentMp = Math.Min(MaxMp, CurrentMp + amount);
        }

        /// <summary>레벨업. HP/MP 비율 유지 후 최대치 재계산.</summary>
        public void LevelUp(int levels = 1)
        {
            if (levels <= 0) return;
            float hpRatio = MaxHp > 0 ? CurrentHp / MaxHp : 1f;
            float mpRatio = MaxMp > 0 ? CurrentMp / MaxMp : 1f;
            Level += levels;
            CurrentHp = MaxHp * hpRatio;
            CurrentMp = MaxMp * mpRatio;
        }

        public void FullRestore()
        {
            CurrentHp = MaxHp;
            CurrentMp = MaxMp;
        }
    }
}
