// tests/SaveLoadTest.cs
// 저장/불러오기 회귀: 완전히 격리된 테스트 파일(test_save.json)로만 동작 — 사용자의 실제
// 플레이 진행도(user://save.json)는 절대 건드리지 않는다(Player.ForceEnableSaveForTests +
// SaveSystem.FileName 오버라이드로 이중 격리).
//
// 시나리오: Player A로 레벨업/스탯분배/스킬강화/아이템 획득 → 새 Player B를 스폰해 값이
// 그대로 이어지는지 확인.

using Godot;
using ChaosOfAI.Actors;
using ChaosOfAI.Core;
using ChaosOfAI.Combat;
using ChaosOfAI.Resources;

namespace ChaosOfAI.Tests
{
    public partial class SaveLoadTest : Node3D
    {
        private const string TestFile = "test_save.json";
        private Player? _playerA;
        private Player? _playerB;
        private int _phase;
        private int _frames;
        private bool _finished;

        public override void _Ready()
        {
            // 이중 격리: 전용 파일명 + 강제 활성화. 끝나면 반드시 원복(_Finish에서).
            SaveSystem.FileName = TestFile;
            Player.ForceEnableSaveForTests = true;
            SaveSystem.DeleteSave(); // 이전 실행 잔여물 제거(격리 파일이므로 안전)

            _playerA = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_playerA);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished) return;
            _frames++;
            if (_frames < 3 || _playerA == null) return;

            if (_phase == 0)
            {
                // 레벨 2까지 XP 지급 → 스탯/스킬 포인트 확보.
                _playerA.Progression.GrantXp(_playerA.Progression.XpToNextLevel);
                _playerA.PickupItem(LootTable.FindById("glitch_core")!);

                bool statSpent = _playerA.Progression.UnspentStatPoints > 0
                    && _playerA.SpendStat(StatKind.Strength); // 실제 입력 핸들러가 타는 것과 동일한 공개 API

                int skillPointsBefore = _playerA.Progression.UnspentSkillPoints;
                if (skillPointsBefore > 0) _playerA.UpgradeSkill(_playerA.StrikeSkill); // Player의 실제 강화 메서드
                bool skillSpent = _playerA.Progression.UnspentSkillPoints == skillPointsBefore - 1;

                if (!statSpent || !skillSpent)
                {
                    Finish(false, $"셋업 실패 statSpent={statSpent} skillSpent={skillSpent}");
                    return;
                }

                _expectedLevel = _playerA.Stats.Level;
                _expectedStr = _playerA.Stats.Attributes.Strength;
                _expectedDmg = _playerA.StrikeSkill!.DamageMultiplier;
                _expectedInvCount = _playerA.Inventory.Count;

                _playerA.QueueFree();
                _phase = 1;
                _frames = 0;
                return;
            }

            if (_phase == 1)
            {
                if (_frames < 3) return; // QueueFree 처리 대기
                _playerB = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
                AddChild(_playerB);
                _phase = 2;
                _frames = 0;
                return;
            }

            if (_phase == 2)
            {
                if (_frames < 2 || _playerB == null) return; // _Ready(로드) 완료 대기

                bool levelOk = _playerB.Stats.Level == _expectedLevel;
                bool strOk = _playerB.Stats.Attributes.Strength == _expectedStr;
                bool dmgOk = Mathf.Abs(_playerB.StrikeSkill!.DamageMultiplier - _expectedDmg) < 0.001f;
                bool invOk = _playerB.Inventory.Count == _expectedInvCount;

                bool pass = levelOk && strOk && dmgOk && invOk;
                Finish(pass, $"level={_playerB.Stats.Level}(exp{_expectedLevel}) str={_playerB.Stats.Attributes.Strength}(exp{_expectedStr}) " +
                    $"dmg={_playerB.StrikeSkill!.DamageMultiplier:F2}(exp{_expectedDmg:F2}) inv={_playerB.Inventory.Count}(exp{_expectedInvCount})");
            }
        }

        private int _expectedLevel, _expectedStr, _expectedInvCount;
        private float _expectedDmg;

        private void Finish(bool pass, string detail)
        {
            _finished = true;
            GD.Print($"[SaveLoadTest] {(pass ? "PASS" : "FAIL")} {detail}");

            // 격리 파일 정리 + 전역 플래그 원복(다음 실행/다른 테스트 오염 방지).
            SaveSystem.DeleteSave();
            SaveSystem.FileName = "save.json";
            Player.ForceEnableSaveForTests = false;

            GetTree().Quit(pass ? 0 : 1);
        }
    }
}
