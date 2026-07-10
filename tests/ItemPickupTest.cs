// tests/ItemPickupTest.cs
// 바닥 드랍 픽업 회귀: 몬스터를 강제 처치해 100% 드랍시키고, 스폰된 ItemPickup에 플레이어를
// 걸어 들어가게 해 실제로 Inventory에 추가되는지 엔진에서 확인한다.
//
// ★ 주의: 노드를 AddChild()한 "다음"에 GlobalPosition을 설정하면, 물리 서버가 그 프레임엔
// 아직 이전(스폰 기본) 위치로 알고 있어 첫 물리 스텝에서 두 바디가 겹친 것으로 오판해
// MoveAndSlide가 서로를 밀어내는 레이스 컨디션이 생긴다(원인 규명 완료). 반드시 AddChild
// 전에 Position을 설정할 것 — CombatSmokeTest 등 다른 테스트도 같은 원칙을 따른다.

using Godot;
using ChaosOfAI.Actors;
using ChaosOfAI.Combat;
using ChaosOfAI.Resources;

namespace ChaosOfAI.Tests
{
    public partial class ItemPickupTest : Node3D
    {
        private Player? _player;
        private EnemyAI? _enemy;
        private int _frames;
        private bool _killed;
        private bool _finished;

        public override void _Ready()
        {
            var floor = new StaticBody3D { CollisionLayer = 1, CollisionMask = 0 };
            floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(40, 1, 40) }, Position = new Vector3(0, -0.5f, 0) });
            AddChild(floor);

            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            _player.Position = Vector3.Zero; // AddChild 전에 설정(물리 서버 레이스 회피)
            AddChild(_player);

            _enemy = GD.Load<PackedScene>("res://scenes/Enemy.tscn").Instantiate<EnemyAI>();
            _enemy.Data = GD.Load<EnemyData>("res://data/enemies/overmind_herald.tres"); // DropChance=1.0
            _enemy.Position = new Vector3(3f, 0, 0); // AddChild 전에 설정
            AddChild(_enemy);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_finished || _player == null || _enemy == null) return;
            _frames++;
            if (_frames < 3) return;

            if (!_killed)
            {
                _enemy.ReceiveHit(new DamageResult(true, false, 999999f), Vector3.Forward, 0f);
                _killed = true;
                return;
            }

            // 처치 직후엔 아직 습득 전이어야 한다(§ 즉시적용 아님, 걸어가야 함).
            if (_frames == 6)
            {
                if (_player.Inventory.Count != 0)
                {
                    Finish(false, $"처치 직후인데 벌써 인벤토리에 있음(즉시적용 남아있음) count={_player.Inventory.Count}");
                    return;
                }
            }

            // 드랍 위치로 이동 지시.
            if (_frames == 8)
                _player.MoveTo(new Vector3(3f, 0, 0));

            if (_frames >= 120)
            {
                bool pass = _player.Inventory.Count == 1;
                Finish(pass, $"walkedInventoryCount={_player.Inventory.Count} playerPos={_player.GlobalPosition}");
            }
        }

        private void Finish(bool pass, string detail)
        {
            _finished = true;
            GD.Print($"[ItemPickupTest] {(pass ? "PASS" : "FAIL")} {detail}");
            GetTree().Quit(pass ? 0 : 1);
        }
    }
}
