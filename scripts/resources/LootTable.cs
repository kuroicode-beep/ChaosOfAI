// scripts/resources/LootTable.cs
// M4 드랍 테이블(§5.4) 코드 폴백 — SkillLibrary와 동일한 패턴. 최종적으로는 .tres 접사 풀로
// 확장 가능하지만, MVP 손맛 검증엔 이 정도 소품종으로 충분하다.
//
// 처치 시 이 풀에서 굴린 아이템을 ItemPickupSpawner가 바닥에 스폰하고(§5.4),
// 플레이어가 걸어가 밟으면 Player.PickupItem으로 습득·적용된다(§5.5 진행 루프).
// 그리드 인벤토리 시각화는 아직 없고(목록형 패널), 즉시장착도 없다(습득 즉시 적용은 유지).

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

        /// <summary>저장 데이터 복원용: Id로 풀에서 아이템을 찾는다(없으면 null).</summary>
        public static ItemData? FindById(string id)
        {
            foreach (var item in Pool)
                if (item.Id == id) return item;
            return null;
        }
    }
}
