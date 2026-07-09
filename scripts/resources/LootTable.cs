// scripts/resources/LootTable.cs
// M4 드랍 테이블(§5.4) 코드 폴백 — SkillLibrary와 동일한 패턴. 최종적으로는 .tres 접사 풀로
// 확장 가능하지만, MVP 손맛 검증엔 이 정도 소품종으로 충분하다.
//
// 범위 축소(§5.4 대비 간소화, docs/reports 참고): 그리드 인벤토리·바닥 픽업 오브젝트 없이
// 처치 즉시 장비 보너스를 적용한다. "드랍 발생 → 즉시 장착" 순간 피드백으로 진행 루프
// (§5.5: 처치→경험치→레벨업→드랍습득→장비강화)의 핵심 체감만 먼저 검증하기 위함.

using System;
using System.Collections.Generic;

namespace ChaosOfAI.Resources
{
    public static class LootTable
    {
        private static readonly ItemData[] Pool =
        {
            new ItemData
            {
                Id = "rusty_gauntlet", DisplayName = "녹슨 건틀릿",
                Rarity = ItemRarity.Normal, Slot = EquipSlot.Weapon,
                BonusMinDamage = 1, BonusMaxDamage = 2,
            },
            new ItemData
            {
                Id = "scrap_plate", DisplayName = "고철 흉갑",
                Rarity = ItemRarity.Magic, Slot = EquipSlot.Armor,
                BonusMaxHp = 15, BonusDefense = 3,
            },
            new ItemData
            {
                Id = "glitch_core", DisplayName = "글리치 코어",
                Rarity = ItemRarity.Rare, Slot = EquipSlot.Trinket,
                BonusStrength = 3, BonusAttackRating = 10,
            },
        };

        /// <summary>몬스터의 DropChance를 굴려 드랍 여부와 아이템을 결정. 드랍 없으면 null.</summary>
        public static ItemData? Roll(float dropChance, Random rng)
        {
            if (rng.NextDouble() >= dropChance) return null;
            return Pool[rng.Next(Pool.Length)];
        }
    }
}
