// tests/DungeonSmokeTest.cs
// M5 통합 회귀: Dungeon.tscn을 실제 엔진에서 구동해
//  1) 플레이어·보스가 정상 스폰되는지
//  2) 보스를 강제 처치하면 Died→ShowVictory 경로로 클리어 패널이 뜨는지
// 를 검증한다. headless 자동화(0=성공, 1=실패).

using Godot;
using ChaosOfAI.Actors;
using ChaosOfAI.Combat;
using ChaosOfAI.UI;

namespace ChaosOfAI.Tests
{
    public partial class DungeonSmokeTest : Node
    {
        private Node? _dungeon;
        private int _frames;
        private bool _killed;
        private bool _finished;

        public override void _Ready()
        {
            _dungeon = GD.Load<PackedScene>("res://scenes/Dungeon.tscn").Instantiate();
            AddChild(_dungeon);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished || _dungeon == null) return;
            _frames++;
            if (_frames < 5) return; // 빌더 스폰 완료 대기

            if (!_killed)
            {
                Player? player = null;
                EnemyAI? boss = null;
                foreach (Node child in _dungeon.GetChildren())
                {
                    if (child is Player p) player = p;
                    else if (child is EnemyAI e && e.Data?.Id == "overmind_herald") boss = e;
                }

                if (player == null || boss == null)
                {
                    Finish(false, $"spawn 실패 player={player != null} boss={boss != null}");
                    return;
                }

                boss.ReceiveHit(new DamageResult(true, false, 999999f), Vector3.Forward, 0f);
                _killed = true;
                return;
            }

            if (_frames >= 10)
            {
                var hud = GetTree().GetFirstNodeInGroup("hud") as Hud;
                bool victory = hud?.VictoryVisible ?? false;
                Finish(victory, $"victoryPanel={victory}");
            }
        }

        private void Finish(bool pass, string detail)
        {
            _finished = true;
            GD.Print($"[DungeonSmokeTest] {(pass ? "PASS" : "FAIL")} {detail}");
            GetTree().Quit(pass ? 0 : 1);
        }
    }
}
