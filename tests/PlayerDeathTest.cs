// tests/PlayerDeathTest.cs
// 발견1 수정 회귀: 플레이어에 치명 데미지를 주면 IsAlive=false로 전환되고, 이후 추가 피격이
// 예외 없이 무시되며, _PhysicsProcess가 사망 상태에서 안전하게 도는지 엔진에서 검증한다.
// HUD 없이도 Die()의 ShowDeath 호출이 null-safe해야 한다.

using Godot;
using ChaosOfAI.Actors;
using ChaosOfAI.Combat;

namespace ChaosOfAI.Tests
{
    public partial class PlayerDeathTest : Node3D
    {
        private Player? _player;
        private int _phase;
        private int _frames;
        private bool _finished;

        public override void _Ready()
        {
            var scene = GD.Load<PackedScene>("res://scenes/Player.tscn");
            _player = scene.Instantiate<Player>();
            AddChild(_player);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished || _player == null) return;
            _frames++;
            if (_frames < 2) return; // _Ready 완료 후 한 프레임 대기

            if (_phase == 0)
            {
                bool aliveBefore = _player.IsAlive;
                // 치명타 주입.
                _player.ReceiveHit(new DamageResult(true, false, 999999f), Vector3.Forward, 0f);
                bool deadAfter = !_player.IsAlive;

                // 사망 후 추가 피격이 예외 없이 무시되는지.
                _player.ReceiveHit(new DamageResult(true, true, 999999f), Vector3.Forward, 5f);

                if (!(aliveBefore && deadAfter))
                {
                    Finish(false, $"aliveBefore={aliveBefore} deadAfter={deadAfter}");
                    return;
                }
                _phase = 1;
                return;
            }

            // 사망 상태에서 몇 프레임 더 돌려 _PhysicsProcess 안전성 확인.
            if (_phase == 1)
            {
                if (_frames > 12)
                    Finish(!_player.IsAlive, $"stillDead={!_player.IsAlive} hp={_player.Stats.CurrentHp:F1}");
            }
        }

        private void Finish(bool pass, string detail)
        {
            _finished = true;
            GD.Print($"[PlayerDeathTest] {(pass ? "PASS" : "FAIL")} {detail}");
            GetTree().Quit(pass ? 0 : 1);
        }
    }
}
