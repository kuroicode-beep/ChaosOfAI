// tests/MovementTest.cs
// "조작이 되는가" 회귀: Main 씬을 띄워 내비 베이크 후, 플레이어의 NavigationAgent3D에 목표를 주고
// 실제로 이동하는지(위치가 유의미하게 변하는지) 검증한다. 클릭 이동 파이프라인의 핵심(내비→HandleMovement).
// headless로도 NavigationServer가 동작하므로 자동화 가능.

using Godot;
using ChaosOfAI.Actors;

namespace ChaosOfAI.Tests
{
    public partial class MovementTest : Node
    {
        private Player? _player;
        private Vector3 _startPos;
        private int _frames;
        private bool _targetSet;
        private bool _finished;

        public override void _Ready()
        {
            var main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate();
            AddChild(main);
            _player = main.GetNodeOrNull<Player>("Player");
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished) return;
            _frames++;

            if (_player == null) { Finish(false, "player null"); return; }

            if (!_targetSet && _frames >= 5)
            {
                _startPos = _player.GlobalPosition;
                _player.MoveTo(_startPos + new Vector3(8, 0, 0)); // 클릭 이동 시뮬레이션
                _targetSet = true;
                return;
            }

            if (_targetSet && _frames >= 100)
            {
                float moved = _player.GlobalPosition.DistanceTo(_startPos);
                Finish(moved > 1.0f, $"moved={moved:F2} from={_startPos} to={_player.GlobalPosition}");
            }
        }

        private void Finish(bool pass, string detail)
        {
            _finished = true;
            GD.Print($"[MovementTest] {(pass ? "PASS" : "FAIL")} {detail}");
            GetTree().Quit(pass ? 0 : 1);
        }
    }
}
