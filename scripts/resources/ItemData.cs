// scripts/resources/ItemData.cs
// 아이템 데이터(§5.4). 등급/슬롯/접사(affix)를 .tres로 정의. 등급별 발광 색상은 가독성 위해 라벨과 병행.

using Godot;

namespace ChaosOfAI.Resources
{
    public enum ItemRarity
    {
        Normal, // 일반 (흰색)
        Magic,  // 매직 (파랑)
        Rare,   // 레어 (노랑)
        Unique  // 유니크 (금색) — MVP 이후 확장
    }

    public enum EquipSlot
    {
        None,
        Weapon,  // 주먹 강화 건틀릿류
        Armor,
        Trinket
    }

    [GlobalClass]
    public partial class ItemData : Resource
    {
        [Export] public string Id { get; set; } = "";
        [Export] public string DisplayName { get; set; } = "";
        [Export] public ItemRarity Rarity { get; set; } = ItemRarity.Normal;
        [Export] public EquipSlot Slot { get; set; } = EquipSlot.None;

        // 인벤토리 그리드 점유(디아2식). MVP는 1x1~2x3 정도.
        [Export] public int GridWidth { get; set; } = 1;
        [Export] public int GridHeight { get; set; } = 1;

        // 기본 접사(§5.4) — 장착 시 CombatStats.EquipmentAggregate로 합산.
        [Export] public int BonusMinDamage { get; set; } = 0;
        [Export] public int BonusMaxDamage { get; set; } = 0;
        [Export] public int BonusMaxHp { get; set; } = 0;
        [Export] public int BonusStrength { get; set; } = 0;
        [Export] public int BonusDefense { get; set; } = 0;
        [Export] public int BonusAttackRating { get; set; } = 0;

        /// <summary>등급별 UI 표시 색상(발광 라벨용). 색상만으로 구분하지 않고 라벨 텍스트와 병행.</summary>
        public Color RarityColor => Rarity switch
        {
            ItemRarity.Normal => new Color(0.90f, 0.90f, 0.90f),
            ItemRarity.Magic  => new Color(0.35f, 0.55f, 1.00f),
            ItemRarity.Rare   => new Color(1.00f, 0.85f, 0.20f),
            ItemRarity.Unique => new Color(0.80f, 0.55f, 0.15f),
            _ => Colors.White
        };
    }
}
