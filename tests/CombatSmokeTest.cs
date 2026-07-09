// tests/CombatSmokeTest.cs
// 엔진 통합 스모크 테스트: DamageCalculatorTests(순수 로직)와 달리 실제 Area3D/물리/
// NavigationAgent3D/EnemyAI 상태머신을 구동해 "Enemy가 Player를 실제로 때릴 수 있는가"를 검증한다.
// Enemy를 AttackRange 안에서 시작시켜 내비게이션 베이크 없이도 Idle→Chase→Attack 전이를 즉시 유도.
// --headless 자동화: 결과를 GD.Print 후 종료(0=성공, 1=실패/타임아웃).

using Godot;
using ChaosOfAI.Actors;

namespace ChaosOfAI.Tests
{
    public partial class CombatSmokeTest : Node3D
    {
        [Export] public PackedScene? PlayerScene;
        [Export] public PackedScene? EnemyScene;

        private Player? _player;
        private EnemyAI? _enemy;
        private float _elapsed;
        private const float TimeoutSeconds = 8f;
        private float _startHp = -1f;
        private bool _finished;

        public override void _Ready()
        {
            PlayerScene ??= GD.Load<PackedScene>("res://scenes/Player.tscn");
            EnemyScene ??= GD.Load<PackedScene>("res://scenes/Enemy.tscn");

            _player = PlayerScene!.Instantiate<Player>();
            AddChild(_player);
            _player.GlobalPosition = Vector3.Zero;

            _enemy = EnemyScene!.Instantiate<EnemyAI>();
            AddChild(_enemy);
            // glitch_drone AttackRange=1.8 → 처음부터 사거리 안에 배치해 내비 없이 즉시 Attack 전이 유도.
            _enemy.GlobalPosition = new Vector3(1.5f, 0, 0);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished || _player == null || _enemy == null) return;
            _elapsed += (float)delta;

            if (_startHp < 0f) _startHp = _player.Stats.CurrentHp;

            bool damaged = _player.Stats.CurrentHp < _startHp;
            bool timeout = _elapsed >= TimeoutSeconds;

            if (damaged || timeout)
            {
                _finished = true;
                GD.Print($"[CombatSmokeTest] {(damaged ? "PASS" : "FAIL")} " +
                    $"enemyState={_enemy.State} playerHp={_player.Stats.CurrentHp:F1}/{_player.Stats.MaxHp:F1} elapsed={_elapsed:F1}s");
                GetTree().Quit(damaged ? 0 : 1);
            }
        }
    }
}
