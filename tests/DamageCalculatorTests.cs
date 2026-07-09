// tests/DamageCalculatorTests.cs
// 핵심 데미지/명중 규격(§4.2) 회귀 검증. 외부 테스트 프레임워크 없이 동작하도록
// Node로 만들어 씬에 붙이거나 SceneTree에서 RunAll()을 호출해 GD.Print로 결과 확인.
// Sonnet: M1 밸런싱 변경 시 이 테스트가 통과하는지 먼저 확인할 것.

using System;
using Godot;
using ChaosOfAI.Combat;

namespace ChaosOfAI.Tests
{
    public partial class DamageCalculatorTests : Node
    {
        private int _pass, _fail;

        // --headless 자동화 실행 시 결과 출력 후 즉시 종료(0=전부 통과, 1=실패 있음).
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

            HitChance_Clamps();
            HitChance_EqualStats_IsHalfIsh();
            Damage_StatBonus_UsesFloorStrOverK();
            Damage_SkillMultiplier_Applies();
            Damage_Reduction_Applies();
            Damage_Crit_Doubles();
            Miss_Returns_Zero();

            GD.Print($"[DamageCalculatorTests] PASS={_pass} FAIL={_fail}");
        }

        private void Check(bool cond, string name)
        {
            if (cond) { _pass++; }
            else { _fail++; GD.PushError($"[FAIL] {name}"); GD.Print($"[FAIL] {name}"); }
        }

        private static bool Approx(float a, float b, float eps = 0.001f) => Math.Abs(a - b) <= eps;

        private void HitChance_Clamps()
        {
            // 압도적 AR → 상한 95%
            float hi = DamageCalculator.ComputeHitChance(10000, 1, 50, 1);
            Check(Approx(hi, 0.95f), nameof(HitChance_Clamps) + ".max");
            // 압도적 DEF/레벨 → 하한 5%
            float lo = DamageCalculator.ComputeHitChance(1, 10000, 1, 500);
            Check(Approx(lo, 0.05f), nameof(HitChance_Clamps) + ".min");
        }

        private void HitChance_EqualStats_IsHalfIsh()
        {
            // AR==DEF, 레벨 동일 → 2·0.5·0.5 = 0.5
            float h = DamageCalculator.ComputeHitChance(100, 100, 10, 10);
            Check(Approx(h, 0.5f), nameof(HitChance_EqualStats_IsHalfIsh));
        }

        // 명중 100%(AR 압도) + 크리 0% + min==max로 랜덤 제거 → 결정론적 데미지 검증
        private AttackProfile FixedAtk(int dmg, int str, float mult, float critChance = 0f)
            => new AttackProfile(dmg, dmg, str, mult, attackRating: 100000, level: 50, critChance: critChance);

        private DefenderProfile Def(float reduction)
            => new DefenderProfile(defense: 0, damageReduction: reduction, level: 1);

        private void Damage_StatBonus_UsesFloorStrOverK()
        {
            // dmg=10, STR=25, K=10 → bonus=floor(25/10)=2 → 12
            var r = DamageCalculator.Resolve(FixedAtk(10, 25, 0f), Def(0f), new Random(1));
            Check(r.Hit && Approx(r.Amount, 12f), nameof(Damage_StatBonus_UsesFloorStrOverK));
        }

        private void Damage_SkillMultiplier_Applies()
        {
            // base=10(+0 STR bonus, STR=0) × (1+0.5)=15
            var r = DamageCalculator.Resolve(FixedAtk(10, 0, 0.5f), Def(0f), new Random(2));
            Check(r.Hit && Approx(r.Amount, 15f), nameof(Damage_SkillMultiplier_Applies));
        }

        private void Damage_Reduction_Applies()
        {
            // base=10 × (1-0.25)=7.5
            var r = DamageCalculator.Resolve(FixedAtk(10, 0, 0f), Def(0.25f), new Random(3));
            Check(r.Hit && Approx(r.Amount, 7.5f), nameof(Damage_Reduction_Applies));
        }

        private void Damage_Crit_Doubles()
        {
            // 크리 100% → base=10 × 2 = 20
            var r = DamageCalculator.Resolve(FixedAtk(10, 0, 0f, critChance: 1f), Def(0f), new Random(4));
            Check(r.Hit && r.IsCritical && Approx(r.Amount, 20f), nameof(Damage_Crit_Doubles));
        }

        private void Miss_Returns_Zero()
        {
            // AR 극소 + DEF 극대 → 5% 근처. 수백 회 중 미스가 반드시 존재하고 미스 시 Amount=0.
            var atk = new AttackProfile(10, 10, 0, 0f, attackRating: 1, level: 1, critChance: 0f);
            var def = new DefenderProfile(defense: 100000, damageReduction: 0f, level: 500);
            var rng = new Random(5);
            bool sawMiss = false;
            for (int i = 0; i < 500; i++)
            {
                var r = DamageCalculator.Resolve(atk, def, rng);
                if (!r.Hit) { sawMiss = true; Check(Approx(r.Amount, 0f), "Miss.zeroAmount"); break; }
            }
            Check(sawMiss, nameof(Miss_Returns_Zero));
        }
    }
}
