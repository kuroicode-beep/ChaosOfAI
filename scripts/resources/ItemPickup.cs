// scripts/resources/ItemPickup.cs
// 바닥 드랍 픽업(§5.4/§5.5): 처치 위치에 등급색 이펙트로 떠 있다가, 플레이어가 걸어 오면
// Player.PickupItem을 호출하고 사라진다. "처치 → 드랍 → 습득하러 감" 손맛을 위해
// 즉시 자동 적용(이전 간소화)에서 전환.

using Godot;
using ChaosOfAI.Actors;

namespace ChaosOfAI.Resources
{
    public partial class ItemPickup : Area3D
    {
        public ItemData? Item { get; set; }

        private MeshInstance3D _mesh = null!;
        private float _spinSpeed = 1.6f;
        private float _bobPhase;

        public override void _Ready()
        {
            _mesh = GetNode<MeshInstance3D>("MeshInstance3D");
            if (Item != null && _mesh.MaterialOverride is StandardMaterial3D mat)
            {
                mat.AlbedoColor = Item.RarityColor;
                mat.Emission = Item.RarityColor;
            }
            BodyEntered += OnBodyEntered;
        }

        public override void _Process(double delta)
        {
            _bobPhase += (float)delta;
            RotateY(_spinSpeed * (float)delta);
            _mesh.Position = new Vector3(0, 0.6f + Mathf.Sin(_bobPhase * 2f) * 0.12f, 0);
        }

        private void OnBodyEntered(Node3D body)
        {
            if (Item == null || body is not Player player) return;
            player.PickupItem(Item);
            QueueFree();
        }
    }
}
