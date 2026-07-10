// tests/CombatSmokeTest.cs
// 엔진 통합 스모크 테스트: DamageCalculatorTests(순수 로직)와 달리 실제 Area3D/물리/
// NavigationAgent3D/EnemyAI 상태머신을 구동해 검증한다.
//   Phase 1: Enemy가 Player를 실제로 때릴 수 있는가(AttackRange 안에서 시작 → 즉시 Attack 전이).
//   Phase 2: Enemy를 처치했을 때 Die()→GrantRewardsToPlayer()(XP 지급/드랍)가 예외 없이 도는가.
// --headless 자동화: 결과를 GD.Print 후 종료(0=성공, 1=실패/타임아웃).

using Godot;
using ChaosOfAI.Actors;
using ChaosOfAI.Combat;

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
        private bool _hitConfirmed;
        private bool _killTriggered;
        private int _framesSinceKill;
        private bool _finished;

        public override void _Ready()
        {
            PlayerScene ??= GD.Load<PackedScene>("res://scenes/Player.tscn");
            EnemyScene ??= GD.Load<PackedScene>("res://scenes/Enemy.tscn");

            // ★ Position은 AddChild "전"에 설정한다. 후에 설정하면 물리 서버가 그 프레임엔 아직
            // 스폰 기본 위치로 알고 있어, 두 CharacterBody3D가 원점 부근에서 겹친 것으로 오판해
            // MoveAndSlide가 서로 밀어내는 레이스가 생긴다(ItemPickupTest 원인 규명 중 발견).
            _player = PlayerScene!.Instantiate<Player>();
            _player.Position = Vector3.Zero;
            AddChild(_player);

            _enemy = EnemyScene!.Instantiate<EnemyAI>();
            // glitch_drone AttackRange=1.8 → 처음부터 사거리 안에 배치해 내비 없이 즉시 Attack 전이 유도.
            _enemy.Position = new Vector3(1.5f, 0, 0);
            AddChild(_enemy);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished || _player == null || _enemy == null) return;
            _elapsed += (float)delta;

            if (_startHp < 0f) _startHp = _player.Stats.CurrentHp;

            // ── Phase 1: Enemy가 Player를 실제로 명중시키는가 ──
            if (!_hitConfirmed)
            {
                if (_player.Stats.CurrentHp < _startHp)
                {
                    _hitConfirmed = true;
                }
                else if (_elapsed >= TimeoutSeconds)
                {
                    Finish(false, "Phase1(피격) 타임아웃");
                    return;
                }
                return;
            }

            // ── Phase 2: 처치 → Die()→GrantRewardsToPlayer() 엔진 경로 검증 ──
            if (!_killTriggered)
            {
                _enemy.ReceiveHit(new DamageResult(true, false, 999999f), Vector3.Forward, 0f);
                _killTriggered = true;
                _framesSinceKill = 0;
                return;
            }

            _framesSinceKill++;
            if (_framesSinceKill < 2) return; // QueueFree가 프레임 말미에 처리되도록 한 프레임 대기

            bool enemyFreed = !IsInstanceValid(_enemy);
            bool xpGranted = _player.Progression.CurrentXp > 0 || _player.Stats.Level > 1;
            bool pass = enemyFreed && xpGranted;

            Finish(pass, $"enemyFreed={enemyFreed} xp={_player.Progression.CurrentXp} level={_player.Stats.Level} inv={_player.Inventory.Count}");
        }

        private void Finish(bool pass, string detail)
        {
            _finished = true;
            GD.Print($"[CombatSmokeTest] {(pass ? "PASS" : "FAIL")} {detail} elapsed={_elapsed:F1}s");
            GetTree().Quit(pass ? 0 : 1);
        }
    }
}
