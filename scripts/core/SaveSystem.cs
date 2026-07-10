// scripts/core/SaveSystem.cs
// 진행도 영속화(§9 열린질문 "저장/세이브 구조" 응답). user://save.json에 JSON 저장.
// 던전 재입장(R 재시작)마다 처음부터 시작하던 것을, 레벨/스탯/장비/인벤토리/스킬강화가
// 이어지도록 최소 구현. 던전 자체 상태(적 위치·클리어 여부)는 저장하지 않는다(범위 밖).

using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace ChaosOfAI.Core
{
    /// <summary>순수 데이터 컨테이너. Godot 타입에 의존하지 않아 System.Text.Json으로 직렬화 가능.</summary>
    public sealed class SaveData
    {
        public int Level { get; set; } = 1;
        public int CurrentXp { get; set; }
        public int UnspentStatPoints { get; set; }
        public int UnspentSkillPoints { get; set; }

        public int Str { get; set; }
        public int Dex { get; set; }
        public int Vit { get; set; }
        public int Ene { get; set; }

        public int EqBonusStrength { get; set; }
        public int EqFlatDefense { get; set; }
        public int EqFlatAttackRating { get; set; }
        public int EqFlatMaxHp { get; set; }
        public int EqFlatMinDamage { get; set; }
        public int EqFlatMaxDamage { get; set; }

        public List<string> InventoryItemIds { get; set; } = new();
        public Dictionary<string, int> SkillUpgrades { get; set; } = new();
    }

    public static class SaveSystem
    {
        // 테스트가 실제 사용자 세이브(save.json)를 절대 건드리지 않도록 파일명을 교체 가능하게 둔다.
        // (tests/SaveLoadTest가 "test_save.json"으로 바꿔 격리 실행 후 원복)
        public static string FileName { get; set; } = "save.json";

        public static void Save(SaveData data)
        {
            string json = JsonSerializer.Serialize(data);
            using var f = FileAccess.Open($"user://{FileName}", FileAccess.ModeFlags.Write);
            f?.StoreString(json);
        }

        /// <summary>저장 파일이 없거나 손상됐으면 null(새 게임으로 취급).</summary>
        public static SaveData? Load()
        {
            using var dir = DirAccess.Open("user://");
            if (dir == null || !dir.FileExists(FileName)) return null;

            using var f = FileAccess.Open($"user://{FileName}", FileAccess.ModeFlags.Read);
            if (f == null) return null;

            try { return JsonSerializer.Deserialize<SaveData>(f.GetAsText()); }
            catch { return null; }
        }

        public static void DeleteSave()
        {
            using var dir = DirAccess.Open("user://");
            if (dir != null && dir.FileExists(FileName))
                dir.Remove(FileName);
        }
    }
}
