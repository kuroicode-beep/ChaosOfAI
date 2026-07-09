// tests/ProgressionTests.cs
// M3(경험치/레벨업)·M4(드랍/장비 보너스) 회귀 검증. DamageCalculatorTests와 동일한 패턴
// (엔진 비의존 순수 로직 — PlayerProgression/CombatStats/LootTable은 Node가 아니므로
// Player를 씬에 인스턴스화하지 않고도 직접 검증 가능).

using System;
using Godot;
using ChaosOfAI.Combat;
using ChaosOfAI.Resources;

namespace ChaosOfAI.Tests
{
    public partial class ProgressionTests : Node
    {
        private int _pass, _fail;

        [Export] public bool QuitOnFinish = false;

        public override void _Ready()
        {
            RunAll();
            if (QuitOnFinish)
                GetTree().Quit(_fail == 0 ? 0 : 1);
        }

        public void RunAll()
        {
            _pass = _fail = 0;

            XpToNextLevel_MatchesFormula();
            GrantXp_ExactThreshold_LevelsUpOnce();
            GrantXp_Overflow_CascadesMultipleLevels();
            SpendStatPoint_IncrementsAttributeAndDrainsPool();
            SpendStatPoint_FailsWhenPoolEmpty();
            EquipmentBonus_AffectsDerivedStats();
            LootTable_ZeroChance_NeverDrops();
            LootTable_CertainChance_AlwaysDropsFromPool();

            GD.Print($"[ProgressionTests] PASS={_pass} FAIL={_fail}");
        }

        private void Check(bool cond, string name)
        {
            if (cond) { _pass++; }
            else { _fail++; GD.PushError($"[FAIL] {name}"); GD.Print($"[FAIL] {name}"); }
        }

        private static CombatStats NewStats(int level = 1)
            => new CombatStats(level, new PrimaryAttributes(str: 10, dex: 10, vit: 10, ene: 10));

        private void XpToNextLevel_MatchesFormula()
        {
            var stats = NewStats(3);
            var prog = new PlayerProgression(stats);
            int expected = BalanceConstants.XpCurveBase + (3 - 1) * BalanceConstants.XpCurveGrowth;
            Check(prog.XpToNextLevel == expected, nameof(XpToNextLevel_MatchesFormula));
        }

        private void GrantXp_ExactThreshold_LevelsUpOnce()
        {
            var stats = NewStats(1);
            var prog = new PlayerProgression(stats);
            int need = prog.XpToNextLevel; // Base(레벨1)
            prog.GrantXp(need);
            Check(stats.Level == 2 && prog.CurrentXp == 0, nameof(GrantXp_ExactThreshold_LevelsUpOnce) + ".level");
            Check(prog.UnspentStatPoints == BalanceConstants.StatPointsPerLevel, nameof(GrantXp_ExactThreshold_LevelsUpOnce) + ".statPoints");
            Check(prog.UnspentSkillPoints == BalanceConstants.SkillPointsPerLevel, nameof(GrantXp_ExactThreshold_LevelsUpOnce) + ".skillPoints");
        }

        private void GrantXp_Overflow_CascadesMultipleLevels()
        {
            var stats = NewStats(1);
            var prog = new PlayerProgression(stats);
            // 레벨1→2에 필요한 양 + 레벨2→3에 필요한 양을 한 번에 지급 → 레벨 3까지 연쇄 상승.
            int need1 = BalanceConstants.XpCurveBase + 0 * BalanceConstants.XpCurveGrowth;
            int need2 = BalanceConstants.XpCurveBase + 1 * BalanceConstants.XpCurveGrowth;
            prog.GrantXp(need1 + need2);
            Check(stats.Level == 3, nameof(GrantXp_Overflow_CascadesMultipleLevels) + ".level");
            Check(prog.UnspentStatPoints == BalanceConstants.StatPointsPerLevel * 2, nameof(GrantXp_Overflow_CascadesMultipleLevels) + ".points");
        }

        private void SpendStatPoint_IncrementsAttributeAndDrainsPool()
        {
            var stats = NewStats(1);
            var prog = new PlayerProgression(stats);
            prog.GrantXp(prog.XpToNextLevel); // 포인트 확보
            int before = stats.Attributes.Strength;
            int pointsBefore = prog.UnspentStatPoints;

            bool ok = prog.SpendStatPoint(StatKind.Strength);

            Check(ok, nameof(SpendStatPoint_IncrementsAttributeAndDrainsPool) + ".returnsTrue");
            Check(stats.Attributes.Strength == before + 1, nameof(SpendStatPoint_IncrementsAttributeAndDrainsPool) + ".strIncremented");
            Check(prog.UnspentStatPoints == pointsBefore - 1, nameof(SpendStatPoint_IncrementsAttributeAndDrainsPool) + ".poolDrained");
        }

        private void SpendStatPoint_FailsWhenPoolEmpty()
        {
            var stats = NewStats(1); // 레벨업 없음 → 포인트 0
            var prog = new PlayerProgression(stats);
            bool ok = prog.SpendStatPoint(StatKind.Vitality);
            Check(!ok, nameof(SpendStatPoint_FailsWhenPoolEmpty));
        }

        private void EquipmentBonus_AffectsDerivedStats()
        {
            var stats = NewStats(1);
            float hpBefore = stats.MaxHp;
            float defBefore = stats.Defense;

            // Player.PickupItem과 동일한 합산 방식(구조체 복사-수정-대입).
            var eq = stats.Equipment;
            eq.FlatMaxHp += 15;
            eq.FlatDefense += 3;
            stats.Equipment = eq;

            Check(Math.Abs(stats.MaxHp - (hpBefore + 15)) < 0.01f, nameof(EquipmentBonus_AffectsDerivedStats) + ".hp");
            Check(Math.Abs(stats.Defense - (defBefore + 3)) < 0.01f, nameof(EquipmentBonus_AffectsDerivedStats) + ".defense");
        }

        private void LootTable_ZeroChance_NeverDrops()
        {
            var rng = new Random(42);
            bool anyDrop = false;
            for (int i = 0; i < 200; i++)
                if (LootTable.Roll(0f, rng) != null) { anyDrop = true; break; }
            Check(!anyDrop, nameof(LootTable_ZeroChance_NeverDrops));
        }

        private void LootTable_CertainChance_AlwaysDropsFromPool()
        {
            var rng = new Random(7);
            bool allDropped = true;
            for (int i = 0; i < 50; i++)
            {
                var item = LootTable.Roll(1f, rng);
                if (item == null || string.IsNullOrEmpty(item.Id)) { allDropped = false; break; }
            }
            Check(allDropped, nameof(LootTable_CertainChance_AlwaysDropsFromPool));
        }
    }
}
