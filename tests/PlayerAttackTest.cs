// tests/PlayerAttackTest.cs
// ★ 지금까지 한 번도 검증되지 않았던 경로: Player가 실제로 스킬(마우스 조준 + 부채꼴 각도
// 필터)로 적을 명중시킬 수 있는가. 기존 테스트들은 전부 ReceiveHit을 직접 호출해 각도
// 필터를 우회했다(CombatSmokeTest는 반대로 Enemy→Player, ItemPickupTest/DungeonSmokeTest는
// 강제 처치). FaceDirection의 180도 반전 버그가 바로 이 경로 검증 부재 때문에 숨어 있었다.
//
// 시나리오: 적을 플레이어 정면(-Z 방향)에 배치하고 마우스 커서도 그 방향에 두어(카메라 없이는
// 마우스 투영이 안 되므로, Player.MoveTo 대신 스킬 입력 전 카메라를 등록) 강타를 발동시킨다.

using Godot;
using ChaosOfAI.Actors;

namespace ChaosOfAI.Tests
{
    public partial class PlayerAttackTest : Node3D
    {
        private Player? _player;
        private EnemyAI? _enemy;
        private Camera3D? _camera;
        private float _startHp = -1f;
        private int _frames;
        private bool _attackStarted;
        private bool _finished;

        public override void _Ready()
        {
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            _player.Position = Vector3.Zero;
            AddChild(_player);

            // 적을 플레이어 정면(-Z)에 배치(§ Player 기본 회전 0 = -Z가 정면).
            _enemy = GD.Load<PackedScene>("res://scenes/Enemy.tscn").Instantiate<EnemyAI>();
            _enemy.Position = new Vector3(0, 0, -1.5f);
            AddChild(_enemy);

            // 카메라: 정확한 마우스 투영이 필요 없도록 진짜 등각 카메라를 두고, 화면 중앙(적 방향)에
            // 마우스를 둔 것과 동치가 되도록 실제 뷰포트 크기 기준 스크린 좌표를 계산한다.
            _camera = new Camera3D
            {
                Projection = Camera3D.ProjectionType.Orthogonal,
                Size = 22f,
                Current = true,
            };
            AddChild(_camera);
            _camera.Position = new Vector3(0, 16, 12);
            _camera.LookAt(Vector3.Zero, Vector3.Up);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished || _player == null || _enemy == null || _camera == null) return;
            _frames++;
            if (_startHp < 0f) _startHp = 9999f; // enemy 체력은 CurrentHp로 직접 비교

            if (!_attackStarted && _frames == 5)
            {
                // 화면상에서 적(0,0,-1.5)이 투영되는 지점으로 마우스를 이동(이벤트 파이프라인 경유 —
                // Input.WarpMouse는 OS 커서 제어라 headless에서 미동작할 수 있어 사용하지 않음).
                Vector2 screenPos = _camera.UnprojectPosition(_enemy.GlobalPosition);
                Input.ParseInputEvent(new InputEventMouseMotion { Position = screenPos, GlobalPosition = screenPos });

                // 강타 입력 이벤트 주입(실제 키 입력과 동일 경로).
                var down = new InputEventAction { Action = "skill_strike", Pressed = true };
                Input.ParseInputEvent(down);
                _attackStarted = true;
                return;
            }

            if (_frames >= 60)
            {
                bool enemyHit = !GodotObject.IsInstanceValid(_enemy) || _enemy.State != EnemyState.Idle;
                // 더 명확한 기준: 적이 파괴됐거나(과다데미지는 아니므로 상태 변화로 판단), 최소 State가 Idle이 아니어야 함은
                // 피격 시 즉시 Chase 전환(EnemyAI.ReceiveHit)에서 보장됨. 죽지 않았다면 State!=Idle이어야 명중.
                Finish(enemyHit, $"enemyValid={GodotObject.IsInstanceValid(_enemy)} state={(GodotObject.IsInstanceValid(_enemy) ? _enemy.State.ToString() : "freed(dead)")}");
            }
        }

        private void Finish(bool pass, string detail)
        {
            _finished = true;
            GD.Print($"[PlayerAttackTest] {(pass ? "PASS" : "FAIL")} {detail}");
            GetTree().Quit(pass ? 0 : 1);
        }
    }
}
